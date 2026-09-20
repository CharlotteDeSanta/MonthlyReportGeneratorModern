using ClosedXML.Excel;
using MonthlyReportGenerator.Models;
using MonthlyReportGenerator.Services;

// ============================================================
// 黄金样本回归（MIGRATION.md §9）
// 输入：AGV月报_曹铮_2026_9.csv 的录入数据（2026-09）
// 期望六项汇总：6.0 / 6.00 / 52.68 / 12.68 / 1.58 / 1.0
// 期望地点汇总：无锡国药 6.0 / 12.68
// ============================================================

var failures = new List<string>();

void Check(string name, decimal actual, decimal expected)
{
    if (actual != expected)
        failures.Add($"{name}: 期望 {expected}，实际 {actual}");
    else
        Console.WriteLine($"PASS  {name,-12} = {actual}");
}

void CheckText(string name, string actual, string expected)
{
    if (actual != expected)
        failures.Add($"{name}: 期望 \"{expected}\"，实际 \"{actual}\"");
    else
        Console.WriteLine($"PASS  {name,-12} = {actual}");
}

// ---------- 录入 2026-09 整月 ----------
var days = new List<DailyEntry>();
for (var day = 1; day <= 30; day++)
    days.Add(new DailyEntry { Date = new DateOnly(2026, 9, day) });

void Set(int day, string location, string start, string end, string content = "")
{
    var d = days[day - 1];
    d.Location = location;
    d.StartTimeText = start;
    d.EndTimeText = end;
    d.WorkContent = content;
}

Set(1, "调休", "", "");
Set(2, "无锡国药", "09:05", "18:51", "初到国药现场，熟悉项目，测量巷道中点，贴地码");
Set(3, "无锡国药", "08:56", "17:33", "继续贴地码，测量货架ab面铅垂线");
Set(4, "无锡国药", "09:02", "18:08", "测量货架ab面铅垂线");
Set(5, "无锡国药", "09:00", "17:10", "安装STU供电单元，绘制货架仓位码定位线");
Set(7, "无锡国药", "08:46", "17:47", "绘制货架仓位码定位线");
Set(8, "无锡国药", "09:00", "17:00", "");

// ---------- 汇总断言 ----------
var summary = ReportLayoutBuilder.ComputeSummary(days);
Check("实际上班天数", summary.ActualWorkDays, 6.0m);
Check("项目出勤天数", summary.ProjectAttendanceDays, 6.00m);
Check("总工作时长", summary.TotalWorkHours, 52.68m);
Check("总加班时长", summary.TotalOvertimeHours, 12.68m);
Check("累计可调休天数", summary.AccruedRestDays, 1.58m);
Check("已调休天数", summary.UsedRestDays, 1.0m);

var locations = ReportLayoutBuilder.ComputeLocationSummary(days);
if (locations.Count != 1 || locations[0].Location != "无锡国药")
{
    failures.Add($"地点汇总: 期望单行 无锡国药，实际 {locations.Count} 行");
}
else
{
    Check("无锡国药 出勤天数", locations[0].AttendanceDays, 6.0m);
    Check("无锡国药 加班时长", locations[0].OvertimeHours, 12.68m);
}

// ---------- 导出三份 XLSX ----------
var outDir = Path.Combine(AppContext.BaseDirectory, "out");
Directory.CreateDirectory(outDir);

var meta = new ReportMeta { Year = 2026, Month = 9, EmployeeName = "曹铮", Level = EngineerLevel.新进工程师 };
var monthlyPath = Path.Combine(outDir, ReportExporter.DefaultFileName(meta));
ReportExporter.Export(meta, days, monthlyPath);
Console.WriteLine($"已导出月报: {monthlyPath}");

