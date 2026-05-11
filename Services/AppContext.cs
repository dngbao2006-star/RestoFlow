using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Services;

public class AppContext : ObservableObject
{
    private static AppContext? _instance;

    public static AppContext Instance => _instance ?? throw new InvalidOperationException("AppContext chưa được khởi tạo bởi DI.");

    private readonly IDataStore _dataStore;
    private Staff? _currentUser;
    private Order? _selectedOrder;
    private Table? _selectedTable;
    internal static string? BaseDirectory;

    public AppContext(IDataStore dataStore)
    {
        _instance = this;
        _dataStore = dataStore;

        Notifications.CollectionChanged += (_, _) => RefreshBadges();
        ChatMessages.CollectionChanged += (_, _) => RefreshBadges();
        Orders.CollectionChanged += (_, _) => RefreshBadges();
    }

    public async Task InitializeAsync()
    {
        await _dataStore.SeedAsync(this);
        SelectedOrder = Orders.FirstOrDefault();
        RefreshBadges();
    }

    public Staff? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (SetProperty(ref _currentUser, value))
            {
                OnPropertyChanged(nameof(IsStaff));
                OnPropertyChanged(nameof(IsManager));
                RefreshBadges();
            }
        }
    }

    public bool IsStaff => CurrentUser?.Role == StaffRole.Staff;
    public bool IsManager => CurrentUser?.Role == StaffRole.Manager;

    public ObservableCollection<Order> Orders { get; } = new();
    public ObservableCollection<Order> FilteredOrders { get; } = new();
    public ObservableCollection<Table> Tables { get; } = new();
    public ObservableCollection<FoodItem> MenuItems { get; } = new();
    public ObservableCollection<FoodItem> FilteredMenuItems { get; } = new();
    public ObservableCollection<Notification> Notifications { get; } = new();
    public ObservableCollection<ChatMessage> ChatMessages { get; } = new();
    public ObservableCollection<Order> OrderHistory { get; } = new();
    public ObservableCollection<Staff> StaffMembers { get; } = new();
    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueDaily { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueWeekly { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueMonthly { get; } = new();
    public ObservableCollection<DishRevenue> TopDishes { get; } = new();

    public Order? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public Table? SelectedTable
    {
        get => _selectedTable;
        set => SetProperty(ref _selectedTable, value);
    }

    public int UnreadNotifications => Notifications.Count(notification => !notification.Read);
    public int UnreadMessages => ChatMessages.Count(message => !message.IsSystem && !message.IsRead);
    public int ReadyDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Ready);

    public int PendingDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Pending);
    public int PreparingDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Preparing);
    public int ServedDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Served);

    public IEnumerable<OrderItem> ReadyItems => Orders.SelectMany(order => order.Items).Where(item => item.Status == DishStatus.Ready);

    public string StaffChatTitle => UnreadMessages > 0 ? $"Chat ({UnreadMessages})" : "Chat";
    public string DishStatusTitle => ReadyDishCount > 0 ? $"Món ăn ({ReadyDishCount})" : "Món ăn";
    public string NotificationsTitle => UnreadNotifications > 0 ? $"Thông báo ({UnreadNotifications})" : "Thông báo";

    public int AvailableCount => Tables.Count(table => table.Status == TableStatus.Available);
    public int OccupiedCount => Tables.Count(table => table.Status == TableStatus.Occupied);
    public int ReservedCount => Tables.Count(table => table.Status == TableStatus.Reserved);
    public int NeedsClearingCount => Tables.Count(table => table.Status == TableStatus.NeedsClearing);
    public int AvailableMenuItemCount => MenuItems.Count(item => !item.OutOfStock && item.Available);
    public int OutOfStockMenuItemCount => MenuItems.Count(item => item.OutOfStock || !item.Available);

    public IEnumerable<Table> GroundFloorTables => Tables.Where(table => table.Floor == "Ground Floor");
    public IEnumerable<Table> SecondFloorTables => Tables.Where(table => table.Floor == "Second Floor");
    public IEnumerable<Table> GardenTables => Tables.Where(table => table.Floor == "Garden");

    public string TodayRevenueDisplay => RevenueDaily.Count > 0 ? RevenueDaily[Math.Min(6, RevenueDaily.Count - 1)].ValueDisplay : Formatters.FormatCurrency(0);
    public string AvgPerDayDisplay => RevenueWeekly.Count > 0 ? Formatters.FormatCurrency(RevenueWeekly.Average(point => point.Value)) : Formatters.FormatCurrency(0);
    public string TotalRevenueDisplay => RevenueWeekly.Count > 0 ? Formatters.FormatCurrency(RevenueWeekly.Sum(point => point.Value)) : Formatters.FormatCurrency(0);
    public string AvgOrderValueDisplay => Formatters.FormatCurrency(250000);

    public decimal FilteredOrdersTotal => FilteredOrders.Sum(o => o.Total);
    public string FilteredOrdersTotalDisplay => Formatters.FormatCurrency(FilteredOrdersTotal);
    public int FilteredOrdersCount => FilteredOrders.Count;

    public void MarkNotificationsRead()
    {
        // TODO: [BACKEND] - Chỗ này gọi API cập nhật trạng thái đã đọc cho danh sách thông báo.
        foreach (var notification in Notifications)
        {
            notification.Read = true;
        }

        RefreshBadges();
    }

    public void RefreshBadges()
    {
        OnPropertyChanged(nameof(UnreadNotifications));
        OnPropertyChanged(nameof(UnreadMessages));
        OnPropertyChanged(nameof(ReadyDishCount));
        OnPropertyChanged(nameof(PendingDishCount));
        OnPropertyChanged(nameof(PreparingDishCount));
        OnPropertyChanged(nameof(ServedDishCount));
        OnPropertyChanged(nameof(ReadyItems));
        OnPropertyChanged(nameof(StaffChatTitle));
        OnPropertyChanged(nameof(DishStatusTitle));
        OnPropertyChanged(nameof(NotificationsTitle));
    }

    public void TransferOrder(int targetTableId)
    {
        if (SelectedOrder == null)
        {
            return;
        }

        var targetTable = Tables.FirstOrDefault(t => t.Id == targetTableId);
        if (targetTable == null)
        {
            return;
        }

        var currentTableId = SelectedOrder.TableId;

        // TODO: [BACKEND] - Chỗ này gọi API cập nhật chuyển bàn và đồng bộ trạng thái bàn liên quan.
        SelectedOrder.TableId = targetTable.Id;
        SelectedOrder.TableNumber = targetTable.Number;

        var currentTable = Tables.FirstOrDefault(t => t.Id == currentTableId);
        if (currentTable != null)
        {
            currentTable.CurrentOrderId = null;
            currentTable.Status = TableStatus.Available;
        }

        targetTable.Status = TableStatus.Occupied;
        targetTable.CurrentOrderId = SelectedOrder.Id;

        RefreshBadges();
    }

    public void MergeOrder(int targetOrderId)
    {
        if (SelectedOrder == null)
        {
            return;
        }

        var targetOrder = Orders.FirstOrDefault(o => o.Id == targetOrderId);
        if (targetOrder == null || targetOrder.Id == SelectedOrder.Id)
        {
            return;
        }

        // TODO: [BACKEND] - Chỗ này gọi API ghép đơn và cập nhật lại món/bàn trên server.
        foreach (var item in targetOrder.Items)
        {
            var existingItem = SelectedOrder.Items.FirstOrDefault(i => i.MenuItemId == item.MenuItemId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                SelectedOrder.Items.Add(new OrderItem
                {
                    MenuItemId = item.MenuItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Image = item.Image,
                    Status = item.Status,
                    Notes = item.Notes
                });
            }
        }

        var targetTable = Tables.FirstOrDefault(t => t.Id == targetOrder.TableId);
        if (targetTable != null)
        {
            targetTable.Status = TableStatus.Available;
            targetTable.CurrentOrderId = null;
        }

        Orders.Remove(targetOrder);

        SelectedOrder.NotifyItemsChanged();
        RefreshBadges();
    }


}
