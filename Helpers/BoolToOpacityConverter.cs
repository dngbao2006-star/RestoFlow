using System.Globalization;

namespace AppManagermentRestaurant.Helpers;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return 0.6; // Out of stock - reduced opacity
        }

        return 1.0; // In stock - full opacity
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
