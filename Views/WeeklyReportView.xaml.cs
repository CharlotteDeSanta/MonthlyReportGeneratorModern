using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace MonthlyReportGeneratorModern.Views
{
    public partial class WeeklyReportView : UserControl
    {
        public WeeklyReportView()
        {
            InitializeComponent();
        }

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
    }
}
