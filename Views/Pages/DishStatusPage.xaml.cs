using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Constants;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class DishStatusPage : ContentPage
{
    private DishStatus? _selectedStatusFilter;
    private bool _isObservingContext;
    private readonly HashSet<string> _servingItems = new();

    private readonly FirebaseService firebase = new();

    public ObservableCollection<DishReady> ReadyDishList { get; private set; }
        = new ObservableCollection<DishReady>();

    public bool IsPendingSelected => _selectedStatusFilter == DishStatus.Pending;
    public bool IsPreparingSelected => _selectedStatusFilter == DishStatus.Preparing;
    public bool IsReadySelected => _selectedStatusFilter == DishStatus.Ready;
    public bool IsServedSelected => _selectedStatusFilter == DishStatus.Served;

    public DishStatusPage()
    {
        InitializeComponent();
        BindingContext = this;

    }

    private void OnAppContextPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppContext.OrderItemsVersion):
                RefreshReadyDishes();
                OnPropertyChanged(nameof(FilteredOrders));
                break;
            case nameof(AppContext.PendingDishCount):
                OnPropertyChanged(nameof(PendingDishCount));
                break;
            case nameof(AppContext.PreparingDishCount):
                OnPropertyChanged(nameof(PreparingDishCount));
                break;
            case nameof(AppContext.ReadyDishCount):
                OnPropertyChanged(nameof(ReadyDishCount));
                OnPropertyChanged(nameof(HasReadyItems));
                OnPropertyChanged(nameof(ReadyItemsCount));
                break;
            case nameof(AppContext.ServedDishCount):
                OnPropertyChanged(nameof(ServedDishCount));
                break;
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObservingContext)
        {
            AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
            _isObservingContext = true;
        }
        RefreshReadyDishes();
        RefreshPage();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObservingContext)
        {
            AppContext.Instance.PropertyChanged -= OnAppContextPropertyChanged;
            _isObservingContext = false;
        }
        base.OnNavigatingFrom(args);
    }

    private void RefreshReadyDishes()
    {
        var readyDishes = new List<DishReady>();

        foreach (var order in AppContext.Instance.Orders.Where(order => order.Status == OrderStatus.Active))
        {
            foreach (var item in order.Items.Where(item => item.Status == DishStatus.Ready))
            {
                readyDishes.Add(new DishReady
                {
                    Id = item.Id,
                    DishName = item.Name,
                    Image = item.Image,
                    Quantity = item.Quantity,
                    TableId = order.TableId,
                    TableNumber = order.TableNumber,
                    Status = item.Status.ToString(),
                    CreatedAt = order.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    MenuItemId = item.MenuItemId,
                    OrderId = order.Id,
                    ParentOrder = order,
                    SourceItem = item
                });
            }
        }

        ReadyDishList = new ObservableCollection<DishReady>(readyDishes);

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

        var operationKey = $"{item.OrderId}:{item.Id}:{item.SourceItem?.FirebaseKey}";
        if (!_servingItems.Add(operationKey)) return;

        OrderItem? orderItem = null;
        try
        {
            var order = item.ParentOrder
                     ?? AppContext.Instance.Orders.FirstOrDefault(order => order.Id == item.OrderId);
            orderItem = item.SourceItem
                     ?? order?.Items.FirstOrDefault(source => source.Id == item.Id);
            if (order == null || orderItem == null)
                throw new InvalidOperationException("Không tìm thấy món trong đơn hàng.");

            button.IsEnabled = false;
            button.Text = "Đang cập nhật…";

            // Optimistic UI: respond immediately, then synchronize in background.
            orderItem.Status = DishStatus.Served;
            AppContext.Instance.NotifyOrderItemsChanged();

            await firebase.UpdateOrderItemStatusAsync(order, orderItem, DishStatus.Served)
                .WaitAsync(TimeSpan.FromSeconds(12));
        }
        catch (Exception ex)
        {
            if (orderItem != null)
            {
                orderItem.Status = DishStatus.Ready;
                AppContext.Instance.NotifyOrderItemsChanged();
            }
            await DisplayAlert("Lỗi", $"Không thể cập nhật trạng thái: {ex.Message}", "OK");
        }
        finally
        {
            _servingItems.Remove(operationKey);
        }
    }
}
