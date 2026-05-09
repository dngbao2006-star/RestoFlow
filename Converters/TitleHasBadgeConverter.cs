using System.Globalization;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Converters;

/// <summary>
/// Trả về true nếu mục menu hiện tại có badge count > 0.
/// Dùng cho IsVisible của badge trong sidebar ItemTemplate.
/// </summary>
public class TitleHasBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            var title = value?.ToString() ?? string.Empty;
            var ctx = AppContext.Instance;
            var count = title switch
            {
                "Trạng thái món" => ctx.ReadyDishCount,
                "Nhắn tin"       => ctx.UnreadMessages,
                "Thông báo"      => ctx.UnreadNotifications,
                _                => 0
            };
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
