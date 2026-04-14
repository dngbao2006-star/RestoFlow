using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Constants;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class DishStatusPage : ContentPage
{
    private DishStatus? _selectedStatusFilter;
    // Đã gỡ bỏ _selectedTableIdFilter để bộ lọc luôn áp dụng lên toàn bộ danh sách

    public bool IsPendingSelected => _selectedStatusFilter == DishStatus.Pending;
    public bool IsPreparingSelected => _selectedStatusFilter == DishStatus.Preparing;
    public bool IsReadySelected => _selectedStatusFilter == DishStatus.Ready;
    public bool IsServedSelected => _selectedStatusFilter == DishStatus.Served;

    public DishStatusPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Subscribe to AppContext changes
        AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
    }

    private void OnAppContextPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(ReadyItems));
        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
        OnPropertyChanged(nameof(FilteredOrders));
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        MainThread.BeginInvokeOnMainThread(RefreshPage);
    }

    private void RefreshPage()
    {
        // Đã xóa block code hiển thị DisplayAlert khó chịu "Hoàn thành đơn hàng"
        ApplyFilters();
        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(ReadyItems));
    }

    public bool HasReadyItems => AppContext.Instance.ReadyDishCount > 0;

    public int ReadyItemsCount => AppContext.Instance.ReadyItems.Count();

    public int PendingDishCount => AppContext.Instance.PendingDishCount;
    public int PreparingDishCount => AppContext.Instance.PreparingDishCount;
    public int ReadyDishCount => AppContext.Instance.ReadyDishCount;
    public int ServedDishCount => AppContext.Instance.ServedDishCount;

    public IEnumerable<OrderItem> ReadyItems => AppContext.Instance.ReadyItems;

    public IEnumerable<Order> FilteredOrders
    {
        get
        {
            var orders = AppContext.Instance.Orders.AsEnumerable();

            // Lọc chính xác trạng thái trên toàn bộ Orders, không bị chặn bởi table ID ẩn nữa
            if (_selectedStatusFilter != null)
            {
                orders = orders.Where(o => o.Items.Any(item => item.Status == _selectedStatusFilter));
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

        if (_selectedStatusFilter == newStatus)
        {
            _selectedStatusFilter = null;
        }
        else
        {
            _selectedStatusFilter = newStatus;
        }

        OnPropertyChanged(nameof(IsPendingSelected));
        OnPropertyChanged(nameof(IsPreparingSelected));
        OnPropertyChanged(nameof(IsReadySelected));
        OnPropertyChanged(nameof(IsServedSelected));

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

        orderItem.Status = DishStatus.Served;

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
}