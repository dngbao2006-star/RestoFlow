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

    public DateTime? ArrivalTime { get; set; }
    public bool HasOrdered { get; set; }
    public int OrderItemCount { get; set; }
    public string OrderTotal { get; set; } = string.Empty;

    public string DisplayNumber => $"Bàn T{Number}";

    // ── BỔ SUNG ICON THEO YÊU CẦU ───────────────────────────────────────────
    public string StatusIcon => Status switch
    {
        TableStatus.Available => "✅",
        TableStatus.Occupied when HasOrdered => "🍲", // Icon nồi/kitchen lid
        TableStatus.Occupied => "👤",
        TableStatus.Reserved => "🕒",
        TableStatus.NeedsClearing => "🧹",
        _ => "❓"
    };

    public string StatusLabel => Status switch
    {
        TableStatus.Available => "Còn trống",
        TableStatus.Occupied when HasOrdered => "Đã gọi món",
        TableStatus.Occupied => "Chưa gọi món",
        TableStatus.Reserved => "Đặt trước",
        TableStatus.NeedsClearing => "Cần dọn",
        _ => "Không xác định"
    };

    public Color StatusColor => Status switch
    {
        TableStatus.Available => Color.FromArgb("#22C55E"),
        TableStatus.Occupied when HasOrdered => Color.FromArgb("#3B82F6"), // Xanh dương
        TableStatus.Occupied => Color.FromArgb("#EF4444"),                 // Đỏ
        TableStatus.Reserved => Color.FromArgb("#F59E0B"),
        TableStatus.NeedsClearing => Color.FromArgb("#6B7280"),
        _ => Color.FromArgb("#7B6A57")
    };

    public string StatusBadgeText => StatusLabel.ToUpperInvariant();

    public bool IsAvailable => Status == TableStatus.Available;
    public bool IsReserved => Status == TableStatus.Reserved;
    public bool IsOccupied => Status == TableStatus.Occupied;
    public bool IsNeedsClearing => Status == TableStatus.NeedsClearing;

    public bool CanClearEmptyOccupied => Status == TableStatus.Occupied && !HasOrdered;
    public bool CanPayOccupied => Status == TableStatus.Occupied && HasOrdered;

    public string OccupiedDuration => ArrivalTime.HasValue
        ? $"{(int)(DateTime.Now - ArrivalTime.Value).TotalMinutes} phút"
        : string.Empty;

    public string OrderStatusText => HasOrdered ? "Đã gọi món" : "Chưa gọi món";
    public string ReservationGuestName => ReservedFor ?? string.Empty;
    public string ReservationTime => ReservedAt.HasValue
        ? ReservedAt.Value.ToString("HH:mm")
        : string.Empty;
}