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
    private bool _isForceLoggingOut;
    private Task? _initializationTask;
    private int _orderItemsVersion;
    private int _lastUnreadNotifications = -1;
    private int _lastUnreadMessages = -1;
    private int _lastReadyDishCount = -1;
    private int _lastPendingDishCount = -1;
    private int _lastPreparingDishCount = -1;
    private int _lastServedDishCount = -1;
    private SystemConfiguration _systemConfiguration = new();
    internal static string? BaseDirectory;

    /// <summary>
    /// SessionId duy nhất của phiên đăng nhập hiện tại trên thiết bị này.
    /// Được sinh mới mỗi lần đăng nhập thành công.
    /// </summary>
    public string? CurrentSessionId { get; set; }

    /// <summary>
    /// Subscription lắng nghe thay đổi Presence node của chính user hiện tại.
    /// Dùng để phát hiện khi thiết bị khác đăng nhập cùng tài khoản.
    /// </summary>
    public IDisposable? SessionConflictSubscription { get; set; }

    public AppContext(IDataStore dataStore)
    {
        _instance = this;
        _dataStore = dataStore;

        Notifications.CollectionChanged += (_, _) => RefreshBadges();
        ChatMessages.CollectionChanged += (_, _) => RefreshBadges();
        Orders.CollectionChanged += (_, _) => NotifyOrderItemsChanged();
        Tables.CollectionChanged += (_, _) => RefreshTableMetrics();
        StaffMembers.CollectionChanged += (_, _) => RefreshStaffMetrics();
        Invoices.CollectionChanged += (_, _) => RefreshFinancials();
        RevenueDaily.CollectionChanged += (_, _) => RefreshFinancials();
        RevenueWeekly.CollectionChanged += (_, _) => RefreshFinancials();
        RevenueMonthly.CollectionChanged += (_, _) => RefreshFinancials();
    }

    public Task InitializeOnceAsync()
        => _initializationTask ??= InitializeCoreAsync();

    private async Task InitializeCoreAsync()
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
                OnPropertyChanged(nameof(IsAuthenticated));

                if (value != null)
                {
                    foreach (var message in ChatMessages.Where(message => message.SenderId == value.FirebaseUid))
                    {
                        message.IsRead = true;
                    }
                }

                RefreshBadges();
            }
        }
    }

    public bool IsStaff => CurrentUser?.Role == StaffRole.Staff;
    public bool IsManager => CurrentUser?.Role == StaffRole.Manager;
    public bool IsAuthenticated => CurrentUser != null;

    public ObservableCollection<Order> Orders { get; } = new();
    public ObservableCollection<Order> FilteredOrders { get; } = new();
    public ObservableCollection<Table> Tables { get; } = new();
    public ObservableCollection<FoodItem> MenuItems { get; } = new();
    public ObservableCollection<FoodItem> FilteredMenuItems { get; private set; } = new();
    public ObservableCollection<Notification> Notifications { get; } = new();
    public ObservableCollection<ChatMessage> ChatMessages { get; } = new();
    public ObservableCollection<Order> OrderHistory { get; } = new();
    public ObservableCollection<Staff> StaffMembers { get; } = new();
    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueDaily { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueWeekly { get; } = new();
    public ObservableCollection<RevenuePoint> RevenueMonthly { get; } = new();
    public ObservableCollection<DishRevenue> TopDishes { get; } = new();

    public SystemConfiguration SystemConfiguration
    {
        get => _systemConfiguration;
        set => SetProperty(ref _systemConfiguration, value ?? new SystemConfiguration());
    }

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

    public IEnumerable<Notification> VisibleNotifications => Notifications.Where(IsNotificationVisible);
    public int UnreadNotifications => VisibleNotifications.Count(notification => !HasCurrentUserRead(notification));
    public int UnreadMessages => ChatMessages.Count(message =>
        !message.IsSystem &&
        !message.IsRead &&
        message.SenderId != CurrentUser?.FirebaseUid);
    public int ReadyDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Ready);

    public int PendingDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Pending);
    public int PreparingDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Preparing);
    public int ServedDishCount => Orders.SelectMany(order => order.Items).Count(item => item.Status == DishStatus.Served);

    public IEnumerable<OrderItem> ReadyItems => Orders.SelectMany(order => order.Items).Where(item => item.Status == DishStatus.Ready);
    public int OrderItemsVersion => _orderItemsVersion;

    public string StaffChatTitle => UnreadMessages > 0 ? $"Chat ({UnreadMessages})" : "Chat";
    public string DishStatusTitle => ReadyDishCount > 0 ? $"Món ăn ({ReadyDishCount})" : "Món ăn";
    public string NotificationsTitle => UnreadNotifications > 0 ? $"Thông báo ({UnreadNotifications})" : "Thông báo";

    public int AvailableCount => Tables.Count(table => table.Status == TableStatus.Available);
    public int OccupiedCount => Tables.Count(table => table.Status == TableStatus.Occupied);
    public int ReservedCount => Tables.Count(table => table.Status == TableStatus.Reserved);
    public int NeedsClearingCount => Tables.Count(table => table.Status == TableStatus.NeedsClearing);
    public int OnlineStaffCount => StaffMembers.Count(staff => staff.IsOnline);
    public int AvailableMenuItemCount => MenuItems.Count(item => !item.OutOfStock && item.Available);
    public int OutOfStockMenuItemCount => MenuItems.Count(item => item.OutOfStock || !item.Available);

    public IEnumerable<Table> GroundFloorTables => Tables.Where(table => table.Floor == "Ground Floor");
    public IEnumerable<Table> SecondFloorTables => Tables.Where(table => table.Floor == "Second Floor");
    public IEnumerable<Table> GardenTables => Tables.Where(table => table.Floor == "Garden");

    public string TodayRevenueDisplay => Formatters.FormatCurrency(RevenueDaily.Sum(point => point.Value));
    public string AvgPerDayDisplay => RevenueWeekly.Count > 0 ? Formatters.FormatCurrency(RevenueWeekly.Average(point => point.Value)) : Formatters.FormatCurrency(0);
    public string TotalRevenueDisplay => RevenueWeekly.Count > 0 ? Formatters.FormatCurrency(RevenueWeekly.Sum(point => point.Value)) : Formatters.FormatCurrency(0);
    public int TotalOrdersCount => Invoices.Count;
    public string InvoiceRevenueDisplay => Formatters.FormatCurrency(Invoices.Sum(invoice => invoice.Total));
    public string AvgOrderValueDisplay => Invoices.Count > 0 ? Formatters.FormatCurrency(Invoices.Average(i => i.Total)) : Formatters.FormatCurrency(0);
    public string TotalDiscountDisplay => Invoices.Count > 0 ? Formatters.FormatCurrency(Invoices.Sum(i => i.Discount)) : Formatters.FormatCurrency(0);

    public decimal FilteredOrdersTotal => FilteredOrders.Sum(o => o.Total);
    public string FilteredOrdersTotalDisplay => Formatters.FormatCurrency(FilteredOrdersTotal);
    public int FilteredOrdersCount => FilteredOrders.Count;

    public void MarkNotificationsRead()
    {
        var uid = CurrentUser?.FirebaseUid;
        if (string.IsNullOrWhiteSpace(uid)) return;

        foreach (var notification in VisibleNotifications)
        {
            notification.ReadBy ??= new Dictionary<string, bool>();
            notification.ReadBy[uid] = true;
        }

        RefreshBadges();
    }

    public bool HasCurrentUserRead(Notification notification)
    {
        if (notification.Read) return true; // compatibility with legacy records
        var uid = CurrentUser?.FirebaseUid;
        return !string.IsNullOrWhiteSpace(uid)
            && notification.ReadBy?.TryGetValue(uid, out var hasRead) == true
            && hasRead;
    }

    private bool IsNotificationVisible(Notification notification)
    {
        if (string.IsNullOrWhiteSpace(notification.Audience) || notification.Audience == "All") return true;
        return CurrentUser?.Role switch
        {
            StaffRole.Staff => notification.Audience.Equals("Staff", StringComparison.OrdinalIgnoreCase),
            StaffRole.Manager => notification.Audience.Equals("Manager", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public void ReplaceFilteredMenuItems(IEnumerable<FoodItem> items)
    {
        FilteredMenuItems = new ObservableCollection<FoodItem>(items);
        OnPropertyChanged(nameof(FilteredMenuItems));
    }

    public void MarkChatMessagesRead()
    {
        foreach (var message in ChatMessages.Where(message => message.SenderId != CurrentUser?.FirebaseUid))
        {
            message.IsRead = true;
        }

        RefreshBadges();
    }

    public ChatMessage AddOrUpdateChatMessage(string firebaseKey, FirebaseChatMessage source)
    {
        var existing = ChatMessages.FirstOrDefault(message => message.FirebaseKey == firebaseKey);
        if (existing != null)
        {
            return existing;
        }

        var message = new ChatMessage
        {
            FirebaseKey = firebaseKey,
            Id = StringComparer.Ordinal.GetHashCode(firebaseKey),
            SenderId = source.SenderId,
            SenderName = source.SenderName,
            SenderRole = source.SenderRole,
            Message = source.Message,
            Timestamp = source.Timestamp,
            IsSystem = source.IsSystem,
            IsRead = source.SenderId == CurrentUser?.FirebaseUid
        };

        ChatMessages.Add(message);
        return message;
    }

    public void RefreshBadges()
    {
        var unreadNotifications = UnreadNotifications;
        if (_lastUnreadNotifications != unreadNotifications)
        {
            _lastUnreadNotifications = unreadNotifications;
            OnPropertyChanged(nameof(UnreadNotifications));
            OnPropertyChanged(nameof(NotificationsTitle));
        }

        var unreadMessages = UnreadMessages;
        if (_lastUnreadMessages != unreadMessages)
        {
            _lastUnreadMessages = unreadMessages;
            OnPropertyChanged(nameof(UnreadMessages));
            OnPropertyChanged(nameof(StaffChatTitle));
        }

        var ready = ReadyDishCount;
        if (_lastReadyDishCount != ready)
        {
            _lastReadyDishCount = ready;
            OnPropertyChanged(nameof(ReadyDishCount));
            OnPropertyChanged(nameof(ReadyItems));
            OnPropertyChanged(nameof(DishStatusTitle));
        }

        var pending = PendingDishCount;
        if (_lastPendingDishCount != pending)
        {
            _lastPendingDishCount = pending;
            OnPropertyChanged(nameof(PendingDishCount));
        }

        var preparing = PreparingDishCount;
        if (_lastPreparingDishCount != preparing)
        {
            _lastPreparingDishCount = preparing;
            OnPropertyChanged(nameof(PreparingDishCount));
        }

        var served = ServedDishCount;
        if (_lastServedDishCount != served)
        {
            _lastServedDishCount = served;
            OnPropertyChanged(nameof(ServedDishCount));
        }
    }

    public void NotifyOrderItemsChanged()
    {
        unchecked { _orderItemsVersion++; }
        OnPropertyChanged(nameof(OrderItemsVersion));
        RefreshBadges();
    }

    public void RefreshFinancials()
    {
        OnPropertyChanged(nameof(TodayRevenueDisplay));
        OnPropertyChanged(nameof(AvgPerDayDisplay));
        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(TotalOrdersCount));
        OnPropertyChanged(nameof(InvoiceRevenueDisplay));
        OnPropertyChanged(nameof(AvgOrderValueDisplay));
        OnPropertyChanged(nameof(TotalDiscountDisplay));
    }

    public void RefreshTableMetrics()
    {
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(OccupiedCount));
        OnPropertyChanged(nameof(ReservedCount));
        OnPropertyChanged(nameof(NeedsClearingCount));
        OnPropertyChanged(nameof(GroundFloorTables));
        OnPropertyChanged(nameof(SecondFloorTables));
        OnPropertyChanged(nameof(GardenTables));
    }

    public void RefreshStaffMetrics()
    {
        OnPropertyChanged(nameof(OnlineStaffCount));
    }

    /// <summary>
    /// Resolve tên nhân viên thực từ Firebase UID (ServerId).
    /// Tra cứu trong danh sách StaffMembers đã tải từ Firebase.
    /// Nếu không tìm thấy, trả về fallbackName.
    /// </summary>
    public string ResolveServerName(string? serverId, string? fallbackName = null)
    {
        if (!string.IsNullOrWhiteSpace(serverId))
        {
            var staff = StaffMembers.FirstOrDefault(s => s.FirebaseUid == serverId);
            if (staff != null)
                return staff.Name;
        }

        // Nếu không có ServerId hoặc không tìm thấy, dùng CurrentUser
        if (string.IsNullOrWhiteSpace(fallbackName) || fallbackName == "Nhân viên")
            return CurrentUser?.Name ?? fallbackName ?? "Nhân viên";

        return fallbackName;
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

    /// <summary>
    /// Force logout: hiển thị thông báo và đưa user về màn hình đăng nhập.
    /// Được gọi khi phát hiện SessionId trên Firebase không còn khớp với thiết bị này.
    /// </summary>
    public async Task ForceLogoutAsync()
    {
        if (_isForceLoggingOut) return;
        _isForceLoggingOut = true;

        try
        {
            // Hủy listener tránh loop
            SessionConflictSubscription?.Dispose();
            SessionConflictSubscription = null;
            CurrentSessionId = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Hiển thị thông báo cho user
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Phiên đăng nhập hết hạn",
                        "Tài khoản đã được đăng nhập trên thiết bị khác. Bạn sẽ được chuyển về màn hình đăng nhập.",
                        "OK");
                }

                // QUAN TRỌNG: Phải ngắt BindingContext của AppShell TRƯỚC khi set CurrentUser = null.
                // Nếu không, setter của CurrentUser sẽ fire OnPropertyChanged("IsStaff")
                // → binding system cố update UI element đã bị dispose → NullReferenceException.
                // Thứ tự này giống hệt pattern an toàn trong AppShell.HandleSignOutAsync().
                if (Application.Current?.MainPage is AppShell shell)
                {
                    shell.BindingContext = null;
                }

                // Chuyển về login TRƯỚC
                App.ShowLogin();

                // Set null SAU khi binding đã ngắt
                CurrentUser = null;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Session] Force logout failed safely: {ex}");
        }
        finally
        {
            _isForceLoggingOut = false;
        }
    }

}
