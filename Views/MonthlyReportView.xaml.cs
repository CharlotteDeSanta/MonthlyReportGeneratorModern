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

        /// <summary>
        /// 休息日行红底（周末/法定节假日且非补班日）。
        /// 注意：DataGrid 虚拟化会回收行容器，非休息日必须显式清除背景，
        /// 否则红底会残留在无关日期上；时间下拉同样要在行加载后复位选中值。
        /// </summary>
        private void OnGridLoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is DailyEntry { IsRestDay: true })
            {
                e.Row.Background = RestRowBrush;
                e.Row.BorderBrush = RestRowBorderBrush;
            }
            else
            {
                e.Row.Background = null;
                e.Row.BorderBrush = null;
            }

            if (e.Row.DataContext is DailyEntry entry)
                FixTimeCombos(e.Row, entry);
        }

        /// <summary>
        /// 行容器回收后，把该行时间下拉的选中值复位为条目当前值（空→null，显示空白）。
        /// 延迟到布局完成后执行（此时单元格模板已实例化）；若期间行又被回收则跳过。
        /// </summary>
        private void FixTimeCombos(DataGridRow row, DailyEntry entry)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (row.DataContext is not DailyEntry current || !ReferenceEquals(current, entry))
                    return; // 行已被回收给其他条目，由对应 LoadingRow 处理

                foreach (var combo in FindDescendants<ComboBox>(row))
                {
                    if (combo.Tag is not string tag) continue;
                    var parts = tag.Split(':');
                    if (parts.Length != 2) continue;

                    var value = (parts[0], parts[1]) switch
                    {
                        ("start", "h") => entry.StartHour,
                        ("start", "m") => entry.StartMinute,
                        ("end", "h") => entry.EndHour,
                        ("end", "m") => entry.EndMinute,
                        _ => null,
                    };
                    if (value is not null)
                        combo.SelectedItem = value.Length == 0 ? null : value;
                }
            });
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var descendant in FindDescendants<T>(child)) yield return descendant;
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
        /// 小时下拉获得焦点且当前时间未填写时，自动预填默认小时（上班 09 / 下班 17），
        /// 与原网页版 input[type=time] 的 focus 行为一致。Tag 形如 "start:h" / "end:h"。
        /// </summary>
        private void OnTimeComboFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox combo ||
                combo.DataContext is not DailyEntry entry ||
                combo.Tag is not string tag)
                return;

            var parts = tag.Split(':');
            if (parts.Length != 2 || parts[1] != "h") return; // 仅小时下拉预填

            var defaultHour = parts[0] == "start" ? "09" : "17";
            if (parts[0] == "start")
            {
                if (entry.StartTimeText.Length == 0) entry.StartHour = defaultHour;
            }
            else
            {
                if (entry.EndTimeText.Length == 0) entry.EndHour = defaultHour;
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
