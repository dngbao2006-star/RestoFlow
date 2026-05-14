using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Models;

public class RevenueChartItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public double BarWidthFraction { get; set; }

    public string ValueDisplay => Formatters.FormatCurrency(Value);
}
