using System.Globalization;

namespace AppManagermentRestaurant.Helpers;

public class StatusFilterColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string selectedStatus && parameter is string status)
        {
            if (selectedStatus == status)
            {
                return Color.FromArgb("#F5F0E8"); // Highlight color
            }
        }
        return Color.FromArgb("#FDFAF6"); // Default color
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
