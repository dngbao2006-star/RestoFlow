using System.Globalization;

namespace AppManagermentRestaurant.Converters;

/// <summary>
/// Trả về true nếu giá trị int > 0 (dùng để ẩn/hiện badge).
/// </summary>
public class IntGreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
