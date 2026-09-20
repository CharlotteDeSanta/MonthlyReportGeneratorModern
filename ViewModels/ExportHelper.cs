using System.IO;
using MonthlyReportGenerator.Services;

namespace MonthlyReportGenerator.ViewModels;

/// <summary>导出公共流程：文件保存选择 → 写 XLSX → 成功提示 / 失败写日志。</summary>
public static class ExportHelper
{
    public static async Task ExportWithDialogAsync(string defaultFileName, Action<string> writeTo)
    {
        var path = await DialogService.PickSaveFileAsync(defaultFileName);
        if (path is null) return;

        try
        {
            writeTo(path);
            await DialogService.ShowMessageAsync("导出成功", $"已导出：{path}");
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    /// <summary>完整异常写入日志并提示（弹窗文本不可复制）。</summary>
    public static async Task ReportErrorAsync(Exception ex)
    {
        try
        {
            var logFolder = DraftService.Folder; // 打包版 LocalState
            Directory.CreateDirectory(logFolder);
            var logPath = Path.Combine(logFolder, "export-error.log");
            File.AppendAllText(logPath,
                $"==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===={Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");

            await DialogService.ShowMessageAsync("错误",
                $"导出失败：{ex.Message}{Environment.NewLine}{Environment.NewLine}详细信息已写入：{logPath}");
        }
        catch
        {
            await DialogService.ShowMessageAsync("错误", $"导出失败：{ex.Message}");
        }
    }
}
