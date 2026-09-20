namespace MonthlyReportGenerator.ViewModels;

/// <summary>
/// 时间下拉选项（小时 00–23 / 分钟 00–59）。
/// 作为 XAML 静态资源使用：DataGrid 模板列作用域内 ElementName 绑定不可靠，
/// 改用 StaticResource + Source 绑定。
/// </summary>
public sealed class TimeOptions
{
    public IReadOnlyList<string> Hours { get; } = BuildRange(0, 23);
    public IReadOnlyList<string> Minutes { get; } = BuildRange(0, 59);

    private static List<string> BuildRange(int start, int end)
    {
        var list = new List<string>();
        for (var i = start; i <= end; i++)
            list.Add(i.ToString("00"));
        return list;
    }
}
