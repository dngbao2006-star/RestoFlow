using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Constants;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class OrderCreationPage : ContentPage
{
    private string _selectedFilter = "All";
    private string _searchText = "";
    private bool _isOrderReadOnly = false;
    private bool _isLoadingData = false;
    private readonly FirebaseService _firebase = new();
    private ObservableCollection<OrderItem> _existingItems = new();
    private ObservableCollection<OrderItem> _newItems = new();
    private ObservableCollection<Table> _activeTables = new();
    private Table _selectedActiveTable;
    private HashSet<int> _itemsSubmittedPreviously = new();

    /// <summary>
    /// Order nháp chỉ tồn tại local, CHƯA được thêm vào AppContext.Orders.
    /// Chỉ được đẩy vào AppContext.Orders + Firebase khi nhấn "Gửi lên bếp".
    /// </summary>
    private Order? _draftOrder;

    public OrderCreationPage()
    {
        InitializeComponent();
        BindingContext = this;
        InitializeFilteredMenuItems();
        UpdatePageState();
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await LoadPageDataAsync();
    }

    private async Task LoadPageDataAsync()
    {
        _isLoadingData = true;
        await Task.Yield();

        // 1. Tải bàn Occupied và sắp xếp (Bàn số nhỏ nhất đứng đầu)
        _activeTables.Clear();
        var occupiedTables = AppContext.Instance.Tables
            .Where(t => t.Status == TableStatus.Occupied)
            .OrderBy(t => t.Number)
            .ToList();

        foreach (var t in occupiedTables)
        {
            _activeTables.Add(t);
        }

        Table targetTable = null;

        // 2. Xét xem có bàn nào đang được truyền từ trang sơ đồ qua không
        if (AppContext.Instance.SelectedTable != null && AppContext.Instance.SelectedTable.Status == TableStatus.Occupied)
        {
            targetTable = _activeTables.FirstOrDefault(t => t.Id == AppContext.Instance.SelectedTable.Id);
        }
        else if (AppContext.Instance.SelectedOrder == null && AppContext.Instance.Orders.Any())
        {
            var activeOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Status == OrderStatus.Active);
            if (activeOrder != null)
            {
                targetTable = _activeTables.FirstOrDefault(t => t.Id == activeOrder.TableId);
            }
        }

        // 3. TỰ ĐỘNG CHỌN BÀN SỐ NHỎ NHẤT (Nếu không có bàn nào đang chọn)
        if (targetTable == null && _activeTables.Any())
        {
            targetTable = _activeTables.First();
        }

        // 4. Ép SelectedActiveTable cập nhật
        if (targetTable != null)
        {
            AppContext.Instance.SelectedTable = targetTable;
            _selectedActiveTable = targetTable;
            OnPropertyChanged(nameof(SelectedActiveTable));
            SwitchTableData(targetTable);
        }
        else
        {
            AppContext.Instance.SelectedOrder = null;
            _draftOrder = null;
            UpdatePageState();
        }

        _isLoadingData = false;
    }

    private void SwitchTableData(Table table)
    {
        // Reset draft khi chuyển bàn
        _draftOrder = null;

        // Tìm order đã tồn tại trên Firebase (đã submit trước đó)
        var existingOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.TableId == table.Id && o.Status == OrderStatus.Active);

        if (existingOrder != null)
        {
            // Order đã submit rồi → dùng order đó (có dữ liệu từ Firebase)
            AppContext.Instance.SelectedOrder = existingOrder;

            // Đánh dấu tất cả items hiện có là đã submit
            _itemsSubmittedPreviously.Clear();
            foreach (var item in existingOrder.Items)
            {
                _itemsSubmittedPreviously.Add(item.Id);
            }
        }
        else
        {
            // Bàn có khách nhưng CHƯA có order → tạo order nháp LOCAL
            var allOrders = AppContext.Instance.Orders.Concat(AppContext.Instance.OrderHistory).ToList();
            _draftOrder = new Order
            {
                Id = allOrders.Any() ? allOrders.Max(o => o.Id) + 1 : 1,
                TableId = table.Id,
                TableNumber = table.Number,
                Status = OrderStatus.Active,
                CreatedAt = DateTime.Now,
                ServerName = AppContext.Instance.CurrentUser?.Name ?? "Nhân viên",
                ServerId = AppContext.Instance.CurrentUser?.FirebaseUid ?? "",
                Items = new()
            };

            // KHÔNG thêm vào AppContext.Instance.Orders
            // Chỉ set SelectedOrder để UI binding hoạt động
            AppContext.Instance.SelectedOrder = _draftOrder;
            _itemsSubmittedPreviously.Clear();
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(SelectedOrder));
            OnPropertyChanged(nameof(ExistingOrderInfo));
            UpdatePageState();
        });
    }

    private void UpdatePageState()
    {
        if (AppContext.Instance.SelectedOrder == null)
        {
            _isOrderReadOnly = false;
            return;
        }

        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == AppContext.Instance.SelectedOrder.TableNumber);

        if (table == null)
        {
            _isOrderReadOnly = false;
            return;
        }

        _isOrderReadOnly = table.Status == TableStatus.Occupied || table.Status == TableStatus.Reserved;
        RefreshItemCollections();
    }

    public Order SelectedOrder => AppContext.Instance.SelectedOrder;
    public IEnumerable<Models.FoodItem> FilteredMenuItems => AppContext.Instance.FilteredMenuItems;
    public ObservableCollection<OrderItem> ExistingItems => _existingItems;
    public ObservableCollection<OrderItem> NewItems => _newItems;
    public ObservableCollection<Table> ActiveTables => _activeTables;

    public Table SelectedActiveTable
    {
        get => _selectedActiveTable;
        set
        {
            if (_selectedActiveTable != value)
            {
                _selectedActiveTable = value;
                OnPropertyChanged();

                if (value != null && !_isLoadingData)
                {
                    AppContext.Instance.SelectedTable = value;
                    SwitchTableData(value);
                }
            }
        }
    }

    public bool HasExistingItems => _existingItems.Count > 0;
    public bool HasNewItems => _newItems.Count > 0;
    public bool HasNoItems => (AppContext.Instance.SelectedOrder?.Items.Count ?? 0) == 0;

    public string ExistingOrderInfo
    {
        get
        {
            var order = AppContext.Instance.SelectedOrder;
            if (order == null) return "Đơn mới";
            return order.Items.Count > 0 ? $"Cập nhật ({order.Items.Count} món)" : "Đơn mới";
        }
    }

    private void RefreshItemCollections()
    {
        _existingItems.Clear();
        _newItems.Clear();

        var order = AppContext.Instance.SelectedOrder;
        if (order == null) return;

        foreach (var item in order.Items)
        {
            if (_itemsSubmittedPreviously.Contains(item.Id))
            {
                _existingItems.Add(item);
            }
            else
            {
                _newItems.Add(item);
            }
        }

        OnPropertyChanged(nameof(SelectedOrder));
        OnPropertyChanged(nameof(ExistingItems));
        OnPropertyChanged(nameof(NewItems));
        OnPropertyChanged(nameof(HasExistingItems));
        OnPropertyChanged(nameof(HasNewItems));
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(ExistingOrderInfo));
    }

    private void InitializeFilteredMenuItems()
    {
        ApplyFilters();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.ToLower() ?? "";
        ApplyFilters();
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var category = button.CommandParameter as string;
        if (string.IsNullOrEmpty(category)) return;

        _selectedFilter = category;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        AppContext.Instance.FilteredMenuItems.Clear();

        var items = AppContext.Instance.MenuItems
            .Where(item =>
            {
                var categoryMatch = _selectedFilter == "All" || item.Category == _selectedFilter;
                var searchMatch = string.IsNullOrEmpty(_searchText) ||
                                 item.Name.ToLower().Contains(_searchText);
                return categoryMatch && searchMatch;
            });

        foreach (var item in items)
        {
            AppContext.Instance.FilteredMenuItems.Add(item);
        }
    }

    private void OnFilterScrolled(object sender, ScrolledEventArgs e)
    {
        UpdateScrollButtonsVisibility();
    }

    private void OnFilterScrollViewSizeChanged(object sender, EventArgs e)
    {
        UpdateScrollButtonsVisibility();
    }

    private void UpdateScrollButtonsVisibility()
    {
        if (FilterScrollView.Width <= 0 || FilterScrollView.ContentSize.Width <= 0) return;

        double maxScroll = FilterScrollView.ContentSize.Width - FilterScrollView.Width;

        if (maxScroll <= 0)
        {
            BtnScrollLeft.IsVisible = false;
            BtnScrollRight.IsVisible = false;
        }
        else
        {
            BtnScrollLeft.IsVisible = FilterScrollView.ScrollX > 2;
            BtnScrollRight.IsVisible = FilterScrollView.ScrollX < (maxScroll - 2);
        }
    }

    private void OnScrollLeftClicked(object sender, EventArgs e)
    {
        double target = Math.Max(0, FilterScrollView.ScrollX - 150);
        FilterScrollView.ScrollToAsync(target, 0, true);
    }

    private void OnScrollRightClicked(object sender, EventArgs e)
    {
        double maxScroll = FilterScrollView.ContentSize.Width - FilterScrollView.Width;
        double target = Math.Min(maxScroll, FilterScrollView.ScrollX + 150);
        FilterScrollView.ScrollToAsync(target, 0, true);
    }

    private void OnAddMenuItemClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not Models.FoodItem menuItem || AppContext.Instance.SelectedOrder is null)
            return;

        if (menuItem.OutOfStock)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Thông báo", "Món này hiện đã hết hàng", "OK");
            });
            return;
        }

        AddItemToOrder(menuItem);
    }

    private void OnDeleteItemClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not OrderItem orderItem || AppContext.Instance.SelectedOrder is null)
            return;

        var order = AppContext.Instance.SelectedOrder;
        order.Items.Remove(orderItem);
        order.NotifyItemsChanged();
        AppContext.Instance.RefreshBadges();
        RefreshItemCollections();
    }

    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not OrderItem orderItem) return;

        orderItem.Quantity++;
        AppContext.Instance.SelectedOrder?.NotifyItemsChanged();
        AppContext.Instance.RefreshBadges();
        RefreshItemCollections();
    }

    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not OrderItem orderItem) return;

        if (orderItem.Quantity > 1)
        {
            orderItem.Quantity--;
        }
        else
        {
            var order = AppContext.Instance.SelectedOrder;
            order?.Items.Remove(orderItem);
        }

        AppContext.Instance.SelectedOrder?.NotifyItemsChanged();
        AppContext.Instance.RefreshBadges();
        RefreshItemCollections();
    }

    private async void OnNoteItemClicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button || button.CommandParameter is not OrderItem orderItem) return;

        var currentNote = orderItem.Notes ?? "";
        var newNote = await DisplayPromptAsync(
            "Ghi chú cho " + orderItem.Name,
            "Nhập yêu cầu đặc biệt (vd: không cay, ít đường):",
            "Lưu",
            "Hủy",
            placeholder: currentNote,
            maxLength: 200);

        if (newNote != null)
        {
            orderItem.Notes = newNote.Trim() == "" ? null : newNote.Trim();
            RefreshItemCollections();
        }
    }

    private void OnNoteEntryCompleted(object sender, EventArgs e)
    {
        AppContext.Instance.RefreshBadges();
    }

    private void AddItemToOrder(Models.FoodItem menuItem)
    {
        var order = AppContext.Instance.SelectedOrder;
        if (order == null) return;

        // Tìm item chưa submit (đang trong phần "Gọi thêm") để cộng dồn
        var unsubmittedItem = order.Items.FirstOrDefault(item =>
            item.MenuItemId == menuItem.Id && !_itemsSubmittedPreviously.Contains(item.Id));

        if (unsubmittedItem != null)
        {
            // Món này đang nằm trong "Gọi thêm" → cộng dồn số lượng
            unsubmittedItem.Quantity++;
        }
        else
        {
            // Món mới hoặc món đã submit trước đó → tạo entry riêng
            order.Items.Add(new OrderItem
            {
                Id = order.Items.Any() ? order.Items.Max(i => i.Id) + 1 : 1,
                MenuItemId = menuItem.Id,
                Name = menuItem.Name,
                Price = menuItem.Price,
                Quantity = 1,
                Image = menuItem.Image,
                Status = DishStatus.Pending
            });
        }

        order.NotifyItemsChanged();
        AppContext.Instance.RefreshBadges();
        RefreshItemCollections();
    }

    private void MergeOrderItems(Order order)
    {
        var grouped = order.Items
            .GroupBy(i => i.MenuItemId)
            .ToList();

        // Nếu không có item trùng nào thì không cần merge
        if (grouped.All(g => g.Count() == 1)) return;

        var mergedItems = new List<OrderItem>();

        foreach (var group in grouped)
        {
            var first = group.First();
            var totalQty = group.Sum(i => i.Quantity);

            // Gộp notes từ các entry khác nhau
            var allNotes = group
                .Where(i => !string.IsNullOrWhiteSpace(i.Notes))
                .Select(i => i.Notes!)
                .Distinct()
                .ToList();

            first.Quantity = totalQty;
            first.Notes = allNotes.Count > 0 ? string.Join("; ", allNotes) : null;
            mergedItems.Add(first);
        }

        order.Items.Clear();
        foreach (var item in mergedItems)
        {
            order.Items.Add(item);
        }

        order.NotifyItemsChanged();
    }
    private bool _isSubmitting = false;

    private async void OnSubmitOrderClicked(object sender, EventArgs e)
    {
        //Kiem tra bam gui don lien tuc
        if (_isSubmitting) return;
        _isSubmitting = true;

        if (AppContext.Instance.SelectedOrder?.Items.Count == 0)
        {
            await DisplayAlert("Thông báo", "Vui lòng thêm ít nhất một món trước khi gửi", "OK");
            _isSubmitting = false;
            return;
        }

        var order = AppContext.Instance.SelectedOrder;
        if (order == null)
        {
            _isSubmitting = false;
            return;
        }

        var confirm = await DisplayAlert(
            "Xác nhận đơn hàng",
            $"Bàn {order.TableNumber}\nTổng: {order.TotalDisplay}\n\nGửi đơn lên bếp?",
            "Có",
            "Không");

        if (!confirm)
        {
            _isSubmitting = false;
            return;
        }

        // Gộp các món trùng MenuItemId lại thành 1 entry duy nhất
        MergeOrderItems(order);

        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
        if (table != null)
        {
            table.Status = TableStatus.Occupied;
            table.CurrentOrderId = order.Id;
            table.HasOrdered = true;
            table.OrderItemCount = order.Items.Count;
            table.OrderTotal = order.TotalDisplay;
            AppContext.Instance.SelectedTable = table;
            _ = _firebase.UpdateTableAsync(table);
        }

        // === CHÍNH THỨC thêm order vào AppContext.Orders nếu là draft ===
        if (_draftOrder != null && _draftOrder.Id == order.Id)
        {
            AppContext.Instance.Orders.Add(order);
            _draftOrder = null; // Không còn là draft nữa
        }

        // Sync order + items to Firebase
        _itemsSubmittedPreviously.Clear();
        _ = _firebase.CreateOrderAsync(order);
        foreach (var item in order.Items)
        {
            item.Status = DishStatus.Pending;
            _itemsSubmittedPreviously.Add(item.Id);
            _ = _firebase.SaveOrderItemAsync(order, item);
        }

        AppContext.Instance.RefreshBadges();
        ActivityLogService.Instance.LogOrderCreation(order.TableNumber.ToString());

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Thành công", "Đơn hàng đã được gửi lên bếp", "OK");
            await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.TableMap));
        });
        _isSubmitting = false;
    }
}