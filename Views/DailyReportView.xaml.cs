using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MonthlyReportGenerator.Models;

namespace MonthlyReportGeneratorModern.Views
{
    public partial class DailyReportView : UserControl
    {
        public DailyReportView()
        {
            InitializeComponent();
        }

        private static readonly SolidColorBrush RestRowBrush = new(ColorHelper.FromArgb(255, 0xFE, 0xE2, 0xE2));
        private static readonly SolidColorBrush RestRowBorderBrush = new(ColorHelper.FromArgb(255, 0xFE, 0xCA, 0xCA));

        /// <summary>单击即进入编辑（WinUI DataGrid 默认双击）。</summary>
        private void OnGridPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not DataGrid grid) return;
            try
            {
                grid.BeginEdit();
            }
            catch
            {
                // 无当前单元格等场景：保持默认双击编辑
            }
        }

        /// <summary>
        /// 休息日行红底（周末/法定节假日且非补班日）。
        /// 注意：DataGrid 虚拟化会回收行容器，非休息日必须显式清除背景，
        /// 否则红底会残留在无关日期上。
        /// </summary>
        private void OnGridLoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is DailyDayEntry { IsRestDay: true })
            {
                e.Row.Background = RestRowBrush;
                e.Row.BorderBrush = RestRowBorderBrush;
            }
            else
            {
                e.Row.Background = null;
                e.Row.BorderBrush = null;
            }
        }
    }
}
