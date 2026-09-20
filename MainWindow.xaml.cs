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

            // 页面 VM 懒创建：创建完成后把视图 DataContext 指过去（不用 ElementName 绑定，WinUI 3 下不可靠）
            _vm.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ShellViewModel.MonthlyPage):
                        MonthlyView.DataContext = _vm.MonthlyPage;
                        break;
                    case nameof(ShellViewModel.DailyPage):
                        DailyView.DataContext = _vm.DailyPage;
                        break;
                    case nameof(ShellViewModel.WeeklyPage):
                        WeeklyView.DataContext = _vm.WeeklyPage;
                        break;
                }
            };
            MonthlyView.DataContext = _vm.MonthlyPage;
            UpdatePageVisibility();

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
            try
            {
                // 打包运行时工作目录不可靠，用程序所在目录拼接绝对路径
                AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, @"Assets\app.ico"));
            }
            catch { /* 图标设置失败不影响运行 */ }

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

        /// <summary>三个页面视图常驻，仅切换 Visibility（与 WPF 版行为一致）。</summary>
        private void UpdatePageVisibility()
        {
            MonthlyView.Visibility = _vm.SelectedTab == 0 ? Visibility.Visible : Visibility.Collapsed;
            DailyView.Visibility = _vm.SelectedTab == 1 ? Visibility.Visible : Visibility.Collapsed;
            WeeklyView.Visibility = _vm.SelectedTab == 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTabSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var index = sender.Items.IndexOf(sender.SelectedItem);
            if (index < 0) return;
            _vm.SelectedTab = index;
            UpdatePageVisibility();
        }
    }
}
