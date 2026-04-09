using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class OrderCreationPage : ContentPage
{
    private string _selectedFilter = "All";
    private string _searchText = "";
    private bool _isOrderReadOnly = false;
    private ObservableCollection<OrderItem> _existingItems = new();
    private ObservableCollection<OrderItem> _newItems = new();
    private HashSet<int> _itemsSubmittedPreviously = new(); // Track items that were already submitted

    public OrderCreationPage()
    {
        InitializeComponent();
        BindingContext = this;
        InitializeFilteredMenuItems();
        UpdatePageState();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // If a table has been selected, create a new order for it if needed
        if (AppContext.Instance.SelectedTable != null)
        {
            var table = AppContext.Instance.SelectedTable;
            var existingOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.TableId == table.Id && o.Status == OrderStatus.Active);

            if (existingOrder != null)
            {
                AppContext.Instance.SelectedOrder = existingOrder;
            }
            else
            {
                // Create a new order for this table
                var allOrders = AppContext.Instance.Orders.Concat(AppContext.Instance.OrderHistory).ToList();
                var newOrder = new Order
                {
                    Id = allOrders.Any() ? allOrders.Max(o => o.Id) + 1 : 1,
                    TableId = table.Id,
                    TableNumber = table.Number,
                    Status = OrderStatus.Active,
                    CreatedAt = DateTime.Now,
                    ServerName = AppContext.Instance.CurrentUser?.Name ?? "Staff",
                    ServerId = AppContext.Instance.CurrentUser?.Id ?? 0,
                    Items = new()
                };

                AppContext.Instance.Orders.Add(newOrder);
                AppContext.Instance.SelectedOrder = newOrder;
            }

            // Notify UI that SelectedOrder has changed
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(SelectedOrder));
                OnPropertyChanged(nameof(ExistingOrderInfo));
            });
        }
        else if (AppContext.Instance.SelectedOrder == null && AppContext.Instance.Orders.Any())
        {
            // If no table selected but there are orders, use the first active order
            var activeOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Status == OrderStatus.Active);
            if (activeOrder != null)
            {
                AppContext.Instance.SelectedOrder = activeOrder;
                // Set the table as well for consistency
                var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Id == activeOrder.TableId);
                if (table != null)
                {
                    AppContext.Instance.SelectedTable = table;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(SelectedOrder));
                    OnPropertyChanged(nameof(ExistingOrderInfo));
                });
            }
        }

        UpdatePageState();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        // Keep the state to allow transfer back to this page
    }

    private void UpdatePageState()
    {
        if (AppContext.Instance.SelectedOrder == null)
        {
            _isOrderReadOnly = false;
            return;
        }

        // Find the table for this order
        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == AppContext.Instance.SelectedOrder.TableNumber);

        if (table == null)
        {
            _isOrderReadOnly = false;
            return;
        }

        // If table is available, this is a new order (not read-only)
        // If table is occupied or reserved, this is an existing order (read-only for existing items)
        _isOrderReadOnly = table.Status == TableStatus.Occupied || table.Status == TableStatus.Reserved;

        RefreshItemCollections();
    }

    // Properties for binding to XAML
    public Order SelectedOrder => AppContext.Instance.SelectedOrder;

    public IEnumerable<Models.MenuItem> FilteredMenuItems => AppContext.Instance.FilteredMenuItems;

    public ObservableCollection<OrderItem> ExistingItems => _existingItems;

    public ObservableCollection<OrderItem> NewItems => _newItems;

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

    /// <summary>
    /// Refresh the ExistingItems and NewItems collections based on SelectedOrder
    /// </summary>
    private void RefreshItemCollections()
    {
        _existingItems.Clear();
        _newItems.Clear();

        var order = AppContext.Instance.SelectedOrder;
        if (order == null)
            return;

        // Separate items into existing (submitted) and new (not yet submitted)
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
        if (sender is not Button button)
            return;

        var category = button.CommandParameter as string;
        if (string.IsNullOrEmpty(category))
            return;

        _selectedFilter = category;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        AppContext.Instance.FilteredMenuItems.Clear();

        var items = AppContext.Instance.MenuItems
            .Where(item =>
            {
                // Lọc theo danh mục
                var categoryMatch = _selectedFilter == "All" || item.Category == _selectedFilter;

                // Tìm kiếm theo tên (case-insensitive)
                var searchMatch = string.IsNullOrEmpty(_searchText) || 
                                 item.Name.ToLower().Contains(_searchText);

                return categoryMatch && searchMatch;
            });

        foreach (var item in items)
        {
            AppContext.Instance.FilteredMenuItems.Add(item);
        }
    }

    private void OnAddMenuItemClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not Models.MenuItem menuItem || AppContext.Instance.SelectedOrder is null)
            return;

        // Kiểm tra xem món có bị hết hàng không
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
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not OrderItem orderItem || AppContext.Instance.SelectedOrder is null)
            return;

        var order = AppContext.Instance.SelectedOrder;
        order.Items.Remove(orderItem);
        order.NotifyItemsChanged();
        AppContext.Instance.RefreshBadges();
        RefreshItemCollections();
    }

    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not OrderItem orderItem)
            return;

        orderItem.Quantity++;
        AppContext.Instance.SelectedOrder?.NotifyItemsChanged();
        AppContext.Instance.RefreshBadges();
        RefreshItemCollections();
    }

    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter is not OrderItem orderItem)
            return;

        if (orderItem.Quantity > 1)
        {
            orderItem.Quantity--;
            AppContext.Instance.SelectedOrder?.NotifyItemsChanged();
            AppContext.Instance.RefreshBadges();
            RefreshItemCollections();
        }
        else
        {
            // Nếu số lượng = 1, xóa hoàn toàn
            var order = AppContext.Instance.SelectedOrder;
            order?.Items.Remove(orderItem);
            order?.NotifyItemsChanged();
            AppContext.Instance.RefreshBadges();
            RefreshItemCollections();
        }
    }

    private async void OnNoteItemClicked(object sender, EventArgs e)
    {
        if (sender is not ImageButton button)
            return;

        if (button.CommandParameter is not OrderItem orderItem)
            return;

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
        // This event is called when user finishes editing notes in the Entry field
        // The binding automatically updates the Notes property, so no additional action needed
        // But we can refresh UI if needed
        AppContext.Instance.RefreshBadges();
    }

    private void AddItemToOrder(Models.MenuItem menuItem)
    {
        var order = AppContext.Instance.SelectedOrder;
        if (order == null)
            return;

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
        if (order == null)
            return;

        // Hiển thị xác nhận
        var confirm = await DisplayAlert(
            "Xác nhận đơn hàng",
            $"Bàn {order.TableNumber}\nTổng: {order.TotalDisplay}\n\nGửi đơn lên bếp?",
            "Có",
            "Không");

        if (!confirm)
            return;

        // Cập nhật trạng thái bàn
        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
        if (table != null)
        {
            table.Status = TableStatus.Occupied;
            table.CurrentOrderId = order.Id;
            AppContext.Instance.SelectedTable = table;
        }

        // Only set NEW items to Pending status - don't re-submit old items
        foreach (var item in order.Items)
        {
            if (!_itemsSubmittedPreviously.Contains(item.Id))
            {
                item.Status = DishStatus.Pending;
                _itemsSubmittedPreviously.Add(item.Id); // Mark as submitted
            }
        }

        AppContext.Instance.RefreshBadges();

        // Log order creation activity
        ActivityLogService.Instance.LogOrderCreation(order.TableNumber.ToString());

        // Auto-navigate to DishStatusPage after successful submission
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Thành công", "Đơn hàng đã được gửi lên bếp", "OK");
            await Shell.Current.GoToAsync("staff/tables");
        });
    }

    private async void OnTransferClicked(object sender, EventArgs e)
    {
        if (AppContext.Instance.SelectedOrder == null)
            return;

        var availableTables = AppContext.Instance.Tables
            .Where(t => t.Status == TableStatus.Available)
            .ToList();

        if (availableTables.Count == 0)
        {
            await DisplayAlert("Chuyển bàn", "Không có bàn trống để chuyển", "OK");
            return;
        }

        var tableNames = availableTables.Select(t => $"Bàn {t.Number}").ToArray();
        var selectedTable = await DisplayActionSheet("Chọn bàn đích", "Hủy", null, tableNames);

        if (selectedTable == null || selectedTable == "Hủy")
            return;

        var selectedTableNum = int.Parse(selectedTable.Replace("Bàn ", ""));
        var targetTable = availableTables.FirstOrDefault(t => t.Number == selectedTableNum);

        if (targetTable != null)
        {
            AppContext.Instance.TransferOrder(targetTable.Id);
            await DisplayAlert("Thành công", $"Đơn hàng đã chuyển sang Bàn {targetTable.Number}", "OK");
        }
    }

    private async void OnMergeClicked(object sender, EventArgs e)
    {
        if (AppContext.Instance.SelectedOrder == null)
            return;

        var otherOrders = AppContext.Instance.Orders
            .Where(o => o.Id != AppContext.Instance.SelectedOrder.Id)
            .ToList();

        if (otherOrders.Count == 0)
        {
            await DisplayAlert("Ghép bàn", "Không có đơn hàng khác để ghép", "OK");
            return;
        }

        var orderNames = otherOrders.Select(o => $"Bàn {o.TableNumber}").ToArray();
        var selectedOrder = await DisplayActionSheet("Chọn bàn để ghép", "Hủy", null, orderNames);

        if (selectedOrder == null || selectedOrder == "Hủy")
            return;

        var selectedTableNum = int.Parse(selectedOrder.Replace("Bàn ", ""));
        var targetOrder = otherOrders.FirstOrDefault(o => o.TableNumber == selectedTableNum);

        if (targetOrder != null)
        {
            AppContext.Instance.MergeOrder(targetOrder.Id);
            await DisplayAlert("Thành công", $"Đã ghép bàn {targetOrder.TableNumber}\n\nTổng mới: {AppContext.Instance.SelectedOrder?.TotalDisplay}", "OK");
        }
    }
}
