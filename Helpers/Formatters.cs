using System.Globalization;

namespace AppManagermentRestaurant.Helpers;

public static class Formatters
{
    private static readonly CultureInfo ViVn = new("vi-VN");

    public static string FormatCurrency(decimal value)
    {
        return string.Format(ViVn, "{0:c0}", value);
    }

    public static string FormatTime(DateTime value)
    {
        return value.ToString("HH:mm", ViVn);
    }

    public static string FormatDate(DateTime value)
    {
        return value.ToString("dd/MM/yyyy", ViVn);
    }

    public static string FormatDateTime(DateTime value)
    {
        return value.ToString("dd/MM/yyyy HH:mm", ViVn);
    }
}
