using System.Globalization;
using System.Windows.Data;

namespace BulentOtoElektrik.UI.Converters;

public class CurrencyFormatConverter : IValueConverter
{
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
            return amount.ToString("C2", TurkishCulture);
        if (value is double dAmount)
            return dAmount.ToString("C2", TurkishCulture);
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && decimal.TryParse(str, NumberStyles.Currency, TurkishCulture, out var result))
            return result;
        return 0m;
    }
}
