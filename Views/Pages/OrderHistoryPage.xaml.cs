using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class OrderHistoryPage : ContentPage
{
    private string _searchText = "";
    private string _selectedTimeFilter = "All";
    private Button? _selectedFilterButton;

    public OrderHistoryPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
        InitializeFilteredOrders();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        InitializeFilteredOrders();
    }

    private void InitializeFilteredOrders()
    {
        AppContext.Instance.FilteredOrders.Clear();
        foreach (var order in AppContext.Instance.OrderHistory)
        {
            AppContext.Instance.FilteredOrders.Add(order);
        }
        UpdateStats();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.ToLower() ?? "";
        ApplyFilters();
    }

    private void OnTimeFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        // Deselect previous button
        if (_selectedFilterButton != null)
        {
            _selectedFilterButton.Opacity = 0.6;
        }

        // Select new button
        _selectedFilterButton = button;
        button.Opacity = 1.0;

        var filter = button.Text;
        _selectedTimeFilter = filter switch
        {
            "Hôm nay" => "Today",
            "Tuần này" => "Week",
            "Tháng này" => "Month",
            _ => "All"
        };

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        AppContext.Instance.FilteredOrders.Clear();

        var now = DateTime.Now;
        var startDate = _selectedTimeFilter switch
        {
            "Today" => now.Date,
            "Week" => now.Date.AddDays(-(int)now.DayOfWeek),
            "Month" => new DateTime(now.Year, now.Month, 1),
            _ => DateTime.MinValue
        };

        var filteredOrders = AppContext.Instance.OrderHistory
            .Where(order =>
            {
                // Time filter
                var dateMatch = _selectedTimeFilter == "All" || order.CreatedAt >= startDate;

                // Search filter
                var searchMatch = string.IsNullOrEmpty(_searchText) ||
                                 order.TableNumber.ToString().Contains(_searchText) ||
                                 (order.StaffName?.ToLower().Contains(_searchText) ?? false);

                return dateMatch && searchMatch;
            })
            .OrderByDescending(o => o.CreatedAt);

        foreach (var order in filteredOrders)
        {
            // Add IsExpanded property if not exists
            if (!order.IsExpanded)
            {
                order.IsExpanded = false;
            }
            AppContext.Instance.FilteredOrders.Add(order);
        }

        UpdateStats();
    }

    private void UpdateStats()
    {
        // Stats update handled by binding
    }

    private void OnOrderTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Order order)
            return;

        order.IsExpanded = !order.IsExpanded;
    }

    private async void OnViewInvoiceClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not Order order)
            return;

        // Navigate to invoice view (you can create an InvoicePage or show modal)
        await DisplayAlert("Hóa đơn", $"Bàn {order.TableNumber}\nTổng: {order.TotalDisplay}\nNgày: {order.CreatedAtDisplay}", "Đóng");
    }

    private async void OnPrintInvoiceClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not Order order)
            return;

        var confirm = await DisplayAlert("In hóa đơn", $"In hóa đơn bàn {order.TableNumber}?", "Có", "Không");
        if (confirm)
        {
            await DisplayAlert("Thành công", "Hóa đơn đã gửi đến máy in", "OK");
        }
    }
}
