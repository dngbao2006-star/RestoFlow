using System.Globalization;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Converters;

/// <summary>
/// Trả về số badge tương ứng với từng mục menu sidebar dựa trên tên Title.
/// </summary>
public class TitleToBadgeCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            var title = value?.ToString() ?? string.Empty;
            var ctx = AppContext.Instance;
            return title switch
            {
                "Trạng thái món" => ctx.ReadyDishCount,
                "Nhắn tin"       => ctx.UnreadMessages,
                "Thông báo"      => ctx.UnreadNotifications,
                _                => 0
            };
        }
        catch
        {
            return 0;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
