using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class OrderHistoryPage : ContentPage
{
    private string _searchText = string.Empty;
    private string _selectedTimeFilter = "All";
    private Button? _selectedFilterButton;
    private bool _isObserving;
    private Invoice? _selectedInvoice;

    public ObservableCollection<Order> FilteredOrders { get; private set; } = new();
    public int FilteredOrdersCount => FilteredOrders.Count;
    public string FilteredOrdersTotalDisplay => Formatters.FormatCurrency(FilteredOrders.Sum(order => order.Total));
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        private set { _selectedInvoice = value; OnPropertyChanged(); }
    }

    public OrderHistoryPage()
    {
        InitializeComponent();
        BindingContext = this;
        ApplyFilters();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObserving)
        {
            AppContext.Instance.OrderHistory.CollectionChanged += OnHistoryChanged;
            AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
            _isObserving = true;
        }
        ApplyFilters();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObserving)
        {
            AppContext.Instance.OrderHistory.CollectionChanged -= OnHistoryChanged;
            AppContext.Instance.PropertyChanged -= OnAppContextPropertyChanged;
            _isObserving = false;
        }
        InvoiceDetailModal.IsVisible = false;
        base.OnNavigatingFrom(args);
    }

    private void OnHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilters();

    private void OnAppContextPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppContext.OrderItemsVersion)) ApplyFilters();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;
        ApplyFilters();
    }

    private void OnTimeFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;
        if (_selectedFilterButton != null) _selectedFilterButton.Opacity = 0.65;
        _selectedFilterButton = button;
        button.Opacity = 1;
        _selectedTimeFilter = button.CommandParameter?.ToString() ?? "All";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var now = DateTime.Now;
        var daysSinceMonday = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var startDate = _selectedTimeFilter switch
        {
            "Today" => now.Date,
            "Week" => now.Date.AddDays(-daysSinceMonday),
            "Month" => new DateTime(now.Year, now.Month, 1),
            _ => DateTime.MinValue
        };

        var items = AppContext.Instance.OrderHistory
            .Where(order => order.CreatedAt >= startDate)
            .Where(order => string.IsNullOrWhiteSpace(_searchText) ||
                order.TableNumber.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                order.ServerName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                order.Id.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        FilteredOrders = new ObservableCollection<Order>(items);
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(FilteredOrdersCount));
        OnPropertyChanged(nameof(FilteredOrdersTotalDisplay));
    }

    private void OnOrderTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Order order) order.IsExpanded = !order.IsExpanded;
    }

    private void OnViewInvoiceClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Order order }) return;
        SelectedInvoice = InvoiceDocumentService.FromOrder(order);
        InvoiceDetailModal.IsVisible = true;
    }

    private void OnCloseInvoiceClicked(object sender, EventArgs e) => InvoiceDetailModal.IsVisible = false;

    private async void OnPrintInvoiceClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Order order }) return;
        await OpenPrintSafelyAsync(InvoiceDocumentService.FromOrder(order));
    }

    private async void OnPrintSelectedInvoiceClicked(object sender, EventArgs e)
    {
        if (SelectedInvoice != null) await OpenPrintSafelyAsync(SelectedInvoice);
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
