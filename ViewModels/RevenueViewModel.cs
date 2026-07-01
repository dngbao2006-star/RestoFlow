using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using CommunityToolkit.Maui.Storage;

namespace AppManagermentRestaurant.ViewModels;

public class RevenueViewModel : ObservableObject
{
    private string _selectedPeriod = "week";
    private bool _isActive;
    private int _refreshScheduled;
    private List<Invoice> _currentInvoices = new();
    private string _netRevenueDisplay = Formatters.FormatCurrency(0);
    private string _grossRevenueDisplay = Formatters.FormatCurrency(0);
    private string _discountDisplay = Formatters.FormatCurrency(0);
    private string _averageOrderDisplay = Formatters.FormatCurrency(0);
    private string _cashRevenueDisplay = Formatters.FormatCurrency(0);
    private string _qrRevenueDisplay = Formatters.FormatCurrency(0);
    private string _comparisonDisplay = "0%";
    private Color _comparisonColor = Color.FromArgb("#6B7280");
    private int _totalOrdersCount;

    private Services.AppContext Ctx => Services.AppContext.Instance;

    public RevenueViewModel()
    {
        ExportCsvCommand = new Command(async () => await ExportCsvAsync());
        ExportTextCommand = new Command(async () => await ExportTextAsync());
        SaveCsvCommand = new Command(async () => await SaveCsvAsync());
        SaveTextCommand = new Command(async () => await SaveTextAsync());
        RefreshData();
    }

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnPropertyChanged(nameof(IsTodaySelected));
                OnPropertyChanged(nameof(IsWeekSelected));
                OnPropertyChanged(nameof(IsMonthSelected));
                OnPropertyChanged(nameof(IsYearSelected));
                OnPropertyChanged(nameof(PeriodLabel));
                RefreshData();
            }
        }
    }

    public bool IsTodaySelected => SelectedPeriod == "today";
    public bool IsWeekSelected => SelectedPeriod == "week";
    public bool IsMonthSelected => SelectedPeriod == "month";
    public bool IsYearSelected => SelectedPeriod == "year";
    public string PeriodLabel => SelectedPeriod switch
    {
        "today" => "Hôm nay",
        "month" => $"Tháng {DateTime.Now.Month}/{DateTime.Now.Year}",
        "year" => $"Năm {DateTime.Now.Year}",
        _ => "Tuần này"
    };

    public ObservableCollection<RevenueChartItem> ChartItems { get; } = new();
    public ObservableCollection<DishRevenue> TopDishes { get; } = new();
    public ObservableCollection<DishRevenue> TopSellingDishes { get; } = new();
    public bool HasNoTopDishes => TopDishes.Count == 0;
    public bool HasNoTopSellingDishes => TopSellingDishes.Count == 0;
    public string NetRevenueDisplay { get => _netRevenueDisplay; private set => SetProperty(ref _netRevenueDisplay, value); }
    public string GrossRevenueDisplay { get => _grossRevenueDisplay; private set => SetProperty(ref _grossRevenueDisplay, value); }
    public string DiscountDisplay { get => _discountDisplay; private set => SetProperty(ref _discountDisplay, value); }
    public string AverageOrderDisplay { get => _averageOrderDisplay; private set => SetProperty(ref _averageOrderDisplay, value); }
    public string CashRevenueDisplay { get => _cashRevenueDisplay; private set => SetProperty(ref _cashRevenueDisplay, value); }
    public string QrRevenueDisplay { get => _qrRevenueDisplay; private set => SetProperty(ref _qrRevenueDisplay, value); }
    public string ComparisonDisplay { get => _comparisonDisplay; private set => SetProperty(ref _comparisonDisplay, value); }
    public Color ComparisonColor { get => _comparisonColor; private set => SetProperty(ref _comparisonColor, value); }
    public int TotalOrdersCount { get => _totalOrdersCount; private set => SetProperty(ref _totalOrdersCount, value); }

    public ICommand ExportCsvCommand { get; }
    public ICommand ExportTextCommand { get; }
    public ICommand SaveCsvCommand { get; }
    public ICommand SaveTextCommand { get; }

    public void Activate()
    {
        if (_isActive) return;
        Ctx.Invoices.CollectionChanged += OnInvoicesChanged;
        Ctx.PropertyChanged += OnContextPropertyChanged;
        _isActive = true;
        RefreshData();
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        Ctx.Invoices.CollectionChanged -= OnInvoicesChanged;
        Ctx.PropertyChanged -= OnContextPropertyChanged;
        _isActive = false;
    }

    private void OnInvoicesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScheduleRefresh();

    private void OnContextPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Services.AppContext.OrderItemsVersion) or nameof(Services.AppContext.InvoiceRevenueDisplay))
            ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        if (Interlocked.Exchange(ref _refreshScheduled, 1) == 1) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try { RefreshData(); }
            finally { Interlocked.Exchange(ref _refreshScheduled, 0); }
        });
    }

    private void RefreshData()
    {
        var now = DateTime.Now;
        var range = RevenueAnalytics.GetRange(SelectedPeriod, now);
        var previousRange = RevenueAnalytics.GetPreviousRange(SelectedPeriod, now);
        _currentInvoices = RevenueAnalytics.FilterInvoices(Ctx.Invoices, range);
        var previousInvoices = RevenueAnalytics.FilterInvoices(Ctx.Invoices, previousRange);

        var net = _currentInvoices.Sum(invoice => invoice.Total);
        var discount = _currentInvoices.Sum(invoice => invoice.Discount);
        var gross = net + discount;
        var previousNet = previousInvoices.Sum(invoice => invoice.Total);

        NetRevenueDisplay = Formatters.FormatCurrency(net);
        GrossRevenueDisplay = Formatters.FormatCurrency(gross);
        DiscountDisplay = Formatters.FormatCurrency(discount);
        TotalOrdersCount = _currentInvoices.Count;
        AverageOrderDisplay = Formatters.FormatCurrency(_currentInvoices.Count > 0 ? net / _currentInvoices.Count : 0);
        CashRevenueDisplay = Formatters.FormatCurrency(_currentInvoices.Where(invoice => invoice.PaymentMethod == PaymentMethod.Cash).Sum(invoice => invoice.Total));
        QrRevenueDisplay = Formatters.FormatCurrency(_currentInvoices.Where(invoice => invoice.PaymentMethod == PaymentMethod.Qr).Sum(invoice => invoice.Total));
        SetComparison(net, previousNet);

        ChartItems.Clear();
        foreach (var point in RevenueAnalytics.BuildChart(_currentInvoices, SelectedPeriod, now))
            ChartItems.Add(point);

        TopDishes.Clear();
        foreach (var dish in RevenueAnalytics.BuildTopDishes(_currentInvoices))
            TopDishes.Add(dish);
        OnPropertyChanged(nameof(HasNoTopDishes));

        TopSellingDishes.Clear();
        foreach (var dish in RevenueAnalytics.BuildTopSellingDishes(_currentInvoices))
            TopSellingDishes.Add(dish);
        OnPropertyChanged(nameof(HasNoTopSellingDishes));
    }

    private void SetComparison(decimal current, decimal previous)
    {
        if (previous <= 0)
        {
            ComparisonDisplay = current > 0 ? "Mới phát sinh" : "0%";
            ComparisonColor = current > 0 ? Color.FromArgb("#16A34A") : Color.FromArgb("#6B7280");
            return;
        }

        var percentage = (double)((current - previous) / previous);
        ComparisonDisplay = $"{percentage:+0%;-0%;0%}";
        ComparisonColor = percentage >= 0 ? Color.FromArgb("#16A34A") : Color.FromArgb("#DC2626");
    }

    private async Task ExportCsvAsync()
    {
        if (!await EnsureHasInvoicesAsync()) return;
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, BuildFileName("csv"));
            await File.WriteAllTextAsync(path, BuildCsv(), new UTF8Encoding(true));
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Báo cáo doanh thu - {PeriodLabel}",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex) { await ShowErrorAsync(ex); }
    }

    private async Task ExportTextAsync()
    {
        if (!await EnsureHasInvoicesAsync()) return;
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, BuildFileName("txt"));
            await File.WriteAllTextAsync(path, BuildTextReport(), Encoding.UTF8);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Báo cáo doanh thu - {PeriodLabel}",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex) { await ShowErrorAsync(ex); }
    }

    private async Task SaveCsvAsync()
    {
        if (!await EnsureHasInvoicesAsync()) return;
        try
        {
            using var stream = new MemoryStream(new UTF8Encoding(true).GetBytes(BuildCsv()));
            var result = await FileSaver.Default.SaveAsync(BuildFileName("csv"), stream, CancellationToken.None);
            if (result.IsSuccessful)
                await Shell.Current.DisplayAlert("Thành công", $"Đã lưu file tại:\n{result.FilePath}", "OK");
        }
        catch (Exception ex) { await ShowErrorAsync(ex); }
    }

    private async Task SaveTextAsync()
    {
        if (!await EnsureHasInvoicesAsync()) return;
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(BuildTextReport()));
            var result = await FileSaver.Default.SaveAsync(BuildFileName("txt"), stream, CancellationToken.None);
            if (result.IsSuccessful)
                await Shell.Current.DisplayAlert("Thành công", $"Đã lưu file tại:\n{result.FilePath}", "OK");
        }
        catch (Exception ex) { await ShowErrorAsync(ex); }
    }

    private string BuildCsv()
    {
        static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var lines = new List<string> { "Mã HĐ,Bàn,Nhân viên,Doanh thu,Giảm giá,Phương thức,Ngày tạo" };
        lines.AddRange(_currentInvoices.Select(invoice => string.Join(",",
            invoice.Id,
            invoice.TableNumber,
            Escape(invoice.ServerName),
            invoice.Total.ToString(CultureInfo.InvariantCulture),
            invoice.Discount.ToString(CultureInfo.InvariantCulture),
            invoice.PaymentMethod == PaymentMethod.Cash ? "Tiền mặt" : "QR Code",
            invoice.CreatedAt.ToString("dd/MM/yyyy HH:mm"))));
        return string.Join(Environment.NewLine, lines);
    }

    private string BuildTextReport()
    {
        var net = _currentInvoices.Sum(invoice => invoice.Total);
        var discount = _currentInvoices.Sum(invoice => invoice.Discount);
        var gross = net + discount;
        var average = _currentInvoices.Count > 0 ? net / _currentInvoices.Count : 0;
        var report = new List<string>
        {
            "BÁO CÁO DOANH THU - THE GOLDEN PLATE",
            $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"Kỳ báo cáo: {PeriodLabel}",
            "",
            $"Doanh thu trước giảm giá: {Formatters.FormatCurrency(gross)}",
            $"Tổng giảm giá: {Formatters.FormatCurrency(discount)}",
            $"Doanh thu: {Formatters.FormatCurrency(net)}",
            $"Tổng hóa đơn: {_currentInvoices.Count}",
            $"Giá trị trung bình/đơn: {Formatters.FormatCurrency(average)}",
            $"So với kỳ trước: {ComparisonDisplay}",
            "",
            "MÓN CÓ DOANH THU CAO NHẤT"
        };
        report.AddRange(TopDishes.Select((dish, index) =>
            $"{index + 1}. {dish.Name}: {dish.RevenueDisplay} ({dish.ShareDisplay})"));
        report.Add("");
        report.Add("CHI TIẾT HÓA ĐƠN");
        report.AddRange(_currentInvoices.Select(invoice =>
            $"#{invoice.Id} - Bàn {invoice.TableNumber} - {invoice.TotalDisplay} - {invoice.PaymentMethodDisplay} - {invoice.CreatedAt:dd/MM/yyyy HH:mm}"));
        return string.Join(Environment.NewLine, report);
    }

    private async Task<bool> EnsureHasInvoicesAsync()
    {
        if (_currentInvoices.Count > 0) return true;
        await Shell.Current.DisplayAlert("Thông báo", $"Không có hóa đơn trong kỳ {PeriodLabel.ToLowerInvariant()}.", "OK");
        return false;
    }

    private string BuildFileName(string extension)
        => $"bao_cao_{SelectedPeriod}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";

    private static Task ShowErrorAsync(Exception ex)
        => Shell.Current.DisplayAlert("Lỗi", $"Không thể tạo báo cáo: {ex.Message}", "OK");
}
