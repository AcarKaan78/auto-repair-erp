using System.Globalization;
using System.Windows.Data;

namespace BulentOtoElektrik.UI.Converters;

public class IsNegativeConverter : IValueConverter
{
    public static readonly IsNegativeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return d < 0;
        if (value is decimal m)
            return m < 0;
        if (value is int i)
            return i < 0;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
