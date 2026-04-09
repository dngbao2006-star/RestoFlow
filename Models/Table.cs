using Microsoft.Maui.Graphics;

namespace AppManagermentRestaurant.Models;

public class Table
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Floor { get; set; } = string.Empty;
    public TableStatus Status { get; set; }
    public int Capacity { get; set; }
    public int? CurrentOrderId { get; set; }
    public string? ReservedFor { get; set; }
    public DateTime? ReservedAt { get; set; }

    public string DisplayNumber => $"T{Number}";

    public string StatusLabel => Status switch
    {
        TableStatus.Available => "Available",
        TableStatus.Occupied => "Occupied",
        TableStatus.Reserved => "Reserved",
        TableStatus.NeedsClearing => "Needs Clearing",
        _ => "Unknown"
    };

    public Color StatusColor => Status switch
    {
        TableStatus.Available => Color.FromArgb("#22C55E"),
        TableStatus.Occupied => Color.FromArgb("#EF4444"),
        TableStatus.Reserved => Color.FromArgb("#F59E0B"),
        TableStatus.NeedsClearing => Color.FromArgb("#6B7280"),
        _ => Color.FromArgb("#7B6A57")
    };

    public string StatusBadgeText => StatusLabel.ToUpperInvariant();

    public bool IsReserved => Status == TableStatus.Reserved;
    public bool IsOccupied => Status == TableStatus.Occupied;
    public bool IsNeedsClearing => Status == TableStatus.NeedsClearing;

    public string ReservedAtDisplay => ReservedAt.HasValue ? ReservedAt.Value.ToString("HH:mm") : string.Empty;
}
