using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class DishStatusPage : ContentPage
{
    private DishStatus? _selectedStatusFilter;
    private int? _selectedTableIdFilter;

    public DishStatusPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Subscribe to AppContext changes
        AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
    }

    private void OnAppContextPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Refresh UI when AppContext properties change
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(ReadyItems));
        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(SelectedTable));
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        MainThread.BeginInvokeOnMainThread(RefreshPage);
    }

    private void RefreshPage()
    {
        // Auto-filter to selected table if available
        if (AppContext.Instance.SelectedTable != null)
        {
            _selectedTableIdFilter = AppContext.Instance.SelectedTable.Id;
        }

        ApplyFilters();
        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
        OnPropertyChanged(nameof(SelectedTable));
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(ReadyItems));

        // Auto-navigate back to table selection if all items for selected table are served
        if (AppContext.Instance.SelectedTable != null && _selectedTableIdFilter.HasValue)
        {
            var selectedTableOrders = AppContext.Instance.Orders.Where(o => o.TableId == _selectedTableIdFilter.Value);
            var hasPendingOrPreparing = selectedTableOrders.Any(o => o.Items.Any(i => i.Status == DishStatus.Pending || i.Status == DishStatus.Preparing));

            if (!hasPendingOrPreparing && selectedTableOrders.Any())
            {
                // All dishes done - update table status and show option to return
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var result = await DisplayAlert(
                        "Hoàn thành đơn hàng",
                        "Bàn đã xong. Quay lại để chọn bàn tiếp theo?",
                        "Có",
                        "Ở lại");

                    if (result)
                    {
                        // Reset selection and navigate back
                        AppContext.Instance.SelectedTable = null;
                        AppContext.Instance.SelectedOrder = null;
                        await Shell.Current.GoToAsync("staff/tables");
                    }
                });
            }
        }
    }

    public bool HasReadyItems => AppContext.Instance.ReadyDishCount > 0;

    public int ReadyItemsCount => AppContext.Instance.ReadyItems.Count();

    public int PendingDishCount => AppContext.Instance.PendingDishCount;
    public int PreparingDishCount => AppContext.Instance.PreparingDishCount;
    public int ReadyDishCount => AppContext.Instance.ReadyDishCount;
    public int ServedDishCount => AppContext.Instance.ServedDishCount;

    public IEnumerable<OrderItem> ReadyItems => AppContext.Instance.ReadyItems;

    public Table SelectedTable => AppContext.Instance.SelectedTable;

    public IEnumerable<Order> FilteredOrders
    {
        get
        {
            var orders = AppContext.Instance.Orders.AsEnumerable();

            // Filter by status if selected
            if (_selectedStatusFilter != null)
            {
                orders = orders.Where(o => o.Items.Any(item => item.Status == _selectedStatusFilter));
            }

            // Filter by selected table
            if (_selectedTableIdFilter.HasValue)
            {
                orders = orders.Where(o => o.TableId == _selectedTableIdFilter.Value);
            }

            return orders;
        }
    }

    private void OnStatusCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string statusStr)
            return;

        DishStatus? newStatus = statusStr switch
        {
            "Pending" => DishStatus.Pending,
            "Preparing" => DishStatus.Preparing,
            "Ready" => DishStatus.Ready,
            "Served" => DishStatus.Served,
            _ => null
        };

        // Toggle filter: if same status clicked, remove filter
        if (_selectedStatusFilter == newStatus)
        {
            _selectedStatusFilter = null;
        }
        else
        {
            _selectedStatusFilter = newStatus;
        }

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        OnPropertyChanged(nameof(FilteredOrders));
    }

    private void OnServeClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not OrderItem orderItem)
            return;

        // Update status to Served
        orderItem.Status = DishStatus.Served;

        // Notify UI with all relevant properties
        AppContext.Instance.RefreshBadges();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(PendingDishCount));
            OnPropertyChanged(nameof(PreparingDishCount));
            OnPropertyChanged(nameof(ReadyDishCount));
            OnPropertyChanged(nameof(ServedDishCount));
            OnPropertyChanged(nameof(FilteredOrders));
            OnPropertyChanged(nameof(ReadyItems));
            OnPropertyChanged(nameof(HasReadyItems));
            OnPropertyChanged(nameof(ReadyItemsCount));
            RefreshPage();
        });
    }

    private async void OnBackToTablesClicked(object sender, EventArgs e)
    {
        AppContext.Instance.SelectedTable = null;
        AppContext.Instance.SelectedOrder = null;
        _selectedTableIdFilter = null;
        await Shell.Current.GoToAsync("staff/tables");
    }

    private void OnClearTableFilterClicked(object sender, EventArgs e)
    {
        AppContext.Instance.SelectedTable = null;
        _selectedTableIdFilter = null;
        RefreshPage();
    }
}