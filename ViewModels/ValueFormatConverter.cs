using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace MonthlyReportGenerator.ViewModels;

/// <summary>
/// IFormattable 值格式化（WinUI Binding 无 StringFormat，用转换器替代）：
/// ConverterParameter 为格式串（如 yyyy-MM-dd、0.00、0.0）。
/// </summary>
public class ValueFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is IFormattable formattable
            ? formattable.ToString(parameter as string, CultureInfo.CurrentCulture)
            : value?.ToString() ?? "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
