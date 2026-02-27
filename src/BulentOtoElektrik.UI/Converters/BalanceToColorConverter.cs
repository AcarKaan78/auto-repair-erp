using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BulentOtoElektrik.UI.Converters;

public class BalanceToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            if (amount > 0) return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); // Red for debt
            if (amount == 0) return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green for paid
            return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green for credit
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
