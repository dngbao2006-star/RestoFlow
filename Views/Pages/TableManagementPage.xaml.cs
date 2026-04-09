using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Views.Pages;

/// <summary>
/// TableManagement page - Enhanced UX with status filtering, search, and area filtering
/// 1.1 - Status summary cards with clickable filters
/// 1.2 - Real-time table search
/// 1.3 - Area/floor filtering
/// 1.4 - Grouped table display by area with full information
/// 1.5 - Different behaviors for different table states
/// </summary>
public partial class TableManagementPage : ContentPage
{
    private readonly TableFilterViewModel _filterViewModel = new();

    public TableManagementPage()
    {
        InitializeComponent();
        BindingContext = _filterViewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _filterViewModel.RefreshTables();
    }

    /// 1.2 - Real-time table search
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _filterViewModel.SearchTables(e.NewTextValue);
    }

    /// 1.3 - Area filter button clicked
    private void OnAreaFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string area)
        {
            _filterViewModel.FilterByArea(area);
        }
    }

    /// 1.1 - Status filter clicked (clickable summary cards)
    private void OnStatusFilterTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string status)
        {
            _filterViewModel.ToggleStatusFilter(status);
        }
    }

    /// 1.5 - Table clicked - Different behaviors based on state
    private async void OnTableTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Table table)
            return;

        switch (table.Status)
        {
            case TableStatus.Available:
                // Empty table: Ask confirmation to seat guest, then go to order creation
                var confirmSeat = await DisplayAlert(
                    "Xếp khách",
                    $"Xếp khách vào bàn {table.Number}?",
                    "Xác nhận",
                    "Hủy");

                if (confirmSeat)
                {
                    table.Status = TableStatus.Occupied;
                    AppContext.Instance.SelectedTable = table;
                    await Shell.Current.GoToAsync("staff/orders");
                }
                break;

            case TableStatus.Occupied:
                // Occupied table: Go directly to order creation to view/add items
                AppContext.Instance.SelectedTable = table;
                await Shell.Current.GoToAsync("staff/orders");
                break;

            case TableStatus.NeedsClearing:
                // Needs clearing: Ask confirmation to mark as cleaned
                var confirmClean = await DisplayAlert(
                    "Đánh dấu dọn xong",
                    $"Đánh dấu bàn {table.Number} là đã dọn xong?",
                    "Xác nhận",
                    "Hủy");

                if (confirmClean)
                {
                    table.Status = TableStatus.Available;
                    _filterViewModel.RefreshTables();
                }
                break;

            case TableStatus.Reserved:
                // Reserved table: Non-clickable, just show message
                await DisplayAlert(
                    "Bàn đã đặt",
                    $"Bàn {table.Number} đã được đặt cho {table.ReservedFor}.",
                    "OK");
                break;
        }
    }
}

/// <summary>
/// ViewModel for table management with filtering, search, and status
/// </summary>
public class TableFilterViewModel : ObservableObject
{
    private string _selectedStatusFilter = ""; // "" = no filter, "Available", "Occupied", "Reserved", "NeedsClearing"
    private string _selectedAreaFilter = "All";
    private string _searchQuery = "";
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

    // Summary counts
    public int AvailableCount => GetFilteredTables(t => t.Status == TableStatus.Available).Count();
    public int OccupiedCount => GetFilteredTables(t => t.Status == TableStatus.Occupied).Count();
    public int ReservedCount => GetFilteredTables(t => t.Status == TableStatus.Reserved).Count();
    public int NeedsClearingCount => GetFilteredTables(t => t.Status == TableStatus.NeedsClearing).Count();

    // All tables with current filters applied
    public IEnumerable<Table> FilteredTables => GetAllFilteredTables();
    public bool HasFilteredTables => FilteredTables.Any();

    // Tables grouped by area with filters
    public IEnumerable<Table> FilteredGroundFloorTables => GetFilteredTablesByArea("Ground Floor");
    public IEnumerable<Table> FilteredSecondFloorTables => GetFilteredTablesByArea("Second Floor");
    public IEnumerable<Table> FilteredGardenTables => GetFilteredTablesByArea("Garden");

