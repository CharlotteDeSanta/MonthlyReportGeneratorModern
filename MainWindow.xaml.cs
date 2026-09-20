using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MonthlyReportGenerator.Services;
using MonthlyReportGenerator.ViewModels;
using Windows.Graphics;

namespace MonthlyReportGeneratorModern
{
    public sealed partial class MainWindow : Window
    {
        private readonly ShellViewModel _vm = new();

        public MainWindow()
        {
            InitializeComponent();
            RootGrid.DataContext = _vm;

            // 初始尺寸与 WPF 版一致（1280×880，按屏幕工作区钳制并居中），
            // 避免模板默认小窗口下各区域挤压重叠。
            SizeWindowToWorkArea();

            // 自绘标题栏：内容延伸进标题栏 + 应用图标（MIGRATION.md §5/§6.3）
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            if (AppWindow.TitleBar is { } titleBar)
            {
                titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
            try { AppWindow.SetIcon(@"Assets\app.ico"); } catch { /* 图标设置失败不影响运行 */ }

            Closed += (_, _) => _vm.Shutdown();

            // WinUI 3 Window 无 Loaded 事件：首次激活时初始化对话框服务并预构建页面
            var ready = false;
            Activated += (_, _) =>
            {
                if (ready) return;
                ready = true;
                DialogService.Initialize(this);
                // 空闲时段分帧预构建日报/周报页，消除首次切换顿挫
                _vm.PreloadPages(DispatcherQueue);
            };
        }

        private void SizeWindowToWorkArea()
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            if (area is null)
            {
                AppWindow.Resize(new SizeInt32(1280, 880));
                return;
            }

            var work = area.WorkArea;
            var width = Math.Min(1280, work.Width);
            var height = Math.Min(880, work.Height);
            AppWindow.MoveAndResize(new RectInt32(
                work.X + (work.Width - width) / 2,
                work.Y + (work.Height - height) / 2,
                width,
                height));
        }

        private void OnTabSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var index = sender.Items.IndexOf(sender.SelectedItem);
            if (index >= 0)
                _vm.SelectedTab = index;
        }
    }
}
