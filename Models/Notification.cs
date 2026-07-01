using AppManagermentRestaurant.Helpers;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;

namespace AppManagermentRestaurant.Models;

public class Notification
{
    [JsonIgnore]
    public string FirebaseKey { get; set; } = string.Empty;

    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Read { get; set; }
    public string Audience { get; set; } = "All";
    public Dictionary<string, bool> ReadBy { get; set; } = new();
    public int? TableId { get; set; }
    public int? OrderId { get; set; }

    public string TimestampDisplay => Formatters.FormatDateTime(Timestamp);

    [JsonIgnore]
    public bool IsImportant => Type == NotificationType.Danger;

    [JsonIgnore]
    public string ImportanceLabel => IsImportant ? "Quan trọng" : "Thông thường";

    public string TypeLabel => Type switch
    {
        NotificationType.Info => "Thông tin",
        NotificationType.Warning => "Cảnh báo",
        NotificationType.Success => "Thành công",
        NotificationType.Danger => "Quan trọng",
        _ => "Thông tin"
    };

    public Color TypeColor => Type switch
    {
        NotificationType.Info => Color.FromArgb("#4A90D9"),
        NotificationType.Warning => Color.FromArgb("#F59E0B"),
        NotificationType.Success => Color.FromArgb("#22C55E"),
        NotificationType.Danger => Color.FromArgb("#EF4444"),
        _ => Color.FromArgb("#4A90D9")
    };

    public string IconGlyph => IsImportant ? "!" : Type == NotificationType.Success ? "✓" : "🔔";

    public Color IconColor => IsImportant
        ? Color.FromArgb("#DC2626")
        : Type == NotificationType.Success
            ? Color.FromArgb("#22C55E")
            : Color.FromArgb("#4A90D9");

    public Color IconBackgroundColor => Type switch
    {
        NotificationType.Danger => Color.FromRgba(220, 38, 38, 0.15f),
        NotificationType.Success => Color.FromRgba(34, 197, 94, 0.15f),
        NotificationType.Info => Color.FromRgba(74, 144, 217, 0.15f),
        _ => Color.FromRgba(245, 158, 11, 0.15f)
    };

    public Color RowBackgroundColor => Read
        || (AppManagermentRestaurant.Services.AppContext.Instance.CurrentUser is { } user
            && ReadBy?.TryGetValue(user.FirebaseUid, out var hasRead) == true && hasRead)
            ? Colors.Transparent
            : Color.FromRgba(74, 144, 217, 0.05f);
}
