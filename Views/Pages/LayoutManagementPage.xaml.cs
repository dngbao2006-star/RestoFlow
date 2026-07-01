using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class LayoutManagementPage : ContentPage
{
    private readonly FirebaseService _firebaseService;
    private bool _isEditMode = false;
    private string _currentFloor = "Ground Floor";
    private Table? _selectedTable;
    private bool _isObservingTables;
    private int _refreshScheduled;
    private bool _isEditingReservation = false;

    // Filtered tables for the current floor tab
    public ObservableCollection<Table> FilteredTables { get; private set; } = new();

    public LayoutManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;

        _firebaseService = new FirebaseService();

        // Default to Ground Floor tab active
        SetActiveTab("Ground Floor");
        RefreshFilteredTables();

    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObservingTables)
        {
            AppContext.Instance.Tables.CollectionChanged += OnTablesCollectionChanged;
            _isObservingTables = true;
        }
        RefreshFilteredTables();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObservingTables)
        {
            AppContext.Instance.Tables.CollectionChanged -= OnTablesCollectionChanged;
            _isObservingTables = false;
        }
        base.OnNavigatingFrom(args);
    }

    private void OnTablesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => RefreshFilteredTables();

    // ═══════════════════════════════════════════════════════════════════
    // FLOOR TAB SWITCHING
    // ═══════════════════════════════════════════════════════════════════

    private void OnFloorTabClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string floor)
        {
            _currentFloor = floor;
            SetActiveTab(floor);
            RefreshFilteredTables();
        }
    }

    private void SetActiveTab(string floor)
    {
        // Reset all tabs
        TabGround.BackgroundColor = Color.FromArgb("#EDE8DC");
        TabGround.TextColor = Color.FromArgb("#7B6A57");
        TabSecond.BackgroundColor = Color.FromArgb("#EDE8DC");
        TabSecond.TextColor = Color.FromArgb("#7B6A57");
        TabGarden.BackgroundColor = Color.FromArgb("#EDE8DC");
        TabGarden.TextColor = Color.FromArgb("#7B6A57");

        // Highlight active tab
        var activeBtn = floor switch
        {
            "Ground Floor" => TabGround,
            "Second Floor" => TabSecond,
            "Garden" => TabGarden,
            _ => TabGround
        };
        activeBtn.BackgroundColor = Color.FromArgb("#1B3A6B");
        activeBtn.TextColor = Colors.White;

        // Update floor title
        FloorTitle.Text = floor switch
        {
            "Ground Floor" => "Tầng trệt",
            "Second Floor" => "Tầng 2",
            "Garden" => "Sân vườn",
            _ => "Tầng trệt"
        };
    }

    private void RefreshFilteredTables()
    {
        if (Interlocked.Exchange(ref _refreshScheduled, 1) == 1)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var tables = AppContext.Instance.Tables
                    .Where(t => t.Floor == _currentFloor)
                    .OrderBy(t => t.Number)
                    .ToList();
                FilteredTables = new ObservableCollection<Table>(tables);

                TablesCollection.ItemsSource = FilteredTables;
            }
            finally
            {
                Interlocked.Exchange(ref _refreshScheduled, 0);
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // EDIT MODE TOGGLE
    // ═══════════════════════════════════════════════════════════════════

    private void OnEditToggleClicked(object sender, EventArgs e)
    {
        _isEditMode = !_isEditMode;

        EditModeBanner.IsVisible = _isEditMode;
        AddTableButton.IsVisible = _isEditMode;

        if (_isEditMode)
        {
            EditButton.Text = "Hoàn tất";
            EditButton.BackgroundColor = Color.FromArgb("#EF4444");
        }
        else
        {
            EditButton.Text = "Chỉnh sửa";
            EditButton.BackgroundColor = Color.FromArgb("#1B3A6B");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // TABLE TAP → DIFFERENT BEHAVIOR PER STATUS
    // ═══════════════════════════════════════════════════════════════════

    private async void OnTableTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Table table) return;

        _selectedTable = table;

        // In edit mode, show the edit action modal
        if (_isEditMode)
        {
            ActionModalTitle.Text = table.DisplayNumber;
            TableActionModal.IsVisible = true;
            return;
        }

        // Normal mode: behavior depends on table status
        switch (table.Status)
        {
            case TableStatus.Occupied:
                // Both "Có khách" (red) and "Đã gọi món" (blue) show the same toast
                await DisplayAlert("Thông báo", "Bàn này đã có khách.", "Đóng");
                break;

            case TableStatus.Available:
                // Green table → show Reservation modal to reserve
                ShowReservationModal(isEditing: false);
                break;

            case TableStatus.NeedsClearing:
                // Gray table → show alert
                await DisplayAlert("Thông báo", "Bàn phải được dọn trước khi đặt chỗ.", "Đóng");
                break;

            case TableStatus.Reserved:
                // Yellow table → show reserved options modal
                ReservedOptionsTitle.Text = table.DisplayNumber;
                ReservedOptionsModal.IsVisible = true;
                break;
        }
    }

    private void OnTableActionClose(object sender, EventArgs e)
    {
        TableActionModal.IsVisible = false;
        _selectedTable = null;
    }

    // ═══════════════════════════════════════════════════════════════════
    // RESERVED TABLE OPTIONS MODAL
    // ═══════════════════════════════════════════════════════════════════

    private void OnReservedOptionsClose(object sender, EventArgs e)
    {
        ReservedOptionsModal.IsVisible = false;
        _selectedTable = null;
    }

    private async void OnCancelReservationClicked(object sender, EventArgs e)
    {
        if (_selectedTable == null) return;

        bool confirmed = await DisplayAlert(
            "Xác nhận hủy đặt bàn",
            $"Bạn có chắc chắn muốn hủy đặt bàn {_selectedTable.DisplayNumber}?",
            "Xác nhận", "Hủy");

        if (!confirmed) return;

        var previous = (_selectedTable.Status, _selectedTable.ReservedFor,
            _selectedTable.ReservedPhone, _selectedTable.ReservedAt);
        try
        {
            _selectedTable.Status = TableStatus.Available;
            _selectedTable.ReservedFor = null;
            _selectedTable.ReservedPhone = null;
            _selectedTable.ReservedAt = null;

            await _firebaseService.UpdateTableAsync(_selectedTable).WaitAsync(TimeSpan.FromSeconds(12));

            ReservedOptionsModal.IsVisible = false;
            _selectedTable = null;
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            _selectedTable.Status = previous.Status;
            _selectedTable.ReservedFor = previous.ReservedFor;
            _selectedTable.ReservedPhone = previous.ReservedPhone;
            _selectedTable.ReservedAt = previous.ReservedAt;
            await DisplayAlert("Lỗi", $"Không thể hủy đặt bàn: {ex.Message}", "Đóng");
        }
    }

    private void OnEditReservationClicked(object sender, EventArgs e)
    {
        if (_selectedTable == null) return;

        ReservedOptionsModal.IsVisible = false;

        // Show reservation modal in edit mode with pre-filled data
        ShowReservationModal(isEditing: true);
    }

    // ═══════════════════════════════════════════════════════════════════
    // RESERVATION MODAL (shared for new reservation & editing)
    // ═══════════════════════════════════════════════════════════════════

    private void ShowReservationModal(bool isEditing)
    {
        _isEditingReservation = isEditing;

        if (isEditing && _selectedTable != null)
        {
            // Pre-fill with existing reservation data
            ReservationModalTitle.Text = $"Chỉnh sửa đặt bàn - {_selectedTable.DisplayNumber}";
            ReserveNameEntry.Text = _selectedTable.ReservedFor ?? string.Empty;
            ReservePhoneEntry.Text = _selectedTable.ReservedPhone ?? string.Empty;
            ReserveTimePicker.Time = _selectedTable.ReservedAt?.TimeOfDay ?? DateTime.Now.TimeOfDay;
        }
        else
        {
            // New reservation - clear fields
            ReservationModalTitle.Text = "Đặt trước bàn";
            ReserveNameEntry.Text = string.Empty;
            ReservePhoneEntry.Text = string.Empty;
            ReserveTimePicker.Time = DateTime.Now.TimeOfDay;
        }

        ReserveErrorLabel.IsVisible = false;
        ReservationModal.IsVisible = true;
    }

    private void OnReservationCancel(object sender, EventArgs e)
    {
        ReservationModal.IsVisible = false;
        _isEditingReservation = false;
    }

    private async void OnReservationConfirm(object sender, EventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(ReserveNameEntry.Text))
        {
            ReserveErrorLabel.Text = "Vui lòng nhập họ và tên.";
            ReserveErrorLabel.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(ReservePhoneEntry.Text))
        {
            ReserveErrorLabel.Text = "Vui lòng nhập số điện thoại.";
            ReserveErrorLabel.IsVisible = true;
            return;
        }

        if (_selectedTable == null) return;

        var previous = (_selectedTable.Status, _selectedTable.ReservedFor,
            _selectedTable.ReservedPhone, _selectedTable.ReservedAt);
        try
        {
            var reserveTime = DateTime.Today.Add(ReserveTimePicker.Time ?? DateTime.Now.TimeOfDay);

            _selectedTable.Status = TableStatus.Reserved;
            _selectedTable.ReservedFor = ReserveNameEntry.Text.Trim();
            _selectedTable.ReservedPhone = ReservePhoneEntry.Text.Trim();
            _selectedTable.ReservedAt = reserveTime;

            await _firebaseService.UpdateTableAsync(_selectedTable).WaitAsync(TimeSpan.FromSeconds(12));

            ReservationModal.IsVisible = false;
            _isEditingReservation = false;
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            _selectedTable.Status = previous.Status;
            _selectedTable.ReservedFor = previous.ReservedFor;
            _selectedTable.ReservedPhone = previous.ReservedPhone;
            _selectedTable.ReservedAt = previous.ReservedAt;
            ReserveErrorLabel.Text = $"Lỗi: {ex.Message}";
            ReserveErrorLabel.IsVisible = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // EDIT MODE: ADD TABLE
    // ═══════════════════════════════════════════════════════════════════

    private void OnAddTableClicked(object sender, EventArgs e)
    {
        NewTableNumberEntry.Text = string.Empty;
        NewTableCapacityEntry.Text = string.Empty;
        AddTableErrorLabel.IsVisible = false;

        AddTableModal.IsVisible = true;
    }

    private void OnAddTableCancel(object sender, EventArgs e)
    {
        AddTableModal.IsVisible = false;
    }

    private async void OnAddTableConfirm(object sender, EventArgs e)
    {
        // Validate table number
        if (string.IsNullOrWhiteSpace(NewTableNumberEntry.Text) ||
            !int.TryParse(NewTableNumberEntry.Text.Trim(), out int tableNumber) ||
            tableNumber <= 0)
        {
            AddTableErrorLabel.Text = "Vui lòng nhập số bàn hợp lệ (số nguyên dương).";
            AddTableErrorLabel.IsVisible = true;
            return;
        }

        // Check if table number already exists
        if (AppContext.Instance.Tables.Any(t => t.Number == tableNumber))
        {
            AddTableErrorLabel.Text = $"Bàn số {tableNumber} đã tồn tại. Vui lòng chọn số khác.";
            AddTableErrorLabel.IsVisible = true;
            return;
        }

        // Validate capacity
        if (string.IsNullOrWhiteSpace(NewTableCapacityEntry.Text) ||
            !int.TryParse(NewTableCapacityEntry.Text.Trim(), out int capacity) ||
            capacity < 1 || capacity > 50)
        {
            AddTableErrorLabel.Text = "Số chỗ ngồi phải nằm trong khoảng từ 1 đến 50.";
            AddTableErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            var newTable = new Table
            {
                Id = tableNumber,
                Number = tableNumber,
                Floor = _currentFloor,
                Status = TableStatus.Available,
                Capacity = capacity
            };

            await _firebaseService.CreateTableAsync(newTable);

            // Add to local collection (real-time listener will also pick it up)
            if (!AppContext.Instance.Tables.Any(t => t.Id == newTable.Id))
            {
                AppContext.Instance.Tables.Add(newTable);
            }

            AddTableModal.IsVisible = false;
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            AddTableErrorLabel.Text = $"Lỗi: {ex.Message}";
            AddTableErrorLabel.IsVisible = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // EDIT MODE: EDIT CAPACITY
    // ═══════════════════════════════════════════════════════════════════

    private void OnEditCapacityClicked(object sender, EventArgs e)
    {
        TableActionModal.IsVisible = false;

        EditCapacityEntry.Text = _selectedTable?.Capacity.ToString() ?? string.Empty;
        EditCapacityErrorLabel.IsVisible = false;

        EditCapacityModal.IsVisible = true;
    }

    private void OnEditCapacityCancel(object sender, EventArgs e)
    {
        EditCapacityModal.IsVisible = false;
    }

    private async void OnEditCapacityConfirm(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EditCapacityEntry.Text) ||
            !int.TryParse(EditCapacityEntry.Text.Trim(), out int newCapacity) ||
            newCapacity < 1 || newCapacity > 50)
        {
            EditCapacityErrorLabel.Text = "Số chỗ ngồi phải nằm trong khoảng từ 1 đến 50.";
            EditCapacityErrorLabel.IsVisible = true;
            return;
        }

        if (_selectedTable == null) return;

        try
        {
            var previousCapacity = _selectedTable.Capacity;
            _selectedTable.Capacity = newCapacity;
            try
            {
                await _firebaseService.UpdateTableAsync(_selectedTable).WaitAsync(TimeSpan.FromSeconds(12));
            }
            catch
            {
                _selectedTable.Capacity = previousCapacity;
                throw;
            }

            EditCapacityModal.IsVisible = false;
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            EditCapacityErrorLabel.Text = $"Lỗi: {ex.Message}";
            EditCapacityErrorLabel.IsVisible = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // EDIT MODE: DELETE TABLE
    // ═══════════════════════════════════════════════════════════════════

    private async void OnDeleteTableClicked(object sender, EventArgs e)
    {
        if (_selectedTable == null) return;

        // Block deletion of tables with active orders (Đã gọi món)
        if (HasActiveOrder(_selectedTable))
        {
            await DisplayAlert("Không thể xóa bàn", "Bàn đang có đơn hoạt động. Hãy thanh toán hoặc xử lý đơn trước.", "Đóng");
            return;
        }

        // Block deletion of tables with status "Có khách" (Occupied, not ordered)
        if (_selectedTable.Status == TableStatus.Occupied && !_selectedTable.HasOrdered)
        {
            await DisplayAlert("Không thể xóa bàn", "Bàn đang có khách ngồi. Không thể xóa bàn khi đang phục vụ khách.", "Đóng");
            return;
        }

        // Block deletion of tables with status "Đặt trước" (Reserved)
        if (_selectedTable.Status == TableStatus.Reserved)
        {
            await DisplayAlert("Không thể xóa bàn", "Bàn đã được đặt trước. Vui lòng hủy đặt bàn trước khi xóa.", "Đóng");
            return;
        }

        TableActionModal.IsVisible = false;

        DeleteConfirmLabel.Text = $"Bạn có muốn xóa bàn {_selectedTable?.DisplayNumber} không?";
        DeleteTableModal.IsVisible = true;
    }

    private void OnDeleteTableCancel(object sender, EventArgs e)
    {
        DeleteTableModal.IsVisible = false;
    }

    private async void OnDeleteTableConfirm(object sender, EventArgs e)
    {
        if (_selectedTable == null) return;

        if (HasActiveOrder(_selectedTable))
        {
            DeleteTableModal.IsVisible = false;
            await DisplayAlert("Không thể xóa bàn", "Bàn vừa phát sinh đơn hoạt động nên thao tác xóa đã bị chặn.", "Đóng");
            return;
        }

        if (_selectedTable.Status == TableStatus.Occupied)
        {
            DeleteTableModal.IsVisible = false;
            await DisplayAlert("Không thể xóa bàn", "Bàn vừa chuyển sang trạng thái có khách nên thao tác xóa đã bị chặn.", "Đóng");
            return;
        }

        if (_selectedTable.Status == TableStatus.Reserved)
        {
            DeleteTableModal.IsVisible = false;
            await DisplayAlert("Không thể xóa bàn", "Bàn vừa được đặt trước nên thao tác xóa đã bị chặn.", "Đóng");
            return;
        }

        try
        {
            await _firebaseService.DeleteTableAsync(_selectedTable.Id);

            // Remove from local collection
            var toRemove = AppContext.Instance.Tables.FirstOrDefault(t => t.Id == _selectedTable.Id);
            if (toRemove != null)
                AppContext.Instance.Tables.Remove(toRemove);

            DeleteTableModal.IsVisible = false;
            _selectedTable = null;
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể xóa bàn: {ex.Message}", "Đóng");
        }
    }

    private static bool HasActiveOrder(Table table)
        => table.HasOrdered
           || table.CurrentOrderId.HasValue
           || AppContext.Instance.Orders.Any(order =>
               order.Status == OrderStatus.Active &&
               (order.TableId == table.Id || order.TableNumber == table.Number));
}
