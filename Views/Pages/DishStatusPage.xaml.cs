using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Constants;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class DishStatusPage : ContentPage
{
    private DishStatus? _selectedStatusFilter;

    private FirebaseService firebase = new FirebaseService();

    public ObservableCollection<DishReady> ReadyDishList { get; set; }
        = new ObservableCollection<DishReady>();

    public bool IsPendingSelected => _selectedStatusFilter == DishStatus.Pending;
    public bool IsPreparingSelected => _selectedStatusFilter == DishStatus.Preparing;
    public bool IsReadySelected => _selectedStatusFilter == DishStatus.Ready;
    public bool IsServedSelected => _selectedStatusFilter == DishStatus.Served;

    public DishStatusPage()
    {
        InitializeComponent();
        BindingContext = this;

        AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
    }

    private void OnAppContextPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
        OnPropertyChanged(nameof(FilteredOrders));
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        await LoadDishReady();

        MainThread.BeginInvokeOnMainThread(RefreshPage);
    }

    private async Task LoadDishReady()
    {
        ReadyDishList.Clear();

        var data = await firebase.GetDishReadyAsync();

        foreach (var item in data)
        {
            ReadyDishList.Add(item);
        }

        OnPropertyChanged(nameof(ReadyDishList));
        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
    }

    private void RefreshPage()
    {
        ApplyFilters();

        OnPropertyChanged(nameof(HasReadyItems));
        OnPropertyChanged(nameof(ReadyItemsCount));
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(ReadyDishList));
    }

    public bool HasReadyItems => ReadyDishList.Count > 0;

    public int ReadyItemsCount => ReadyDishList.Count;

    public int PendingDishCount => AppContext.Instance.PendingDishCount;
    public int PreparingDishCount => AppContext.Instance.PreparingDishCount;
    public int ReadyDishCount => ReadyDishList.Count;
    public int ServedDishCount => AppContext.Instance.ServedDishCount;

    public IEnumerable<Order> FilteredOrders
    {
        get
        {
            // CHỈ hiển thị order đã có items (đã submit lên bếp)
            // Order draft (chưa gửi) không nằm trong AppContext.Orders
            // nhưng thêm bộ lọc an toàn: bỏ qua order rỗng
            var orders = AppContext.Instance.Orders
                .Where(o => o.Items.Count > 0)
                .AsEnumerable();

            if (_selectedStatusFilter != null)
            {
                orders = orders.Where(o =>
                    o.Items.Any(item => item.Status == _selectedStatusFilter));
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
            _selectedStatusFilter = null;
        else
            _selectedStatusFilter = newStatus;

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

    private async void OnServeClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not DishReady item)
            return;

        try
        {
            // Update OrderItem status to Served based on OrderId and MenuItemId
            await firebase.UpdateOrderItemStatusByOrderAndMenuItemAsync(item.OrderId, item.MenuItemId, DishStatus.Served);

            // Update AppContext Orders collection
            var order = AppContext.Instance.Orders.FirstOrDefault(o => o.Id == item.OrderId);
            if (order != null)
            {
                var orderItem = order.Items.FirstOrDefault(oi => oi.MenuItemId == item.MenuItemId);
                if (orderItem != null)
                {
                    orderItem.Status = DishStatus.Served;
                }
            }

            // Delete DishReady
            await firebase.DeleteDishReadyAsync(item.Id);

            // Remove from UI immediately
            ReadyDishList.Remove(item);

            // Update UI properties immediately
            OnPropertyChanged(nameof(ReadyDishList));
            OnPropertyChanged(nameof(HasReadyItems));
            OnPropertyChanged(nameof(ReadyItemsCount));
            OnPropertyChanged(nameof(ReadyDishCount));
            OnPropertyChanged(nameof(PendingDishCount));
            OnPropertyChanged(nameof(PreparingDishCount));
            OnPropertyChanged(nameof(ServedDishCount));
            OnPropertyChanged(nameof(FilteredOrders));
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Lỗi", $"Không thể cập nhật trạng thái: {ex.Message}", "OK");
            });
        }
    }
}