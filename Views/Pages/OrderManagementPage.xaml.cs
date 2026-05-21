using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

/// <summary>
/// Snapshot of an OrderItem before editing, used for rollback on cancel.
/// </summary>
internal class OrderItemSnapshot
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DishStatus Status { get; set; }
    public string? Image { get; set; }
}

public partial class OrderManagementPage : ContentPage
{
    private readonly FirebaseService _firebase = new();
    private string _currentStatusFilter = "All";
    private string _modalCategoryFilter = "All";
    private string _modalSearchText = string.Empty;

    // ── Modal context ──
    private OrderItem? _editingOrderItem;   // item being replaced (null = add mode)
    private Order? _modalTargetOrder;       // order targeted by modal
    private bool _isAddMode;                // true = adding, false = replacing

    // ── Pending edits tracking ──
    private Order? _orderBeingEdited;
    private readonly Dictionary<int, OrderItemSnapshot> _originalSnapshots = new();
    private readonly List<OrderItem> _addedItems = new();

    public ObservableCollection<FoodItem> ModalFilteredMenuItems { get; set; } = new();

    public OrderManagementPage()
    {
        InitializeComponent();
        BindingContext = this;

        AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
    }

    private void OnAppContextPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(FilteredOrders));
            OnPropertyChanged(nameof(OrderSummaryText));
        });
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        MainThread.BeginInvokeOnMainThread(RefreshPage);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        // Persist pending edits when leaving page — they remain in memory
        // so returning to this page keeps the edits alive
        base.OnNavigatingFrom(args);
    }

    private void RefreshPage()
    {
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(OrderSummaryText));
    }

    // ═══════════════════════════════════════════════════════════════
    //  PROPERTIES
    // ═══════════════════════════════════════════════════════════════

    public string OrderSummaryText
    {
        get
        {
            var activeOrders = AppContext.Instance.Orders.Where(o => o.Items.Count > 0).ToList();
            var totalItems = activeOrders.Sum(o => o.Items.Sum(i => i.Quantity));
            return $"{activeOrders.Count} đơn đang hoạt động · {totalItems} món";
        }
    }

    public IEnumerable<Order> FilteredOrders
    {
        get
        {
            var orders = AppContext.Instance.Orders
                .Where(o => o.Items.Count > 0 && o.Status == OrderStatus.Active)
                .AsEnumerable();

            if (_currentStatusFilter != "All")
            {
                var statusEnum = _currentStatusFilter switch
                {
                    "Pending" => DishStatus.Pending,
                    "Preparing" => DishStatus.Preparing,
                    "Ready" => DishStatus.Ready,
                    "Served" => DishStatus.Served,
                    _ => DishStatus.Pending
                };
                orders = orders.Where(o => o.Items.Any(item => item.Status == statusEnum));
            }

            return orders.OrderByDescending(o => o.CreatedAt);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  PENDING EDITS — guards when switching to another order
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if user is already editing a different order. Shows warning if so.
    /// Returns true if this edit should be blocked.
    /// </summary>
    private async Task<bool> GuardPendingEditsAsync(Order targetOrder)
    {
        if (_orderBeingEdited != null && _orderBeingEdited != targetOrder && _orderBeingEdited.HasPendingEdits)
        {
            await DisplayAlert(
                "⚠️ Cảnh báo",
                $"Bạn đang có đơn hàng Bàn {_orderBeingEdited.TableNumber} (Đơn #{_orderBeingEdited.Id}) được chỉnh sửa.\nVui lòng xác nhận hoặc hủy trước khi chỉnh sửa đơn khác.",
                "Đã hiểu");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks the order as having pending edits and snapshots the item if not already captured.
    /// </summary>
    private void MarkOrderDirty(Order order, OrderItem item)
    {
        _orderBeingEdited = order;
        order.HasPendingEdits = true;

        // Snapshot original state (only once per item)
        if (!_originalSnapshots.ContainsKey(item.Id))
        {
            _originalSnapshots[item.Id] = new OrderItemSnapshot
            {
                Id = item.Id,
                MenuItemId = item.MenuItemId,
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                Notes = item.Notes,
                Status = item.Status,
                Image = item.Image
            };
        }
    }

    private void ClearPendingState()
    {
        if (_orderBeingEdited != null)
            _orderBeingEdited.HasPendingEdits = false;
        _orderBeingEdited = null;
        _originalSnapshots.Clear();
        _addedItems.Clear();
    }

    // ── Cancel button ──
    private void OnCancelEditsClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Order order }) return;

        // Rollback all snapshots
        foreach (var snap in _originalSnapshots.Values)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == snap.Id);
            if (item != null)
            {
                item.MenuItemId = snap.MenuItemId;
                item.Name = snap.Name;
                item.Price = snap.Price;
                item.Quantity = snap.Quantity;
                item.Notes = snap.Notes;
                item.Status = DishStatus.Pending; // force re-trigger
                item.Status = snap.Status;
            }
        }

        // Remove added items
        foreach (var added in _addedItems)
        {
            order.Items.Remove(added);
        }

        order.NotifyItemsChanged();
        ClearPendingState();
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(OrderSummaryText));
    }

    // ── Confirm button ──
    private async void OnConfirmEditsClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Order order }) return;

        try
        {
            // Save all modified items to Firebase
            foreach (var snap in _originalSnapshots.Values)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == snap.Id);
                if (item != null)
                {
                    await _firebase.UpdateOrderItemStatusAsync(item.Id, item.Status);
                    await _firebase.UpdateOrderItemFieldsAsync(item.Id, item.Quantity, item.Notes);
                }
            }

            // Save newly added items
            foreach (var added in _addedItems)
            {
                await _firebase.SaveOrderItemAsync(order, added);
            }

            order.NotifyItemsChanged();
            ClearPendingState();
            OnPropertyChanged(nameof(FilteredOrders));
            OnPropertyChanged(nameof(OrderSummaryText));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể lưu thay đổi: {ex.Message}", "OK");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FILTER & REFRESH
    // ═══════════════════════════════════════════════════════════════

    private void OnOrderStatusFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string filter) return;
        _currentStatusFilter = filter;
        OnPropertyChanged(nameof(FilteredOrders));
    }

    private void OnRefreshClicked(object? sender, EventArgs e) => RefreshPage();

    // ═══════════════════════════════════════════════════════════════
    //  STATUS DROPDOWN
    // ═══════════════════════════════════════════════════════════════

    private async void OnStatusButtonTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item) return;
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null) return;

        if (await GuardPendingEditsAsync(parentOrder)) return;

        var result = await DisplayActionSheet(
            $"Trạng thái: {item.Name}", "Hủy", null,
            "Chờ xử lý", "Đang làm", "Sẵn sàng", "Đã phục vụ");

        if (string.IsNullOrEmpty(result) || result == "Hủy") return;

        var newStatus = result switch
        {
            "Chờ xử lý" => DishStatus.Pending,
            "Đang làm" => DishStatus.Preparing,
            "Sẵn sàng" => DishStatus.Ready,
            "Đã phục vụ" => DishStatus.Served,
            _ => item.Status
        };

        if (newStatus == item.Status) return;

        MarkOrderDirty(parentOrder, item);
        item.Status = DishStatus.Pending; // force different first
        item.Status = newStatus;
        parentOrder.NotifyItemsChanged();
        OnPropertyChanged(nameof(FilteredOrders));
    }

    // ═══════════════════════════════════════════════════════════════
    //  QUANTITY CONTROLS
    // ═══════════════════════════════════════════════════════════════

    private async void OnDecreaseQtyTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item) return;
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null) return;

        if (await GuardPendingEditsAsync(parentOrder)) return;

        if (item.Quantity <= 1)
        {
            var confirmed = await DisplayAlert(
                "Xóa món", $"Bạn muốn xóa \"{item.Name}\" khỏi đơn hàng?", "Xóa", "Hủy");
            if (!confirmed) return;

            MarkOrderDirty(parentOrder, item);
            parentOrder.Items.Remove(item);
            parentOrder.NotifyItemsChanged();
            OnPropertyChanged(nameof(FilteredOrders));
            OnPropertyChanged(nameof(OrderSummaryText));
        }
        else
        {
            MarkOrderDirty(parentOrder, item);
            item.Quantity--;
            parentOrder.NotifyItemsChanged();
        }
    }

    private async void OnIncreaseQtyTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item) return;
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null) return;

        if (await GuardPendingEditsAsync(parentOrder)) return;

        MarkOrderDirty(parentOrder, item);
        item.Quantity++;
        parentOrder.NotifyItemsChanged();
    }

    // Direct quantity entry
    private async void OnQtyEntryCompleted(object? sender, EventArgs e)
    {
        if (sender is not Entry entry) return;
        if (!int.TryParse(entry.Text, out var newQty) || newQty < 0)
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập số hợp lệ (>= 0).", "OK");
            return;
        }

        // Find the OrderItem from BindingContext
        if (entry.BindingContext is not OrderItem item) return;
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null) return;

        if (await GuardPendingEditsAsync(parentOrder)) return;

        if (newQty == 0)
        {
            var confirmed = await DisplayAlert(
                "Xóa món", $"Số lượng = 0, xóa \"{item.Name}\" khỏi đơn?", "Xóa", "Hủy");
            if (!confirmed)
            {
                entry.Text = item.Quantity.ToString();
                return;
            }
            MarkOrderDirty(parentOrder, item);
            parentOrder.Items.Remove(item);
        }
        else
        {
            MarkOrderDirty(parentOrder, item);
            item.Quantity = newQty;
        }

        parentOrder.NotifyItemsChanged();
        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(OrderSummaryText));
    }

    // ═══════════════════════════════════════════════════════════════
    //  NOTES EDITING
    // ═══════════════════════════════════════════════════════════════

    private async void OnNoteTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item) return;
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null) return;

        if (await GuardPendingEditsAsync(parentOrder)) return;

        var newNote = await DisplayPromptAsync(
            "Chỉnh sửa ghi chú",
            $"Ghi chú cho \"{item.Name}\":",
            "Lưu", "Hủy",
            initialValue: item.Notes ?? "",
            maxLength: 200,
            keyboard: Keyboard.Text);

        if (newNote == null) return;

        MarkOrderDirty(parentOrder, item);
        item.Notes = newNote;
    }

    // ═══════════════════════════════════════════════════════════════
    //  MENU MODAL — "Thêm món" and "Thay thế món"
    // ═══════════════════════════════════════════════════════════════

    // Tap dish name → replace mode
    private async void OnDishNameTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item) return;
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null) return;

        if (await GuardPendingEditsAsync(parentOrder)) return;

        _editingOrderItem = item;
        _modalTargetOrder = parentOrder;
        _isAddMode = false;

        ModalTitle.Text = "🍽️ Chọn món thay thế";
        ModalSubtitle.Text = $"Thay thế \"{item.Name}\" bằng món khác";
        ModalFooterText.Text = "💡 Chọn một món từ thực đơn để thay thế món hiện tại";
        ResetAndShowModal();
    }

    // "Thêm món" button → add mode
    private async void OnAddItemToOrderTapped(object? sender, TappedEventArgs e)
    {
        // Find the Order from the card's BindingContext
        var element = sender as Element;
        while (element != null && element.BindingContext is not Order)
        {
            element = element.Parent;
        }
        if (element?.BindingContext is not Order order) return;

        if (await GuardPendingEditsAsync(order)) return;

        _editingOrderItem = null;
        _modalTargetOrder = order;
        _isAddMode = true;

        ModalTitle.Text = "🍽️ Thêm món mới";
        ModalSubtitle.Text = $"Thêm món vào đơn hàng Bàn {order.TableNumber}";
        ModalFooterText.Text = "💡 Chọn một món từ thực đơn để thêm vào đơn hàng";
        ResetAndShowModal();
    }

    private void ResetAndShowModal()
    {
        _modalCategoryFilter = "All";
        _modalSearchText = string.Empty;
        ModalSearchEntry.Text = string.Empty;
        RefreshModalMenuItems();
        MenuModal.IsVisible = true;
    }

    private void OnCloseModalTapped(object? sender, TappedEventArgs e)
    {
        MenuModal.IsVisible = false;
        _editingOrderItem = null;
        _modalTargetOrder = null;
    }

    private void OnModalSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _modalSearchText = e.NewTextValue ?? string.Empty;
        RefreshModalMenuItems();
    }

    private void OnModalFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string filter) return;
        _modalCategoryFilter = filter;
        RefreshModalMenuItems();
    }

    private void RefreshModalMenuItems()
    {
        var items = AppContext.Instance.MenuItems.AsEnumerable();

        if (_modalCategoryFilter != "All")
            items = items.Where(i => i.Category.Equals(_modalCategoryFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(_modalSearchText))
            items = items.Where(i => i.Name.Contains(_modalSearchText, StringComparison.OrdinalIgnoreCase));

        ModalFilteredMenuItems.Clear();
        foreach (var item in items)
            ModalFilteredMenuItems.Add(item);

        ModalMenuItems.ItemsSource = ModalFilteredMenuItems;
    }

    private void OnModalMenuItemSelected(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not FoodItem selectedFood) return;
        if (_modalTargetOrder == null) return;

        if (_isAddMode)
        {
            // ADD MODE — create new OrderItem and add to order
            var maxId = _modalTargetOrder.Items.Count > 0
                ? _modalTargetOrder.Items.Max(i => i.Id)
                : 0;

            var newItem = new OrderItem
            {
                Id = maxId + 1,
                MenuItemId = selectedFood.Id,
                Name = selectedFood.Name,
                Price = selectedFood.Price,
                Quantity = 1,
                Status = DishStatus.Pending,
                Image = selectedFood.Image,
                Notes = ""
            };

            _modalTargetOrder.Items.Add(newItem);
            _addedItems.Add(newItem);

            _orderBeingEdited = _modalTargetOrder;
            _modalTargetOrder.HasPendingEdits = true;
        }
        else if (_editingOrderItem != null)
        {
            // REPLACE MODE — update existing item
            MarkOrderDirty(_modalTargetOrder, _editingOrderItem);

            _editingOrderItem.MenuItemId = selectedFood.Id;
            _editingOrderItem.Name = selectedFood.Name;
            _editingOrderItem.Price = selectedFood.Price;
            _editingOrderItem.Image = selectedFood.Image;
        }

        _modalTargetOrder.NotifyItemsChanged();
        MenuModal.IsVisible = false;
        _editingOrderItem = null;
        _modalTargetOrder = null;

        OnPropertyChanged(nameof(FilteredOrders));
        OnPropertyChanged(nameof(OrderSummaryText));
    }
}
