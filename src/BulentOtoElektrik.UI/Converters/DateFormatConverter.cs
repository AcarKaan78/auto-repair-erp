using System.Globalization;
using System.Windows.Data;

namespace BulentOtoElektrik.UI.Converters;

public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime date)
            return date.ToString("dd.MM.yyyy");
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParseExact(str, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        return DateTime.Today;
    }
}
