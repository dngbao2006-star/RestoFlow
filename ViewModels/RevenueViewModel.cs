using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using CommunityToolkit.Maui.Storage;

namespace AppManagermentRestaurant.ViewModels;

public class RevenueViewModel : ObservableObject
{
    private string _selectedPeriod = "week";

    public RevenueViewModel()
    {
        SelectPeriodCommand = new Command<string>(SelectPeriod);
        ExportCsvCommand = new Command(async () => await ExportCsvAsync());
        ExportTextCommand = new Command(async () => await ExportTextAsync());
        SaveCsvCommand = new Command(async () => await SaveCsvAsync());
        SaveTextCommand = new Command(async () => await SaveTextAsync());

        // Initialize with default period
        RefreshData();
    }

    // ── Bindable properties ────────────────────────────────────────

    private Services.AppContext Ctx => Services.AppContext.Instance;

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnPropertyChanged(nameof(IsTodaySelected));
                OnPropertyChanged(nameof(IsWeekSelected));
                OnPropertyChanged(nameof(IsYearSelected));
                RefreshData();
            }
        }
    }

    public bool IsTodaySelected => _selectedPeriod == "today";
    public bool IsWeekSelected => _selectedPeriod == "week";
    public bool IsYearSelected => _selectedPeriod == "year";

    public ObservableCollection<RevenueChartItem> ChartItems { get; } = new();
    public ObservableCollection<DishRevenue> TopDishes => Ctx.TopDishes;

    // ── KPI properties ─────────────────────────────────────────────

    public string TotalRevenueDisplay => Ctx.TotalRevenueDisplay;
    public string AvgPerDayDisplay => Ctx.AvgPerDayDisplay;
    public int TotalOrdersCount => Ctx.TotalOrdersCount;
    public string AvgOrderValueDisplay => Ctx.AvgOrderValueDisplay;

    // ── Commands ───────────────────────────────────────────────────

    public ICommand SelectPeriodCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportTextCommand { get; }
    public ICommand SaveCsvCommand { get; }
    public ICommand SaveTextCommand { get; }

    // ── Filter logic ───────────────────────────────────────────────

    private void SelectPeriod(string period)
    {
        SelectedPeriod = period;
    }

    private void RefreshData()
    {
        var source = _selectedPeriod switch
        {
            "today" => Ctx.RevenueDaily,
            "year" => Ctx.RevenueMonthly,
            _ => Ctx.RevenueWeekly
        };

        ChartItems.Clear();

        if (source.Count == 0) return;

        var maxValue = source.Max(p => p.Value);
        foreach (var point in source)
        {
            ChartItems.Add(new RevenueChartItem
            {
                Label = point.Label,
                Value = point.Value,
                BarWidthFraction = maxValue > 0 ? (double)(point.Value / maxValue) * 250 : 2
            });
        }

        // Refresh KPI displays based on period
        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(AvgPerDayDisplay));
        OnPropertyChanged(nameof(TotalOrdersCount));
        OnPropertyChanged(nameof(AvgOrderValueDisplay));
    }

    // ── Export CSV ──────────────────────────────────────────────────

    private async Task ExportCsvAsync()
    {
        try
        {
            var invoices = Ctx.Invoices;
            if (invoices.Count == 0)
            {
                await Shell.Current.DisplayAlert("Thông báo", "Không có hóa đơn nào để xuất.", "OK");
                return;
            }

            var lines = new List<string>
            {
                "Mã HĐ,Bàn,Nhân viên,Tổng tiền,Giảm giá,Phương thức,Ngày tạo"
            };

            foreach (var inv in invoices)
            {
                var method = inv.PaymentMethod == PaymentMethod.Cash ? "Tiền mặt" : "QR Code";
                lines.Add($"{inv.Id},{inv.TableNumber},{inv.ServerName},{inv.Total},{inv.Discount},{method},{inv.CreatedAt:dd/MM/yyyy HH:mm}");
            }

            var content = string.Join("\n", lines);
            var fileName = $"bao_cao_doanh_thu_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, content);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Xuất báo cáo CSV",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Lỗi", $"Không thể xuất file: {ex.Message}", "OK");
        }
    }

    // ── Export Text Report ──────────────────────────────────────────

    private async Task ExportTextAsync()
    {
        try
        {
            var invoices = Ctx.Invoices;
            var totalRevenue = invoices.Sum(i => i.Total);
            var totalDiscount = invoices.Sum(i => i.Discount);
            var avgOrder = invoices.Count > 0 ? totalRevenue / invoices.Count : 0;

            var report = new List<string>
            {
                "╔══════════════════════════════════════════╗",
                "║       BÁO CÁO DOANH THU GOLDEN PLATE    ║",
                "╚══════════════════════════════════════════╝",
                "",
                $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}",
                $"Kỳ báo cáo: {GetPeriodLabel()}",
                "",
                "── TỔNG QUAN ─────────────────────────────",
                $"  Tổng doanh thu:     {Formatters.FormatCurrency(totalRevenue)}",
                $"  Tổng giảm giá:      {Formatters.FormatCurrency(totalDiscount)}",
                $"  Doanh thu thuần:    {Formatters.FormatCurrency(totalRevenue - totalDiscount)}",
                $"  Tổng hóa đơn:       {invoices.Count}",
                $"  Giá trị TB/đơn:     {Formatters.FormatCurrency(avgOrder)}",
                "",
                "── TOP MÓN BÁN CHẠY ─────────────────────"
            };

            for (var i = 0; i < Ctx.TopDishes.Count; i++)
            {
                var dish = Ctx.TopDishes[i];
                report.Add($"  {i + 1}. {dish.Name,-28} {dish.RevenueDisplay,14}  ({dish.ShareDisplay})");
            }

            report.Add("");
            report.Add("── CHI TIẾT HÓA ĐƠN ─────────────────────");
            foreach (var inv in invoices)
            {
                var method = inv.PaymentMethod == PaymentMethod.Cash ? "Tiền mặt" : "QR Code";
                report.Add($"  #{inv.Id}  Bàn {inv.TableNumber,-4} {inv.TotalDisplay,14}  {method,-10}  {inv.CreatedAt:dd/MM/yyyy}");
            }

            report.Add("");
            report.Add("═══════════════════════════════════════════");

            var content = string.Join("\n", report);
            var fileName = $"bao_cao_doanh_thu_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, content);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Xuất báo cáo doanh thu",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Lỗi", $"Không thể xuất file: {ex.Message}", "OK");
        }
    }

    private string GetPeriodLabel() => _selectedPeriod switch
    {
        "today" => "Hôm nay",
        "year" => "Cả năm",
        _ => "Tuần này"
    };
}
