using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class DashboardPage : ContentPage
{
    private bool _isObservingContext;
    private int _refreshScheduled;

    public ObservableCollection<string> RecentAlerts { get; } = new();
    public ObservableCollection<RevenueChartItem> HourlyRevenue { get; } = new();
    public IEnumerable<Order> ActiveOrders => AppContext.Instance.Orders
        .Where(order => order.Status == OrderStatus.Active)
        .OrderBy(order => order.CreatedAt);

    public string GreetingText
    {
        get
        {
            var greeting = DateTime.Now.Hour switch
            {
                < 11 => "Chào buổi sáng",
                < 14 => "Chào buổi trưa",
                < 18 => "Chào buổi chiều",
                _ => "Chào buổi tối"
            };
            return $"{greeting}, {AppContext.Instance.CurrentUser?.Name ?? "Quản lý"}!";
        }
    }

    public string TodayRevenueDisplay { get; private set; } = Formatters.FormatCurrency(0);
    public string RevenueComparisonDisplay { get; private set; } = "0% so với hôm qua";
    public Color RevenueComparisonColor { get; private set; } = Color.FromArgb("#6B7280");
    public int TodayPaidOrderCount { get; private set; }
    public int ActiveOrderCount => AppContext.Instance.Orders.Count(order => order.Status == OrderStatus.Active);
    public int OnlineStaffCount => AppContext.Instance.StaffMembers.Count(staff => staff.IsOnline);
    public int AvailableCount => AppContext.Instance.AvailableCount;
    public int OccupiedCount => AppContext.Instance.OccupiedCount;
    public int ReservedCount => AppContext.Instance.ReservedCount;
    public int NeedsClearingCount => AppContext.Instance.NeedsClearingCount;

    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = this;
        RefreshDashboard();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObservingContext)
        {
            AppContext.Instance.PropertyChanged += OnContextPropertyChanged;
            AppContext.Instance.Invoices.CollectionChanged += OnSourceCollectionChanged;
            AppContext.Instance.Orders.CollectionChanged += OnSourceCollectionChanged;
            AppContext.Instance.Tables.CollectionChanged += OnSourceCollectionChanged;
            _isObservingContext = true;
        }
        RefreshDashboard();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObservingContext)
        {
            AppContext.Instance.PropertyChanged -= OnContextPropertyChanged;
            AppContext.Instance.Invoices.CollectionChanged -= OnSourceCollectionChanged;
            AppContext.Instance.Orders.CollectionChanged -= OnSourceCollectionChanged;
            AppContext.Instance.Tables.CollectionChanged -= OnSourceCollectionChanged;
            _isObservingContext = false;
        }
        base.OnNavigatingFrom(args);
    }

    private void OnSourceCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => ScheduleRefresh();

    private void OnContextPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => ScheduleRefresh();

    private void ScheduleRefresh()
    {
        if (Interlocked.Exchange(ref _refreshScheduled, 1) == 1) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try { RefreshDashboard(); }
            finally { Interlocked.Exchange(ref _refreshScheduled, 0); }
        });
    }

    private void RefreshDashboard()
    {
        var context = AppContext.Instance;
        var todayRange = RevenueAnalytics.GetRange("today", DateTime.Now);
        var yesterdayRange = RevenueAnalytics.GetPreviousRange("today", DateTime.Now);
        var todayInvoices = RevenueAnalytics.FilterInvoices(context.Invoices, todayRange);
        var yesterdayInvoices = RevenueAnalytics.FilterInvoices(context.Invoices, yesterdayRange);
        var todayRevenue = todayInvoices.Sum(invoice => invoice.Total);
        var yesterdayRevenue = yesterdayInvoices.Sum(invoice => invoice.Total);

        TodayRevenueDisplay = Formatters.FormatCurrency(todayRevenue);
        TodayPaidOrderCount = todayInvoices.Count;
        SetComparison(todayRevenue, yesterdayRevenue);
        RefreshHourlyRevenue(todayInvoices);
        RefreshAlerts(context);

        OnPropertyChanged(nameof(GreetingText));
        OnPropertyChanged(nameof(TodayRevenueDisplay));
        OnPropertyChanged(nameof(RevenueComparisonDisplay));
        OnPropertyChanged(nameof(RevenueComparisonColor));
        OnPropertyChanged(nameof(TodayPaidOrderCount));
        OnPropertyChanged(nameof(ActiveOrderCount));
        OnPropertyChanged(nameof(OnlineStaffCount));
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(OccupiedCount));
        OnPropertyChanged(nameof(ReservedCount));
        OnPropertyChanged(nameof(NeedsClearingCount));
        OnPropertyChanged(nameof(ActiveOrders));
    }

    private void SetComparison(decimal current, decimal previous)
    {
        if (previous <= 0)
        {
            RevenueComparisonDisplay = current > 0 ? "Mới phát sinh hôm nay" : "0% so với hôm qua";
            RevenueComparisonColor = current > 0 ? Color.FromArgb("#16A34A") : Color.FromArgb("#6B7280");
            return;
        }

        var percent = (double)((current - previous) / previous);
        RevenueComparisonDisplay = $"{percent:+0%;-0%;0%} so với hôm qua";
        RevenueComparisonColor = percent >= 0 ? Color.FromArgb("#16A34A") : Color.FromArgb("#DC2626");
    }

    private void RefreshHourlyRevenue(IReadOnlyCollection<Invoice> invoices)
    {
        var ranges = new[] { (0, 5), (6, 9), (10, 13), (14, 17), (18, 21), (22, 23) };
        var values = ranges.Select(range => new
        {
            Label = $"{range.Item1:00}-{range.Item2:00}h",
            Value = invoices.Where(invoice => invoice.CreatedAt.Hour >= range.Item1 && invoice.CreatedAt.Hour <= range.Item2)
                .Sum(invoice => invoice.Total)
        }).ToList();
        var max = values.Count == 0 ? 0 : values.Max(item => item.Value);

        HourlyRevenue.Clear();
        foreach (var value in values)
        {
            HourlyRevenue.Add(new RevenueChartItem
            {
                Label = value.Label,
                Value = value.Value,
                BarWidthFraction = max > 0 ? Math.Max(2, (double)(value.Value / max) * 220) : 2
            });
        }
    }

    private void RefreshAlerts(AppContext context)
    {
        RecentAlerts.Clear();
        var slowOrders = context.Orders.Count(order =>
            order.Status == OrderStatus.Active && DateTime.Now - order.CreatedAt > TimeSpan.FromMinutes(30));
        if (slowOrders > 0) RecentAlerts.Add($"⏱️ {slowOrders} đơn đã phục vụ trên 30 phút");
        if (context.ReadyDishCount > 0) RecentAlerts.Add($"🍽️ {context.ReadyDishCount} món đang chờ phục vụ");
        if (context.NeedsClearingCount > 0) RecentAlerts.Add($"🧹 {context.NeedsClearingCount} bàn đang chờ dọn");
        if (context.ReservedCount > 0) RecentAlerts.Add($"🕒 {context.ReservedCount} bàn đã được đặt trước");
        var unavailable = context.MenuItems.Count(item => item.OutOfStock || !item.Available);
        if (unavailable > 0) RecentAlerts.Add($"⚠️ {unavailable} món đang hết hàng/ngừng phục vụ");
        if (RecentAlerts.Count == 0) RecentAlerts.Add("✅ Không có cảnh báo vận hành");
    }
}
