using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace MonthlyReportGenerator.Services;

/// <summary>
/// WinUI 3 对话框与文件保存选择的中枢（单窗口应用）。
/// 由 MainWindow 初始化一次（XamlRoot + 窗口句柄），ViewModel 层统一经此弹出
/// ContentDialog / FileSavePicker（对应 WPF 的 MessageBox / SaveFileDialog）。
/// </summary>
public static class DialogService
{
    private static XamlRoot? _xamlRoot;
    private static nint _hwnd;

    public static void Initialize(Window window)
    {
        _xamlRoot = window.Content.XamlRoot;
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
    }

    /// <summary>信息提示（单按钮）。</summary>
    public static async Task ShowMessageAsync(string title, string message, string closeText = "确定")
    {
        if (_xamlRoot is null) return;
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = closeText,
                XamlRoot = _xamlRoot,
            };
            await dialog.ShowAsync();
        }
        catch
        {
            // 已有对话框弹出等场景：静默降级
        }
    }

    /// <summary>确认对话框（是/否）。</summary>
    public static async Task<bool> ConfirmAsync(
        string title, string message, string primaryText = "确定", string closeText = "取消")
    {
        if (_xamlRoot is null) return false;
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = primaryText,
                CloseButtonText = closeText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = _xamlRoot,
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>XLSX 保存位置选择（FileSavePicker），返回所选路径（取消返回 null）。</summary>
    public static async Task<string?> PickSaveFileAsync(string suggestedFileName)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedFileName = suggestedFileName,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeChoices.Add("Excel 工作簿", new List<string> { ".xlsx" });
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }
        catch
        {
            return null;
        }
    }
}
