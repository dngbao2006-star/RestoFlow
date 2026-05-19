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

    // Filtered tables for the current floor tab
    public ObservableCollection<Table> FilteredTables { get; } = new();

    public LayoutManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;

        _firebaseService = new FirebaseService();

        // Default to Ground Floor tab active
        SetActiveTab("Ground Floor");
        RefreshFilteredTables();

        // Listen for table collection changes to auto-refresh the grid
        AppContext.Instance.Tables.CollectionChanged += (_, _) => RefreshFilteredTables();
    }

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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FilteredTables.Clear();
            var tables = AppContext.Instance.Tables
                .Where(t => t.Floor == _currentFloor)
                .OrderBy(t => t.Number);
            foreach (var table in tables)
                FilteredTables.Add(table);

            TablesCollection.ItemsSource = FilteredTables;
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
    // TABLE TAP → SHOW ACTION BUTTONS
    // ═══════════════════════════════════════════════════════════════════

    private void OnTableTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Table table) return;

        _selectedTable = table;
        ActionModalTitle.Text = table.DisplayNumber;

        // Show different buttons based on mode
        NormalModeActions.IsVisible = !_isEditMode;
        EditModeActions.IsVisible = _isEditMode;

        TableActionModal.IsVisible = true;
    }

    private void OnTableActionClose(object sender, EventArgs e)
    {
        TableActionModal.IsVisible = false;
        _selectedTable = null;
    }

    // ═══════════════════════════════════════════════════════════════════
    // NORMAL MODE: RESERVATION
    // ═══════════════════════════════════════════════════════════════════

    private void OnReserveTableClicked(object sender, EventArgs e)
    {
        TableActionModal.IsVisible = false;

        // Reset fields
        ReserveNameEntry.Text = string.Empty;
        ReservePhoneEntry.Text = string.Empty;
        ReserveTimePicker.Time = DateTime.Now.TimeOfDay;
        ReserveErrorLabel.IsVisible = false;

        ReservationModal.IsVisible = true;
    }

    private void OnReservationCancel(object sender, EventArgs e)
    {
        ReservationModal.IsVisible = false;
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

        try
        {
            var reserveTime = DateTime.Today.Add(ReserveTimePicker.Time ?? DateTime.Now.TimeOfDay);

            _selectedTable.Status = TableStatus.Reserved;
            _selectedTable.ReservedFor = ReserveNameEntry.Text.Trim();
            _selectedTable.ReservedPhone = ReservePhoneEntry.Text.Trim();
            _selectedTable.ReservedAt = reserveTime;

            await _firebaseService.UpdateTableAsync(_selectedTable);

            ReservationModal.IsVisible = false;
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            ReserveErrorLabel.Text = $"Lỗi: {ex.Message}";
            ReserveErrorLabel.IsVisible = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // NORMAL MODE: CHECK-IN (Nhận bàn)
    // ═══════════════════════════════════════════════════════════════════

    private async void OnCheckInTableClicked(object sender, EventArgs e)
    {
        if (_selectedTable == null) return;

        TableActionModal.IsVisible = false;

        try
        {
            // Chuyển trạng thái từ "Đặt trước" sang "Chưa gọi món" (Occupied, HasOrdered = false)
            _selectedTable.Status = TableStatus.Occupied;
            _selectedTable.HasOrdered = false;
            _selectedTable.ArrivalTime = DateTime.Now;
            _selectedTable.ReservedFor = null;
            _selectedTable.ReservedPhone = null;
            _selectedTable.ReservedAt = null;

            await _firebaseService.UpdateTableAsync(_selectedTable);
            RefreshFilteredTables();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể nhận bàn: {ex.Message}", "Đóng");
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
            capacity < 1 || capacity > 6)
        {
            AddTableErrorLabel.Text = "Số chỗ ngồi phải nằm trong khoảng từ 1 đến 6.";
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
            newCapacity < 1 || newCapacity > 6)
        {
            EditCapacityErrorLabel.Text = "Số chỗ ngồi phải nằm trong khoảng từ 1 đến 6.";
            EditCapacityErrorLabel.IsVisible = true;
            return;
        }

        if (_selectedTable == null) return;

        try
        {
            _selectedTable.Capacity = newCapacity;
            await _firebaseService.UpdateTableAsync(_selectedTable);

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

    private void OnDeleteTableClicked(object sender, EventArgs e)
    {
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
}
