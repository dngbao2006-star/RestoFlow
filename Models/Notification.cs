using AppManagermentRestaurant.Helpers;
using Microsoft.Maui.Graphics;

namespace AppManagermentRestaurant.Models;

public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Read { get; set; }
    public int? TableId { get; set; }
    public int? OrderId { get; set; }

    public string TimestampDisplay => Formatters.FormatTime(Timestamp);

    public string TypeLabel => Type switch
    {
        NotificationType.Info => "Info",
        NotificationType.Warning => "Warning",
        NotificationType.Success => "Success",
        NotificationType.Danger => "Alert",
        _ => "Info"
    };

    public Color TypeColor => Type switch
    {
        NotificationType.Info => Color.FromArgb("#4A90D9"),
        NotificationType.Warning => Color.FromArgb("#F59E0B"),
        NotificationType.Success => Color.FromArgb("#22C55E"),
        NotificationType.Danger => Color.FromArgb("#EF4444"),
        _ => Color.FromArgb("#4A90D9")
    };
}
