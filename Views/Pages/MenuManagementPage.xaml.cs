using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Views.Pages;

public partial class MenuManagementPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new();
    private string _searchText = string.Empty;
    private string _categoryFilter = "All";
    private bool _isObservingMenu;

    public ObservableCollection<FoodItem> FilteredMenuItems { get; private set; } = new();
    public int TotalMenuItemCount => AppContext.Instance.MenuItems.Count;
    public int AvailableMenuItemCount => AppContext.Instance.MenuItems.Count(item => item.Available && !item.OutOfStock);
    public int OutOfStockMenuItemCount => AppContext.Instance.MenuItems.Count(item => !item.Available || item.OutOfStock);

    public MenuManagementPage()
    {
        InitializeComponent();
        BindingContext = this;
        RefreshMenu();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObservingMenu)
        {
            AppContext.Instance.MenuItems.CollectionChanged += OnMenuCollectionChanged;
            _isObservingMenu = true;
        }
        RefreshMenu();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObservingMenu)
        {
            AppContext.Instance.MenuItems.CollectionChanged -= OnMenuCollectionChanged;
            _isObservingMenu = false;
        }
        base.OnNavigatingFrom(args);
    }

    private void OnMenuCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => RefreshMenu();

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;
        RefreshMenu();
    }

    private void OnCategoryFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string category })
        {
            _categoryFilter = category;
            RefreshMenu();
        }
    }

    private void RefreshMenu()
    {
        var items = AppContext.Instance.MenuItems.AsEnumerable();
        if (_categoryFilter != "All")
            items = items.Where(item => MenuCategoryHelper.Matches(item.Category, _categoryFilter));
        if (!string.IsNullOrWhiteSpace(_searchText))
            items = items.Where(item => item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

        FilteredMenuItems = new ObservableCollection<FoodItem>(items.OrderBy(item => item.Name));
        OnPropertyChanged(nameof(FilteredMenuItems));

        OnPropertyChanged(nameof(TotalMenuItemCount));
        OnPropertyChanged(nameof(AvailableMenuItemCount));
        OnPropertyChanged(nameof(OutOfStockMenuItemCount));
    }

    private async void OnAddMenuItemClicked(object sender, EventArgs e)
    {
        AddMenuItemPage.PendingEditItem = null;
        await Shell.Current.GoToAsync("add-menu-item");
    }

    private async void OnEditMenuItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is FoodItem item)
        {
            // Truyền trực tiếp object FoodItem, không dùng URL query parameter
            AddMenuItemPage.PendingEditItem = item;
            await Shell.Current.GoToAsync("edit-menu-item");
        }
    }

    private async void OnDeleteMenuItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is FoodItem item)
        {
            bool confirm = await DisplayAlert(
                "Xác nhận xóa",
                $"Bạn có chắc chắn muốn xóa '{item.Name}'?",
                "Xóa", "Hủy");

            if (confirm)
            {
                try
                {
                    await _firebaseService.DeleteMenuItemAsync(item.Id);
                    AppContext.Instance.MenuItems.Remove(item);
                    await DisplayAlert("Thành công", "Xóa món ăn thành công!", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Lỗi", $"Lỗi xóa món ăn: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnToggleStatusClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is FoodItem item)
        {
            try
            {
                button.IsEnabled = false;
                item.OutOfStock = !item.OutOfStock;
                RefreshMenu();
                await _firebaseService.SaveMenuItemAsync(item).WaitAsync(TimeSpan.FromSeconds(12));

                // Thông báo
                string status = item.OutOfStock ? "Hết hàng" : "Còn hàng";
                await DisplayAlert("Thành công", $"Cập nhật thành công: {status}", "OK");

            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi cập nhật trạng thái: {ex.Message}", "OK");
                item.OutOfStock = !item.OutOfStock; // Revert
                RefreshMenu();
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }
}

