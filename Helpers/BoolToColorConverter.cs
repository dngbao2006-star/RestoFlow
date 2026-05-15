using System.Globalization;

namespace AppManagermentRestaurant.Helpers;

/// <summary>
/// Chuyển đổi giá trị bool thành màu sắc.
/// Khi giá trị là true, trả về màu đỏ (lỗi).
/// Khi giá trị là false, trả về màu border mặc định.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isError && isError)
        {
            return Color.FromArgb("#F44336");
        }

        return Color.FromArgb("#E0E0E0");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
