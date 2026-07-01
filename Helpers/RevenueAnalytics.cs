using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Helpers;

public readonly record struct AnalyticsDateRange(DateTime Start, DateTime End)
{
    public bool Contains(DateTime value) => value >= Start && value < End;
}

public static class RevenueAnalytics
{
    public static AnalyticsDateRange GetRange(string period, DateTime now)
    {
        var today = now.Date;
        return period switch
        {
            "today" => new AnalyticsDateRange(today, today.AddDays(1)),
            "month" => new AnalyticsDateRange(new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1)),
            "year" => new AnalyticsDateRange(new DateTime(now.Year, 1, 1), new DateTime(now.Year + 1, 1, 1)),
            _ => GetWeekRange(today)
        };
    }

    public static AnalyticsDateRange GetPreviousRange(string period, DateTime now)
    {
        var current = GetRange(period, now);
        if (period == "year")
            return new AnalyticsDateRange(current.Start.AddYears(-1), current.Start);
        if (period == "month")
            return new AnalyticsDateRange(current.Start.AddMonths(-1), current.Start);

        var duration = current.End - current.Start;
        return new AnalyticsDateRange(current.Start - duration, current.Start);
    }

    public static List<Invoice> FilterInvoices(
        IEnumerable<Invoice> invoices,
        AnalyticsDateRange range)
        => invoices.Where(invoice => !IsKnownSeedInvoice(invoice) && range.Contains(invoice.CreatedAt))
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ToList();

    private static bool IsKnownSeedInvoice(Invoice invoice)
        => (invoice.Id, invoice.OrderId, invoice.Total) switch
        {
            (8001, 201, 260000m) => true,
            (8002, 202, 285000m) => true,
            (8003, 203, 240000m) => true,
            (8004, 104, 185000m) => true,
            _ => false
        };

    public static List<RevenueChartItem> BuildChart(
        IReadOnlyCollection<Invoice> invoices,
        string period,
        DateTime now)
    {
        var points = period switch
        {
            "today" => Enumerable.Range(0, 24)
                .Select(hour => (Label: $"{hour:00}:00", Value: invoices
                    .Where(invoice => invoice.CreatedAt.Hour == hour)
                    .Sum(invoice => invoice.Total)))
                .ToList(),
            "month" => BuildMonthWeekPoints(invoices, GetRange("month", now)),
            "year" => Enumerable.Range(1, 12)
                .Select(month => (Label: $"Th{month}", Value: invoices
                    .Where(invoice => invoice.CreatedAt.Month == month)
                    .Sum(invoice => invoice.Total)))
                .ToList(),
            _ => BuildWeekPoints(invoices, GetRange("week", now).Start)
        };

        var max = points.Count == 0 ? 0 : points.Max(point => point.Value);
        return points.Select(point => new RevenueChartItem
        {
            Label = point.Label,
            Value = point.Value,
            BarWidthFraction = max > 0 ? Math.Max(2, (double)(point.Value / max) * 250) : 2
        }).ToList();
    }

    public static List<DishRevenue> BuildTopDishes(IEnumerable<Invoice> invoices, int take = 8)
    {
        var dishes = invoices
            .SelectMany(invoice => invoice.Items ?? Enumerable.Empty<OrderItem>())
            .GroupBy(item => item.MenuItemId > 0 ? $"id:{item.MenuItemId}" : $"name:{item.Name}")
            .Select(group => new
            {
                Name = group.First().Name,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.LineTotal)
            })
            .OrderByDescending(item => item.Revenue)
            .ToList();
        var total = dishes.Sum(item => item.Revenue);

        return dishes.Take(take).Select(item => new DishRevenue
        {
            Name = item.Name,
            Revenue = item.Revenue,
            Quantity = item.Quantity,
            Share = total > 0 ? (double)(item.Revenue / total) : 0
        }).ToList();
    }

    public static List<DishRevenue> BuildTopSellingDishes(IEnumerable<Invoice> invoices, int take = 3)
    {
        var dishes = invoices
            .SelectMany(invoice => invoice.Items ?? Enumerable.Empty<OrderItem>())
            .GroupBy(item => item.MenuItemId > 0 ? $"id:{item.MenuItemId}" : $"name:{item.Name}")
            .Select(group => new
            {
                Name = group.First().Name,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.LineTotal)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.Revenue)
            .ToList();
        var totalQuantity = dishes.Sum(item => item.Quantity);

        return dishes.Take(take).Select(item => new DishRevenue
        {
            Name = item.Name,
            Quantity = item.Quantity,
            Revenue = item.Revenue,
            Share = totalQuantity > 0 ? (double)item.Quantity / totalQuantity : 0
        }).ToList();
    }

    private static AnalyticsDateRange GetWeekRange(DateTime today)
    {
        var daysSinceMonday = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var start = today.AddDays(-daysSinceMonday);
        return new AnalyticsDateRange(start, start.AddDays(7));
    }

    private static List<(string Label, decimal Value)> BuildWeekPoints(
        IReadOnlyCollection<Invoice> invoices,
        DateTime weekStart)
    {
        var labels = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
        return Enumerable.Range(0, 7)
            .Select(index =>
            {
                var day = weekStart.AddDays(index);
                return (labels[index], invoices
                    .Where(invoice => invoice.CreatedAt.Date == day)
                    .Sum(invoice => invoice.Total));
            })
            .ToList();
    }

    private static List<(string Label, decimal Value)> BuildMonthWeekPoints(
        IReadOnlyCollection<Invoice> invoices,
        AnalyticsDateRange monthRange)
    {
        var points = new List<(string Label, decimal Value)>();
        var segmentStart = monthRange.Start;

        while (segmentStart < monthRange.End)
        {
            var daysUntilSunday = (7 - (int)segmentStart.DayOfWeek) % 7;
            var segmentEndInclusive = segmentStart.AddDays(daysUntilSunday);
            if (segmentEndInclusive >= monthRange.End)
                segmentEndInclusive = monthRange.End.AddDays(-1);
            var segmentEndExclusive = segmentEndInclusive.AddDays(1);
            var value = invoices
                .Where(invoice => invoice.CreatedAt >= segmentStart && invoice.CreatedAt < segmentEndExclusive)
                .Sum(invoice => invoice.Total);
            points.Add(($"{segmentStart:dd/MM} - {segmentEndInclusive:dd/MM}", value));
            segmentStart = segmentEndExclusive;
        }

        return points;
    }
}
