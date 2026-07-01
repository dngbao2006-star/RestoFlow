using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class InvoiceManagementPage : ContentPage
{
    private string _searchText = string.Empty;
    private bool _isObserving;
    private Invoice? _selectedInvoice;

    public ObservableCollection<Invoice> FilteredInvoices { get; private set; } = new();
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        private set { _selectedInvoice = value; OnPropertyChanged(); }
    }

    public InvoiceManagementPage()
    {
        InitializeComponent();
        BindingContext = this;
        RefreshFilterOptions();
        RefreshInvoices();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObserving)
        {
            AppContext.Instance.Invoices.CollectionChanged += OnInvoicesChanged;
            _isObserving = true;
        }
        RefreshFilterOptions();
        RefreshInvoices();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObserving)
        {
            AppContext.Instance.Invoices.CollectionChanged -= OnInvoicesChanged;
            _isObserving = false;
        }
        InvoiceDetailModal.IsVisible = false;
        base.OnNavigatingFrom(args);
    }

    private void OnInvoicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshFilterOptions();
        RefreshInvoices();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;
        RefreshInvoices();
    }

    private void OnFilterChanged(object? sender, EventArgs e) => RefreshInvoices();

    private void RefreshFilterOptions()
    {
        var invoices = AppContext.Instance.Invoices;
        TableFilter.ItemsSource = new[] { "Tất cả" }
            .Concat(invoices.Select(invoice => invoice.TableNumber).Distinct().OrderBy(number => number).Select(number => $"Bàn {number}"))
            .ToList();
        StaffFilter.ItemsSource = new[] { "Tất cả" }
            .Concat(invoices.Select(invoice => invoice.ServerName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name))
            .ToList();
        PaymentFilter.ItemsSource = new[] { "Tất cả", "Tiền mặt", "Chuyển khoản QR" };
        ValueSortFilter.ItemsSource = new[] { "Mới nhất", "Trị giá tăng dần", "Trị giá giảm dần" };
    }

    private void RefreshInvoices()
    {
        var query = AppContext.Instance.Invoices.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
            query = query.Where(invoice => invoice.Id.ToString().Contains(_searchText.TrimStart('#'), StringComparison.OrdinalIgnoreCase));

        var tableText = TableFilter.Text?.Trim() ?? string.Empty;
        if (!IsAllOrEmpty(tableText))
        {
            var digits = new string(tableText.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digits))
                query = query.Where(invoice => invoice.TableNumber.ToString().Contains(digits, StringComparison.OrdinalIgnoreCase));
        }

        var staffText = StaffFilter.Text?.Trim() ?? string.Empty;
        if (!IsAllOrEmpty(staffText))
            query = query.Where(invoice => invoice.ServerName.Contains(staffText, StringComparison.OrdinalIgnoreCase));

        var paymentText = PaymentFilter.Text?.Trim() ?? string.Empty;
        if (!IsAllOrEmpty(paymentText))
            query = query.Where(invoice => invoice.PaymentMethodDisplay.Contains(paymentText, StringComparison.OrdinalIgnoreCase));

        query = ValueSortFilter.Text switch
        {
            "Trị giá tăng dần" => query.OrderBy(invoice => invoice.Total),
            "Trị giá giảm dần" => query.OrderByDescending(invoice => invoice.Total),
            _ => query.OrderByDescending(invoice => invoice.CreatedAt)
        };

        FilteredInvoices = new ObservableCollection<Invoice>(query);
        OnPropertyChanged(nameof(FilteredInvoices));
    }

    private static bool IsAllOrEmpty(string value)
        => string.IsNullOrWhiteSpace(value) || value.Equals("Tất cả", StringComparison.OrdinalIgnoreCase);

    private void OnViewInvoiceClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Invoice invoice }) return;
        SelectedInvoice = invoice;
        InvoiceDetailModal.IsVisible = true;
    }

    private void OnCloseDetailClicked(object sender, EventArgs e) => InvoiceDetailModal.IsVisible = false;

    private async void OnPrintInvoiceClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Invoice invoice })
            await OpenPrintSafelyAsync(invoice);
    }

    private async void OnPrintSelectedClicked(object sender, EventArgs e)
    {
        if (SelectedInvoice != null)
            await OpenPrintSafelyAsync(SelectedInvoice);
    }

    private async Task OpenPrintSafelyAsync(Invoice invoice)
    {
        try
        {
            await InvoiceDocumentService.OpenPrintableInvoiceAsync(invoice);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể mở bản in: {ex.Message}", "Đóng");
        }
    }
}
