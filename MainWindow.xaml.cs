using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MonthlyReportGenerator.Services;
using MonthlyReportGenerator.ViewModels;

namespace MonthlyReportGeneratorModern
{
    public sealed partial class MainWindow : Window
    {
        private readonly ShellViewModel _vm = new();

        public MainWindow()
        {
            InitializeComponent();
            RootGrid.DataContext = _vm;

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

        private void OnTabSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var index = sender.Items.IndexOf(sender.SelectedItem);
            if (index >= 0)
                _vm.SelectedTab = index;
        }
    }
}
