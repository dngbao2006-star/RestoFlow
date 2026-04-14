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
    private ObservableCollection<OrderItem> _existingItems = new();
    private ObservableCollection<OrderItem> _newItems = new();
    private ObservableCollection<Table> _activeTables = new();
    private Table _selectedActiveTable;
    private HashSet<int> _itemsSubmittedPreviously = new();

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
            targetTable = _activeTables.First(); // Ưu tiên T1, T2...
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
            UpdatePageState();
        }

        _isLoadingData = false;
    }

    private void SwitchTableData(Table table)
    {
        var existingOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.TableId == table.Id && o.Status == OrderStatus.Active);

        if (existingOrder != null)
        {
            AppContext.Instance.SelectedOrder = existingOrder;
        }
        else
        {
            var allOrders = AppContext.Instance.Orders.Concat(AppContext.Instance.OrderHistory).ToList();
            var newOrder = new Order
            {
                Id = allOrders.Any() ? allOrders.Max(o => o.Id) + 1 : 1,
                TableId = table.Id,
                TableNumber = table.Number,
                Status = OrderStatus.Active,
                CreatedAt = DateTime.Now,
                ServerName = AppContext.Instance.CurrentUser?.Name ?? "Nhân viên",
                ServerId = AppContext.Instance.CurrentUser?.Id ?? 0,
                Items = new()
            };

            AppContext.Instance.Orders.Add(newOrder);
            AppContext.Instance.SelectedOrder = newOrder;
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

        var existingItem = order.Items.FirstOrDefault(item => item.MenuItemId == menuItem.Id);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
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

    private async void OnSubmitOrderClicked(object sender, EventArgs e)
    {
        if (AppContext.Instance.SelectedOrder?.Items.Count == 0)
        {
            await DisplayAlert("Thông báo", "Vui lòng thêm ít nhất một món trước khi gửi", "OK");
            return;
        }

        var order = AppContext.Instance.SelectedOrder;
        if (order == null) return;

        var confirm = await DisplayAlert(
            "Xác nhận đơn hàng",
            $"Bàn {order.TableNumber}\nTổng: {order.TotalDisplay}\n\nGửi đơn lên bếp?",
            "Có",
            "Không");

        if (!confirm) return;

        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
        if (table != null)
        {
            table.Status = TableStatus.Occupied;
            table.CurrentOrderId = order.Id;
            AppContext.Instance.SelectedTable = table;
        }

        foreach (var item in order.Items)
        {
            if (!_itemsSubmittedPreviously.Contains(item.Id))
            {
                item.Status = DishStatus.Pending;
                _itemsSubmittedPreviously.Add(item.Id);
            }
        }

        AppContext.Instance.RefreshBadges();
        ActivityLogService.Instance.LogOrderCreation(order.TableNumber.ToString());

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Thành công", "Đơn hàng đã được gửi lên bếp", "OK");
            await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.TableMap));
        });
    }
}