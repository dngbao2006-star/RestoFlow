using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace AppManagermentRestaurant.Helpers;

public class CategorySelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string category || values[1] is not string selectedCategory)
            return Colors.Transparent;

        return category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase) 
            ? Color.FromArgb("#2196F3") 
            : Colors.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CategoryBackgroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string category || values[1] is not string selectedCategory)
            return Colors.White;

        return category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase) 
            ? Color.FromArgb("#E3F2FD") 
            : Colors.White;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
