namespace AppManagermentRestaurant.Models;

/// <summary>
/// DTO for OrderItems on Firebase — has extra OrderId field to link back to parent Order.
/// </summary>
public class FirebaseOrderItemDto
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DishStatus Status { get; set; }
    public string Image { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for Revenue points on Firebase.
/// </summary>
public class FirebaseRevenuePointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
