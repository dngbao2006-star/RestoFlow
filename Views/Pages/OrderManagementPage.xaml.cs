using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class OrderManagementPage : ContentPage
{
    private readonly FirebaseService _firebase = new();
    private string _currentStatusFilter = "All";
    private string _modalCategoryFilter = "All";
    private string _modalSearchText = string.Empty;
    private OrderItem? _editingOrderItem;
    private Order? _editingOrder;

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
    //  STATUS FILTER
    // ═══════════════════════════════════════════════════════════════

    private void OnOrderStatusFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string filter)
            return;

        _currentStatusFilter = filter;
        OnPropertyChanged(nameof(FilteredOrders));
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        RefreshPage();
    }

    // ═══════════════════════════════════════════════════════════════
    //  STATUS DROPDOWN (change item status)
    // ═══════════════════════════════════════════════════════════════

    private async void OnStatusButtonTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item)
            return;

        // Find which order this item belongs to
        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));

        var statusOptions = new[]
        {
            "Chờ xử lý",
            "Đang làm",
            "Sẵn sàng",
            "Đã phục vụ"
        };

        var result = await DisplayActionSheet(
            $"Trạng thái: {item.Name}",
            "Hủy",
            null,
            statusOptions);

        if (string.IsNullOrEmpty(result) || result == "Hủy")
            return;

        var newStatus = result switch
        {
            "Chờ xử lý" => DishStatus.Pending,
            "Đang làm" => DishStatus.Preparing,
            "Sẵn sàng" => DishStatus.Ready,
            "Đã phục vụ" => DishStatus.Served,
            _ => item.Status
        };

        if (newStatus == item.Status)
            return;

        try
        {
            // Update Firebase
            await _firebase.UpdateOrderItemStatusAsync(item.Id, newStatus);

            // Update local — force re-trigger setter to notify bindings
            // The Status setter fires OnPropertyChanged("Status"),
            // and computed properties StatusLabel/StatusColor will re-evaluate on binding refresh
            item.Status = DishStatus.Pending; // force different value first
            item.Status = newStatus;           // then set actual value

            if (parentOrder != null)
                parentOrder.NotifyItemsChanged();

            OnPropertyChanged(nameof(FilteredOrders));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể cập nhật trạng thái: {ex.Message}", "OK");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  QUANTITY CONTROLS
    // ═══════════════════════════════════════════════════════════════

    private async void OnDecreaseQtyTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item)
            return;

        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null)
            return;

        if (item.Quantity <= 1)
        {
            // Confirm delete
            var confirmed = await DisplayAlert(
                "Xóa món",
                $"Bạn muốn xóa \"{item.Name}\" khỏi đơn hàng?",
                "Xóa", "Hủy");

            if (!confirmed)
                return;

            try
            {
                await _firebase.DeleteOrderItemAsync(item.Id);
                parentOrder.Items.Remove(item);
                parentOrder.NotifyItemsChanged();
                OnPropertyChanged(nameof(FilteredOrders));
                OnPropertyChanged(nameof(OrderSummaryText));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể xóa món: {ex.Message}", "OK");
            }
        }
        else
        {
            item.Quantity--;
            try
            {
                await _firebase.UpdateOrderItemFieldsAsync(item.Id, item.Quantity, item.Notes);
                parentOrder.NotifyItemsChanged();
            }
            catch (Exception ex)
            {
                item.Quantity++; // rollback
                await DisplayAlert("Lỗi", $"Không thể cập nhật: {ex.Message}", "OK");
            }
        }
    }

    private async void OnIncreaseQtyTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item)
            return;

        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));
        if (parentOrder == null)
            return;

        item.Quantity++;
        try
        {
            await _firebase.UpdateOrderItemFieldsAsync(item.Id, item.Quantity, item.Notes);
            parentOrder.NotifyItemsChanged();
        }
        catch (Exception ex)
        {
            item.Quantity--; // rollback
            await DisplayAlert("Lỗi", $"Không thể cập nhật: {ex.Message}", "OK");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  NOTES EDITING
    // ═══════════════════════════════════════════════════════════════

    private async void OnNoteTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item)
            return;

        var parentOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));

        var newNote = await DisplayPromptAsync(
            "Chỉnh sửa ghi chú",
            $"Ghi chú cho \"{item.Name}\":",
            "Lưu",
            "Hủy",
            initialValue: item.Notes ?? "",
            maxLength: 200,
            keyboard: Keyboard.Text);

        if (newNote == null) // cancelled
            return;

        var oldNote = item.Notes;
        item.Notes = newNote;

        try
        {
            await _firebase.UpdateOrderItemFieldsAsync(item.Id, item.Quantity, item.Notes);
        }
        catch (Exception ex)
        {
            item.Notes = oldNote; // rollback
            await DisplayAlert("Lỗi", $"Không thể cập nhật ghi chú: {ex.Message}", "OK");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  MENU MODAL (dish name tap → replace dish)
    // ═══════════════════════════════════════════════════════════════

    private void OnDishNameTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not OrderItem item)
            return;

        _editingOrderItem = item;
        _editingOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.Items.Contains(item));

        ModalSubtitle.Text = $"Thay thế \"{item.Name}\" bằng món khác";
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
        _editingOrder = null;
    }

    private void OnModalSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _modalSearchText = e.NewTextValue ?? string.Empty;
        RefreshModalMenuItems();
    }

    private void OnModalFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string filter)
            return;

        _modalCategoryFilter = filter;
        RefreshModalMenuItems();
    }

    private void RefreshModalMenuItems()
    {
        var items = AppContext.Instance.MenuItems.AsEnumerable();

        // Apply category filter
        if (_modalCategoryFilter != "All")
        {
            items = items.Where(i =>
                i.Category.Equals(_modalCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_modalSearchText))
        {
            items = items.Where(i =>
                i.Name.Contains(_modalSearchText, StringComparison.OrdinalIgnoreCase));
        }

        ModalFilteredMenuItems.Clear();
        foreach (var item in items)
        {
            ModalFilteredMenuItems.Add(item);
        }

        ModalMenuItems.ItemsSource = ModalFilteredMenuItems;
    }

    private async void OnModalMenuItemSelected(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not FoodItem selectedFood)
            return;

        if (_editingOrderItem == null || _editingOrder == null)
            return;

        // Update the order item with new dish info
        var oldName = _editingOrderItem.Name;
        _editingOrderItem.MenuItemId = selectedFood.Id;
        _editingOrderItem.Name = selectedFood.Name;
        _editingOrderItem.Price = selectedFood.Price;
        _editingOrderItem.Image = selectedFood.Image;

        try
        {
            // Save to Firebase — re-save the entire order item
            await _firebase.SaveOrderItemAsync(_editingOrder, _editingOrderItem);
            _editingOrder.NotifyItemsChanged();

            MenuModal.IsVisible = false;
            _editingOrderItem = null;
            _editingOrder = null;

            OnPropertyChanged(nameof(FilteredOrders));
        }
        catch (Exception ex)
        {
            // Rollback is complex here, just alert the user
            await DisplayAlert("Lỗi", $"Không thể thay đổi món: {ex.Message}", "OK");
        }
    }
}
