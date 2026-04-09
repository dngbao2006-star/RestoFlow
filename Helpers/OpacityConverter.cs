using System.Globalization;

namespace AppManagermentRestaurant.Helpers;

public class OpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string selectedStatus && parameter is string status)
        {
            if (selectedStatus == status)
            {
                return 1.0; // Fully visible
            }
        }
        return 0.6; // Semi-transparent
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