// 日报结构冒烟（30 天）
var dailyDays = new List<DailyDayEntry>();
for (var day = 1; day <= 30; day++)
{
    dailyDays.Add(new DailyDayEntry
    {
        Date = new DateOnly(2026, 9, day),
        DoneToday = day % 5 == 0 ? "完成事项" : "",
        PlanTomorrow = day % 3 == 0 ? "计划事项" : "",
        TechLeader = "曹铮",
        Implementer = "曹铮",
    });
}
var dailyPath = Path.Combine(outDir, ReportExporter.DailyFileName(2026, 9));
ReportExporter.ExportDaily(dailyDays, dailyPath);
Console.WriteLine($"已导出日报: {dailyPath}");

// 周报结构冒烟（2026 年第 36 周：2026-08-31 ~ 2026-09-06）
var weekMeta = new WeeklyReportMeta
{
    ProjectName = "无锡国药",
    ReportDate = new DateOnly(2026, 9, 6),
    WeekNumber = 36,
    Writer = "曹铮",
    FollowedLastPlan = true,
    Unfinished = "",
};
var thisWeek = new List<WeekDayEntry>();
var nextWeek = new List<WeekDayEntry>();
for (var i = 0; i < 7; i++)
{
    thisWeek.Add(new WeekDayEntry { DayIndex = i, Date = WeekHelper.MondayOfIsoWeek(2026, 36).AddDays(i), Content = i == 0 ? "本周内容" : "" });
    nextWeek.Add(new WeekDayEntry { DayIndex = i, Date = WeekHelper.MondayOfIsoWeek(2026, 37).AddDays(i), Content = i == 0 ? "下周计划" : "" });
}
var weeklyPath = Path.Combine(outDir, ReportExporter.WeeklyFileName(2026, 36));
ReportExporter.ExportWeekly(weekMeta, thisWeek, nextWeek, weeklyPath);
Console.WriteLine($"已导出周报: {weeklyPath}");

// ---------- 回读月报 XLSX：数值 + 存储格式与黄金样本一致 ----------
// 注：ClosedXML 0.105.1 的 GetString() 对整数值会丢尾零（格式本身已正确落盘），
// 故数值列用「数值 + 存储格式」双重校验，文本列用 GetString。
using (var wb = new XLWorkbook(monthlyPath))
{
    var sheet = wb.Worksheets.First();

    void CheckNum(string name, int row, int col, decimal expectedValue, string expectedFormat)
    {
        var cell = sheet.Cell(row, col);
        var value = cell.GetDouble();
        var fmt = cell.Style.NumberFormat?.Format ?? "";
        var ok = Math.Abs(value - (double)expectedValue) < 0.005 && fmt == expectedFormat;
        if (ok)
            Console.WriteLine($"PASS  {name,-12} value={value} fmt='{fmt}'");
        else
            failures.Add($"{name}: 期望 值 {expectedValue} / 格式 \"{expectedFormat}\"，实际 值 {value} / 格式 \"{fmt}\"");
    }

    // 行布局：1 标题 / 2 元信息 / 3 表头 / 4..33 明细 / 34 空 / 35 汇总标题 / 36 汇总值 / 37 空 / 38 地点标题 / 39 地点表头 / 40 地点行
    CheckText("回读·地点名", sheet.Cell(40, 1).GetString(), "无锡国药");

    CheckNum("回读·实际上班天数", 36, 3, 6.0m, "0.0");
    CheckNum("回读·项目出勤天数", 36, 4, 6.00m, "0.00");
    CheckNum("回读·总工作时长", 36, 5, 52.68m, "0.00");
    CheckNum("回读·总加班时长", 36, 6, 12.68m, "0.00");
    CheckNum("回读·累计可调休", 36, 7, 1.58m, "0.00");
    CheckNum("回读·已调休", 36, 8, 1.0m, "0.0");

    CheckNum("回读·地点出勤", 40, 2, 6.0m, "0.0");
    CheckNum("回读·地点加班", 40, 3, 12.68m, "0.00");
}

// ---------- 结论 ----------
if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"FAILED：{failures.Count} 项不匹配");
    foreach (var f in failures) Console.WriteLine("  - " + f);
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine("全部黄金样本断言通过。");
