using System.Globalization;
using System.Windows.Data;
using BulentOtoElektrik.Core.Enums;

namespace BulentOtoElektrik.UI.Converters;

public class PaymentMethodConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Nakit",
                PaymentMethod.CreditCard => "Kredi Kartı",
                PaymentMethod.BankTransfer => "Havale/EFT",
                _ => method.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str switch
            {
                "Nakit" => PaymentMethod.Cash,
                "Kredi Kartı" => PaymentMethod.CreditCard,
                "Havale/EFT" => PaymentMethod.BankTransfer,
                _ => PaymentMethod.Cash
            };
        }
        if (value is PaymentMethod pm)
            return pm;
        return PaymentMethod.Cash;
    }
}
