using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MonthlyReportGenerator.Models;
using MonthlyReportGenerator.ViewModels;

namespace MonthlyReportGeneratorModern.Views
{
    public partial class MonthlyReportView : UserControl
    {
        public MonthlyReportView()
        {
            InitializeComponent();
        }

        private MonthlyReportViewModel? Vm => DataContext as MonthlyReportViewModel;

        private static readonly SolidColorBrush RestRowBrush = new(ColorHelper.FromArgb(255, 0xFE, 0xE2, 0xE2));
        private static readonly SolidColorBrush RestRowBorderBrush = new(ColorHelper.FromArgb(255, 0xFE, 0xCA, 0xCA));

        /// <summary>
        /// 单击即进入编辑（WinUI DataGrid 默认双击）；时间列额外弹出鼠标下方的时分下拉。
        /// 交互细节在低配机实测（MIGRATION.md §6 Spike 1/4）。
        /// </summary>
        private void OnGridPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not DataGrid grid || Vm is null) return;

            var position = e.GetCurrentPoint(grid).Position;
            var hitElements = VisualTreeHelper.FindElementsInHostCoordinates(position, grid);
            var combo = hitElements.OfType<ComboBox>().FirstOrDefault();

            try
            {
                grid.BeginEdit();
            }
            catch
            {
                // 无当前单元格等场景：保持默认双击编辑
            }

            if (combo is not null)
                combo.IsDropDownOpen = true;
        }

        /// <summary>休息日行红底（周末/法定节假日且非补班日）。</summary>
        private void OnGridLoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is DailyEntry { IsRestDay: true })
            {
                e.Row.Background = RestRowBrush;
                e.Row.BorderBrush = RestRowBorderBrush;
            }
        }

        /// <summary>“×”按钮：单击即清空该条记录的上/下班时间（整行替换保证界面同步）。</summary>
        private void OnClearTimeClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.DataContext is not DailyEntry entry ||
                button.Tag is not string tag)
                return;

            Vm?.ClearEntryTime(entry, tag == "start");
        }

        /// <summary>
        /// 时间下拉获得焦点且当前时间未填写时，自动预填默认时间（上班 09:00 / 下班 17:00），
        /// 与原网页版 input[type=time] 的 focus 行为一致。Tag 形如 "start:09" / "end:17"。
        /// </summary>
        private void OnTimeComboFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox combo ||
                combo.DataContext is not DailyEntry entry ||
                combo.Tag is not string tag)
                return;

            var parts = tag.Split(':');
            if (parts.Length != 2) return;

            if (parts[0] == "start")
            {
                if (entry.StartTimeText.Length == 0) entry.StartHour = parts[1];
            }
            else
            {
                if (entry.EndTimeText.Length == 0) entry.EndHour = parts[1];
            }
        }

        /// <summary>节假日数据加载完成后：重置 ItemsSource 触发行重绘（红底随新日历更新）。</summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Vm is { } vm) vm.CalendarRefreshed += OnCalendarRefreshed;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Vm is { } vm) vm.CalendarRefreshed -= OnCalendarRefreshed;
        }

        private void OnCalendarRefreshed()
        {
            if (Vm is null) return;
            MainGrid.ItemsSource = null;
            MainGrid.ItemsSource = Vm.Days;
        }
    }
}
