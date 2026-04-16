using AppManagermentRestaurant.Helpers;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Models;

public class Invoice
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int TableNumber { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public IEnumerable<OrderItem> Items { get; set; } = new List<OrderItem>();

    public string CreatedAtDisplay => Formatters.FormatDateTime(CreatedAt);

    public string TotalDisplay => Formatters.FormatCurrency(Total);

    public bool HasDiscount => Discount > 0;
}
