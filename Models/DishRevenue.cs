using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Models;

public class DishRevenue
{
    public string Name { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Quantity { get; set; }
    public double Share { get; set; }

    public string RevenueDisplay => Formatters.FormatCurrency(Revenue);
    public string QuantityDisplay => $"{Quantity} phần • {Formatters.FormatCurrency(Revenue)}";
    public string ShareDisplay => $"{Share:P0}";
}
