using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Constants;

namespace AppManagermentRestaurant.Views.Pages;

public partial class TableManagementPage : ContentPage
{
    private readonly TableFilterViewModel _filterViewModel = new();
    private readonly FirebaseService _firebase = new();

    public TableManagementPage()
    {
        InitializeComponent();
        BindingContext = _filterViewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await LoadPageDataAsync();
    }

    private async Task LoadPageDataAsync()
    {
        await Task.Yield();
        await _filterViewModel.RefreshTablesAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _filterViewModel.SearchTables(e.NewTextValue);
    }

    private void OnAreaFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string area })
        {
            _filterViewModel.FilterByArea(area);
        }
    }

    private void OnStatusFilterTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string status)
        {
            _filterViewModel.ToggleStatusFilter(status);
        }
    }

    private async void OnReceiveGuestClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table table)
        {
            var confirmSeat = await DisplayAlert("Nhận khách", $"Bạn muốn nhận khách vào bàn {table.Number}?", "Xác nhận", "Hủy");
            if (!confirmSeat) return;

            table.Status = TableStatus.Occupied;
            table.ArrivalTime = DateTime.Now;
            table.HasOrdered = false;
            AppContext.Instance.SelectedTable = table;
            _ = _firebase.UpdateTableAsync(table);

            await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.CreateOrder));
        }
    }

    private async void OnCheckInTableClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table table)
        {
            bool confirm = await DisplayAlert("Nhận bàn đặt", $"Khách {table.ReservedFor} đã đến nhận bàn {table.DisplayNumber}?", "Xác nhận", "Hủy");
            if (confirm)
            {
                table.Status = TableStatus.Occupied;
                table.ArrivalTime = DateTime.Now;
                table.HasOrdered = false;
                _ = _firebase.UpdateTableAsync(table);
                await _filterViewModel.RefreshTablesAsync();
            }
        }
    }

    private async void OnChangeTableClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table currentTable)
        {
            var availableTables = AppContext.Instance.Tables.Where(t => t.Status == TableStatus.Available).ToList();
            if (!availableTables.Any())
            {
                await DisplayAlert("Thông báo", "Hiện tại không còn bàn trống để đổi.", "Đóng");
                return;
            }

            var tableNames = availableTables.Select(t => t.DisplayNumber).ToArray();
            var selectedTableName = await DisplayActionSheet($"Chuyển khách bàn {currentTable.DisplayNumber} sang:", "Hủy", null, tableNames);

            if (selectedTableName != "Hủy" && !string.IsNullOrEmpty(selectedTableName))
            {
                var targetTable = availableTables.First(t => t.DisplayNumber == selectedTableName);

                targetTable.Status = currentTable.Status;
                targetTable.CurrentOrderId = currentTable.CurrentOrderId;
                targetTable.ArrivalTime = currentTable.ArrivalTime;
                targetTable.HasOrdered = currentTable.HasOrdered;
                targetTable.OrderItemCount = currentTable.OrderItemCount;
                targetTable.OrderTotal = currentTable.OrderTotal;

                var orderToTransfer = AppContext.Instance.Orders.FirstOrDefault(o => o.Id == currentTable.CurrentOrderId);
                if (orderToTransfer != null)
                {
                    orderToTransfer.TableId = targetTable.Id;
                    orderToTransfer.TableNumber = targetTable.Number;
                }

                currentTable.Status = TableStatus.NeedsClearing;
                currentTable.CurrentOrderId = null;
                currentTable.ArrivalTime = null;
                currentTable.HasOrdered = false;
                currentTable.OrderItemCount = 0;
                currentTable.OrderTotal = string.Empty;

                _ = _firebase.UpdateTableAsync(targetTable);
                _ = _firebase.UpdateTableAsync(currentTable);

                await _filterViewModel.RefreshTablesAsync();
                await DisplayAlert("Thành công", $"Đã chuyển khách sang {targetTable.DisplayNumber}.", "OK");
            }
        }
    }

    private async void OnPayTableClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table table)
        {
            AppContext.Instance.SelectedTable = table;
            AppContext.Instance.SelectedOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.TableId == table.Id && o.Status == OrderStatus.Active)
                                                ?? AppContext.Instance.Orders.FirstOrDefault(o => o.TableNumber == table.Number && o.Status == OrderStatus.Active);
            await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.Payment));
        }
    }

    private async void OnOrderTableClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table table)
        {
            AppContext.Instance.SelectedTable = table;
            await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.CreateOrder));
        }
    }

    private async void OnClearEmptyTableClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table table)
        {
            bool confirm = await DisplayAlert("Làm trống bàn", $"Bàn {table.Number} chưa gọi món. Bạn muốn làm trống bàn?", "Đồng ý", "Hủy");
            if (confirm)
            {
                table.Status = TableStatus.Available;
                table.CurrentOrderId = null;
                table.ArrivalTime = null;
                table.HasOrdered = false;
                _ = _firebase.UpdateTableAsync(table);

                var draftOrder = AppContext.Instance.Orders.FirstOrDefault(o => o.TableId == table.Id && o.Status == OrderStatus.Active)
                              ?? AppContext.Instance.Orders.FirstOrDefault(o => o.TableNumber == table.Number && o.Status == OrderStatus.Active);
                if (draftOrder != null)
                {
                    AppContext.Instance.Orders.Remove(draftOrder);
                }

                await _filterViewModel.RefreshTablesAsync();
            }
        }
    }

    private async void OnCleanTableClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Table table)
        {
            var confirmClean = await DisplayAlert("Đánh dấu dọn xong", $"Xác nhận bàn {table.Number} đã dọn dẹp xong?", "Xác nhận", "Hủy");
            if (!confirmClean) return;

            table.Status = TableStatus.Available;
            _ = _firebase.UpdateTableAsync(table);
            await _filterViewModel.RefreshTablesAsync();
        }
    }
}

