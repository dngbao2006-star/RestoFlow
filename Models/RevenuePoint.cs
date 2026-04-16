using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Models;

public class RevenuePoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }

    public string ValueDisplay => Formatters.FormatCurrency(Value);
}
