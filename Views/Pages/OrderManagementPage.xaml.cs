using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Views.Pages;

/// <summary>
/// Snapshot of an OrderItem before editing, used for rollback on cancel.
/// </summary>
internal class OrderItemSnapshot
{
    public string FirebaseKey { get; set; } = "";
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
    private string _orderSearchText = string.Empty;
    private string _areaFilter = string.Empty;
    private string _dishStatusFilter = string.Empty;
    private string _staffFilter = string.Empty;
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
    private readonly List<(OrderItem Item, int Index)> _removedItems = new();
    private bool _isSavingEdits;
    private bool _isObservingContext;

    public ObservableCollection<FoodItem> ModalFilteredMenuItems { get; set; } = new();

    public OrderManagementPage()
    {
        InitializeComponent();
        BindingContext = this;
        AreaFilter.ItemsSource = new[] { "Tầng trệt", "Tầng 2", "Sân vườn" };
        DishStatusFilter.ItemsSource = new[] { "Chờ xử lý", "Đang làm", "Sẵn sàng", "Đã phục vụ" };
    }

    private void OnAppContextPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppContext.OrderItemsVersion) || _isSavingEdits)
            return;

        RefreshPage();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObservingContext)
        {
            AppContext.Instance.PropertyChanged += OnAppContextPropertyChanged;
            _isObservingContext = true;
        }
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

    private void RefreshPage()
    {
        StaffFilter.ItemsSource = AppContext.Instance.Orders
            .Where(order => order.Status == OrderStatus.Active && order.Items.Count > 0)
            .Select(order => order.ServerName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToArray();

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
            var activeOrders = FilteredOrders.ToList();
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

            if (!string.IsNullOrWhiteSpace(_orderSearchText))
            {
                var search = _orderSearchText.Trim();
                orders = orders.Where(order =>
                    order.TableNumber.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    order.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    order.Items.Any(item => item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(_areaFilter))
            {
                var floor = _areaFilter switch
                {
                    var value when value.Contains("Tầng trệt", StringComparison.OrdinalIgnoreCase) => "Ground Floor",
                    var value when value.Contains("Tầng 2", StringComparison.OrdinalIgnoreCase) => "Second Floor",
                    var value when value.Contains("Sân vườn", StringComparison.OrdinalIgnoreCase) => "Garden",
                    _ => string.Empty
                };

                if (!string.IsNullOrWhiteSpace(floor))
                {
                    orders = orders.Where(order =>
                    {
                        var table = AppContext.Instance.Tables.FirstOrDefault(item => item.Id == order.TableId)
                                    ?? AppContext.Instance.Tables.FirstOrDefault(item => item.Number == order.TableNumber);
                        return string.Equals(table?.Floor, floor, StringComparison.OrdinalIgnoreCase);
                    });
                }
            }

            if (TryGetDishStatus(_dishStatusFilter, out var dishStatus))
                orders = orders.Where(order => order.Items.Any(item => item.Status == dishStatus));

            if (!string.IsNullOrWhiteSpace(_staffFilter))
            {
                var staff = _staffFilter.Trim();
                orders = orders.Where(order => order.ServerName.Contains(staff, StringComparison.OrdinalIgnoreCase));
            }

            return orders.OrderBy(o => o.CreatedAt);
        }
    }

    private static bool TryGetDishStatus(string text, out DishStatus status)
    {
        if (text.Contains("Chờ", StringComparison.OrdinalIgnoreCase))
        {
            status = DishStatus.Pending;
            return true;
        }

        if (text.Contains("Đang", StringComparison.OrdinalIgnoreCase))
        {
            status = DishStatus.Preparing;
            return true;
        }

        if (text.Contains("Sẵn", StringComparison.OrdinalIgnoreCase))
        {
            status = DishStatus.Ready;
            return true;
        }

        if (text.Contains("phục vụ", StringComparison.OrdinalIgnoreCase))
        {
            status = DishStatus.Served;
            return true;
        }

        status = default;
        return false;
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

        if (_addedItems.Contains(item))
            return;

        // Snapshot original state (only once per item)
        if (!_originalSnapshots.ContainsKey(item.Id))
        {
            _originalSnapshots[item.Id] = new OrderItemSnapshot
            {
                FirebaseKey = item.FirebaseKey,
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
        _removedItems.Clear();
    }

    // ── Cancel button ──
    private void OnCancelEditsClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Order order }) return;

        foreach (var removed in _removedItems.OrderBy(entry => entry.Index))
        {
            if (!order.Items.Contains(removed.Item))
                order.Items.Insert(Math.Min(removed.Index, order.Items.Count), removed.Item);
        }

        // Rollback all snapshots
        foreach (var snap in _originalSnapshots.Values)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == snap.Id);
            if (item != null)
            {
                item.MenuItemId = snap.MenuItemId;
                item.FirebaseKey = snap.FirebaseKey;
                item.Name = snap.Name;
                item.Price = snap.Price;
                item.Quantity = snap.Quantity;
                item.Notes = snap.Notes;
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
        if (_isSavingEdits) return;
        _isSavingEdits = true;

        try
        {
            var operations = new List<Task>();
            operations.AddRange(_removedItems.Select(removed =>
                _firebase.DeleteOrderItemAsync(order, removed.Item)));
            operations.AddRange(_originalSnapshots.Values
                .Select(snapshot => order.Items.FirstOrDefault(item => item.Id == snapshot.Id))
                .Where(item => item != null)
                .Select(item => _firebase.SaveOrderItemAsync(order, item!)));
            operations.AddRange(_addedItems.Select(item => _firebase.SaveOrderItemAsync(order, item)));

            await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(15));

            var table = AppContext.Instance.Tables.FirstOrDefault(candidate => candidate.Id == order.TableId)
                     ?? AppContext.Instance.Tables.FirstOrDefault(candidate => candidate.Number == order.TableNumber);
            if (table != null)
            {
                table.OrderItemCount = order.Items.Sum(item => item.Quantity);
                table.OrderTotal = order.TotalDisplay;
                table.HasOrdered = order.Items.Count > 0;
                await _firebase.UpdateTableAsync(table);
            }

            order.NotifyItemsChanged();
            ClearPendingState();
            OnPropertyChanged(nameof(FilteredOrders));
            OnPropertyChanged(nameof(OrderSummaryText));
            AppContext.Instance.NotifyOrderItemsChanged();
            await DisplayAlert("Thành công", "Đã lưu toàn bộ thay đổi của đơn hàng.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể lưu thay đổi: {ex.Message}", "OK");
        }
        finally
        {
            _isSavingEdits = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FILTER & REFRESH
    // ═══════════════════════════════════════════════════════════════

    private void OnOrderSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _orderSearchText = e.NewTextValue ?? string.Empty;
        RefreshPage();
    }

    private void OnOrderFilterChanged(object? sender, EventArgs e)
    {
        _areaFilter = AreaFilter.Text.Trim();
        _dishStatusFilter = DishStatusFilter.Text.Trim();
        _staffFilter = StaffFilter.Text.Trim();
        RefreshPage();
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
            $"Trạng thái: {item.Name} (x{item.Quantity})", "Hủy", null,
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

        if (item.Quantity > 1)
        {
            // Hỏi admin muốn đổi bao nhiêu cái
            var qtyStr = await DisplayPromptAsync(
                "Số lượng thay đổi",
                $"Đổi bao nhiêu \"{item.Name}\" sang \"{result}\"?\n(Tổng: {item.Quantity})",
                "Xác nhận", "Hủy",
                initialValue: item.Quantity.ToString(),
                maxLength: 3,
                keyboard: Keyboard.Numeric);

            if (qtyStr == null) return;
            if (!int.TryParse(qtyStr, out var changeQty) || changeQty <= 0 || changeQty > item.Quantity)
            {
                await DisplayAlert("Lỗi", $"Vui lòng nhập số từ 1 đến {item.Quantity}.", "OK");
                return;
            }

            if (changeQty == item.Quantity)
            {
                // Đổi toàn bộ
                MarkOrderDirty(parentOrder, item);
                item.Status = newStatus;
            }
            else
            {
                // Tách: giảm qty item gốc, tạo item mới với status mới
                MarkOrderDirty(parentOrder, item);
                item.Quantity -= changeQty;

                var maxId = parentOrder.Items.Max(i => i.Id);
                var splitItem = new OrderItem
                {
                    Id = maxId + 1,
                    MenuItemId = item.MenuItemId,
                    FirebaseKey = "",
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = changeQty,
                    Notes = item.Notes,
                    Status = newStatus,
                    Image = item.Image
                };

                // Chèn ngay sau item gốc
                var index = parentOrder.Items.IndexOf(item);
                parentOrder.Items.Insert(index + 1, splitItem);
                _addedItems.Add(splitItem);

                _orderBeingEdited = parentOrder;
                parentOrder.HasPendingEdits = true;
            }
        }
        else
        {
            // Quantity = 1 → đổi trực tiếp
            MarkOrderDirty(parentOrder, item);
            item.Status = newStatus;
        }

        parentOrder.NotifyItemsChanged();
        OnPropertyChanged(nameof(FilteredOrders));
    }

    // ── Quick status for entire order ──
    private async void OnQuickStatusTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string statusKey) return;

        // Walk up to find the Order from the card's BindingContext
        var element = sender as Element;
        while (element != null && element.BindingContext is not Order)
            element = element.Parent;
        if (element?.BindingContext is not Order order) return;

        if (await GuardPendingEditsAsync(order)) return;

        var newStatus = statusKey switch
        {
            "Pending" => DishStatus.Pending,
            "Preparing" => DishStatus.Preparing,
            "Ready" => DishStatus.Ready,
            "Served" => DishStatus.Served,
            _ => (DishStatus?)null
        };
        if (newStatus == null) return;

        // Skip if all items already have the target status
        if (order.Items.All(i => i.Status == newStatus.Value)) return;

        foreach (var item in order.Items)
        {
            if (item.Status != newStatus.Value)
            {
                MarkOrderDirty(order, item);
                item.Status = newStatus.Value;
            }
        }

        order.NotifyItemsChanged();
        OnPropertyChanged(nameof(FilteredOrders));
    }

    // ═══════════════════════════════════════════════════════════════
    //  QUANTITY CONTROLS
    // ═══════════════════════════════════════════════════════════════

    private void RemovePendingItem(Order order, OrderItem item)
    {
        var index = order.Items.IndexOf(item);
        if (_addedItems.Remove(item))
        {
            _originalSnapshots.Remove(item.Id);
        }
        else
        {
            MarkOrderDirty(order, item);
            if (_removedItems.All(entry => entry.Item != item))
                _removedItems.Add((item, index));
        }

        order.Items.Remove(item);
        if (_originalSnapshots.Count == 0 && _addedItems.Count == 0 && _removedItems.Count == 0)
            ClearPendingState();
    }

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

            RemovePendingItem(parentOrder, item);
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
            RemovePendingItem(parentOrder, item);
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
            items = items.Where(i => MenuCategoryHelper.Matches(i.Category, _modalCategoryFilter));

        if (!string.IsNullOrWhiteSpace(_modalSearchText))
            items = items.Where(i => i.Name.Contains(_modalSearchText, StringComparison.OrdinalIgnoreCase));

        ModalFilteredMenuItems = new ObservableCollection<FoodItem>(items);
        OnPropertyChanged(nameof(ModalFilteredMenuItems));

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