public class TableFilterViewModel : ObservableObject
{
    private string _selectedStatusFilter = string.Empty;
    private string _selectedAreaFilter = "All";
    private string _searchQuery = string.Empty;
    private bool _showGroundFloor = true;
    private bool _showSecondFloor = true;
    private bool _showGarden = true;

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set => SetProperty(ref _selectedStatusFilter, value);
    }

    public string SelectedAreaFilter
    {
        get => _selectedAreaFilter;
        set => SetProperty(ref _selectedAreaFilter, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public bool ShowGroundFloor
    {
        get => _showGroundFloor;
        set => SetProperty(ref _showGroundFloor, value);
    }

    public bool ShowSecondFloor
    {
        get => _showSecondFloor;
        set => SetProperty(ref _showSecondFloor, value);
    }

    public bool ShowGarden
    {
        get => _showGarden;
        set => SetProperty(ref _showGarden, value);
    }

    public int AvailableCount => GetFilteredTables(t => t.Status == TableStatus.Available).Count();
    public int OccupiedCount => GetFilteredTables(t => t.Status == TableStatus.Occupied && !t.HasOrdered).Count();
    public int OrderedCount => GetFilteredTables(t => t.Status == TableStatus.Occupied && t.HasOrdered).Count();
    public int ReservedCount => GetFilteredTables(t => t.Status == TableStatus.Reserved).Count();
    public int NeedsClearingCount => GetFilteredTables(t => t.Status == TableStatus.NeedsClearing).Count();

    public IEnumerable<Table> FilteredTables => GetAllFilteredTables();
    public bool HasFilteredTables => FilteredTables.Any();

    public IEnumerable<Table> FilteredGroundFloorTables => GetFilteredTablesByArea("Ground Floor");
    public IEnumerable<Table> FilteredSecondFloorTables => GetFilteredTablesByArea("Second Floor");
    public IEnumerable<Table> FilteredGardenTables => GetFilteredTablesByArea("Garden");

    public IEnumerable<Table> Tables => AppContext.Instance.Tables;

    public async Task RefreshTablesAsync()
    {
        await Task.Yield();
        RefreshAllData();
    }

    public void ToggleStatusFilter(string status)
    {
        SelectedStatusFilter = SelectedStatusFilter == status ? string.Empty : status;
        RefreshAllData();
    }

    public void SearchTables(string? query)
    {
        SearchQuery = (query ?? string.Empty).Trim();
        RefreshAllData();
    }

    public void FilterByArea(string area)
    {
        SelectedAreaFilter = area;
        UpdateVisibleAreas();
        RefreshAllData();
    }

    private IEnumerable<Table> GetFilteredTablesByArea(string area)
    {
        var tables = AppContext.Instance.Tables.Where(t => t.Floor == area);
        return ApplyAllFilters(tables);
    }

    private IEnumerable<Table> GetAllFilteredTables()
    {
        return ApplyAllFilters(AppContext.Instance.Tables);
    }

    private IEnumerable<Table> ApplyAllFilters(IEnumerable<Table> tables)
    {
        if (SelectedAreaFilter != "All")
        {
            tables = tables.Where(t => t.Floor == SelectedAreaFilter);
        }

        if (!string.IsNullOrEmpty(SelectedStatusFilter))
        {
            if (SelectedStatusFilter == "Ordered")
                tables = tables.Where(t => t.Status == TableStatus.Occupied && t.HasOrdered);
            else if (SelectedStatusFilter == "Occupied")
                tables = tables.Where(t => t.Status == TableStatus.Occupied && !t.HasOrdered);
            else
                tables = tables.Where(t => GetStatusString(t.Status) == SelectedStatusFilter);
        }

        if (!string.IsNullOrEmpty(SearchQuery))
        {
            var query = SearchQuery.ToLowerInvariant();
            tables = tables.Where(t =>
                t.DisplayNumber.ToLowerInvariant().Contains(query) ||
                t.Floor.ToLowerInvariant().Contains(query));
        }

        return tables;
    }

    private IEnumerable<Table> GetFilteredTables(Func<Table, bool> predicate)
    {
        return AppContext.Instance.Tables.Where(predicate);
    }

    private static string GetStatusString(TableStatus status) => status switch
    {
        TableStatus.Available => "Available",
        TableStatus.Reserved => "Reserved",
        TableStatus.NeedsClearing => "NeedsClearing",
        _ => string.Empty
    };

    private void UpdateVisibleAreas()
    {
        ShowGroundFloor = SelectedAreaFilter == "All" || SelectedAreaFilter == "Ground Floor";
        ShowSecondFloor = SelectedAreaFilter == "All" || SelectedAreaFilter == "Second Floor";
        ShowGarden = SelectedAreaFilter == "All" || SelectedAreaFilter == "Garden";
    }

    private void RefreshAllData()
    {
        OnPropertyChanged(nameof(FilteredTables));
        OnPropertyChanged(nameof(FilteredGroundFloorTables));
        OnPropertyChanged(nameof(FilteredSecondFloorTables));
        OnPropertyChanged(nameof(FilteredGardenTables));
        OnPropertyChanged(nameof(HasFilteredTables));
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(OccupiedCount));
        OnPropertyChanged(nameof(OrderedCount));
        OnPropertyChanged(nameof(ReservedCount));
        OnPropertyChanged(nameof(NeedsClearingCount));
    }
}