    public IEnumerable<Table> Tables => AppContext.Instance.Tables;

    /// <summary>
    /// Get tables filtered by area
    /// </summary>
    private IEnumerable<Table> GetFilteredTablesByArea(string area)
    {
        var tables = AppContext.Instance.Tables
            .Where(t => t.Floor == area);
        return ApplyAllFilters(tables);
    }

    /// <summary>
    /// Get all tables with all filters applied
    /// </summary>
    private IEnumerable<Table> GetAllFilteredTables()
    {
        return ApplyAllFilters(AppContext.Instance.Tables);
    }

    /// <summary>
    /// Apply search, status, and area filters to a collection
    /// </summary>
    private IEnumerable<Table> ApplyAllFilters(IEnumerable<Table> tables)
    {
        // Apply area filter
        if (SelectedAreaFilter != "All")
        {
            tables = tables.Where(t => t.Floor == SelectedAreaFilter);
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(SelectedStatusFilter))
        {
            tables = tables.Where(t => GetStatusString(t.Status) == SelectedStatusFilter);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(SearchQuery))
        {
            var query = SearchQuery.ToLower();
            tables = tables.Where(t =>
                t.DisplayNumber.ToLower().Contains(query) ||
                t.Floor.ToLower().Contains(query));
        }

        return tables;
    }

    /// <summary>
    /// Get filtered tables by predicate
    /// </summary>
    private IEnumerable<Table> GetFilteredTables(Func<Table, bool> predicate)
    {
        return AppContext.Instance.Tables.Where(predicate);
    }

    /// <summary>
    /// Convert TableStatus to string for filtering
    /// </summary>
    private string GetStatusString(TableStatus status) => status switch
    {
        TableStatus.Available => "Available",
        TableStatus.Occupied => "Occupied",
        TableStatus.Reserved => "Reserved",
        TableStatus.NeedsClearing => "NeedsClearing",
        _ => ""
    };

    /// <summary>
    /// 1.1 - Toggle status filter (click again to deselect)
    /// </summary>
    public void ToggleStatusFilter(string status)
    {
        if (SelectedStatusFilter == status)
        {
            SelectedStatusFilter = ""; // Deselect
        }
        else
        {
            SelectedStatusFilter = status;
        }

        RefreshAllData();
    }

    /// <summary>
    /// 1.2 - Search tables by number or area
    /// </summary>
    public void SearchTables(string query)
    {
        SearchQuery = query;
        RefreshAllData();
    }

    /// <summary>
    /// 1.3 - Filter by area/floor
    /// </summary>
    public void FilterByArea(string area)
    {
        SelectedAreaFilter = area;
        UpdateVisibleAreas();
        RefreshAllData();
    }

    /// <summary>
    /// Update which area sections are visible based on selected area
    /// </summary>
    private void UpdateVisibleAreas()
    {
        ShowGroundFloor = SelectedAreaFilter == "All" || SelectedAreaFilter == "Ground Floor";
        ShowSecondFloor = SelectedAreaFilter == "All" || SelectedAreaFilter == "Second Floor";
        ShowGarden = SelectedAreaFilter == "All" || SelectedAreaFilter == "Garden";
    }

    /// <summary>
    /// Refresh all filterable data
    /// </summary>
    public void RefreshTables()
    {
        RefreshAllData();
    }

    /// <summary>
    /// Trigger all property notifications to update UI
    /// </summary>
    private void RefreshAllData()
    {
        OnPropertyChanged(nameof(FilteredTables));
        OnPropertyChanged(nameof(FilteredGroundFloorTables));
        OnPropertyChanged(nameof(FilteredSecondFloorTables));
        OnPropertyChanged(nameof(FilteredGardenTables));
        OnPropertyChanged(nameof(HasFilteredTables));
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(OccupiedCount));
        OnPropertyChanged(nameof(ReservedCount));
        OnPropertyChanged(nameof(NeedsClearingCount));
    }
}
