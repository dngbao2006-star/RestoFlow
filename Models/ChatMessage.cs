using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsSystem { get; set; }
    public bool IsMine { get; set; }
    public bool IsRead { get; set; }

    public string TimestampDisplay => Formatters.FormatTime(Timestamp);

    public bool IsManager => SenderRole.Equals("Manager", StringComparison.OrdinalIgnoreCase);

    public string SenderInitial
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SenderName))
            {
                return "?";
            }

            return SenderName.Trim()[0].ToString().ToUpperInvariant();
        }
    }

    public string AvatarBackgroundColor => IsManager ? "#1B3A6B" : "#4A90D9";
}
