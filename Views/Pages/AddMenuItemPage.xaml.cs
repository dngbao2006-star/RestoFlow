using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.ViewModels;
using System.Diagnostics;

namespace AppManagermentRestaurant.Views.Pages;

public partial class AddMenuItemPage : ContentPage
{
    private AddMenuItemViewModel _viewModel;

    public AddMenuItemPage()
    {
        InitializeComponent();
        _viewModel = new AddMenuItemViewModel();
        BindingContext = _viewModel;
    }

    public AddMenuItemPage(FoodItem editingItem) : this()
    {
        _viewModel.LoadMenuItem(editingItem);
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        try
        {
            // Lấy id từ query parameter
            var location = Shell.Current.CurrentState.Location.ToString();
            Debug.WriteLine($"[DEBUG] Location: {location}");

            if (location.Contains("edit-menu-item") && location.Contains("id="))
            {
                var idParam = ExtractQueryParameter(location, "id");
                Debug.WriteLine($"[DEBUG] Extracted ID Parameter: {idParam}");

                if (!string.IsNullOrEmpty(idParam) && int.TryParse(idParam, out int id))
                {
                    Debug.WriteLine($"[DEBUG] Loading menu item with ID: {id}");
                    Debug.WriteLine($"[DEBUG] AppContext MenuItems count: {AppContext.Instance.MenuItems.Count}");

                    // Load dữ liệu trực tiếp
                    LoadMenuItemById(id);

                    Debug.WriteLine($"[DEBUG] Menu item loaded. Name: {_viewModel.Name}, Price: {_viewModel.Price}");
                }
                else
                {
                    Debug.WriteLine("[DEBUG] ID parameter is null or not valid integer");
                }
            }
            else
            {
                Debug.WriteLine("[DEBUG] Not edit-menu-item route");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] Exception in OnNavigatedTo: {ex.Message}");
        }
    }

    private string ExtractQueryParameter(string query, string paramName)
    {
        try
        {
            var startIndex = query.IndexOf($"{paramName}=", StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0) return string.Empty;

            startIndex += paramName.Length + 1;
            var endIndex = query.IndexOf("&", startIndex);
            if (endIndex < 0) endIndex = query.Length;

            var result = query.Substring(startIndex, endIndex - startIndex);
            Debug.WriteLine($"[DEBUG] ExtractQueryParameter - {paramName}={result}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] Error extracting parameter: {ex.Message}");
            return string.Empty;
        }
    }

    private void LoadMenuItemById(int id)
    {
        var menuItem = AppContext.Instance.MenuItems.FirstOrDefault(m => m.Id == id);
        if (menuItem != null)
        {
            Debug.WriteLine($"[DEBUG] Found menu item: Id={menuItem.Id}, Name={menuItem.Name}, Price={menuItem.Price}");
            _viewModel.LoadMenuItem(menuItem);
            Debug.WriteLine($"[DEBUG] ViewModel after LoadMenuItem - Name={_viewModel.Name}, Price={_viewModel.Price}, Category={_viewModel.Category}");
        }
        else
        {
            Debug.WriteLine($"[DEBUG] Menu item with ID {id} NOT FOUND in AppContext.MenuItems");
            Debug.WriteLine($"[DEBUG] Available IDs in MenuItems: {string.Join(", ", AppContext.Instance.MenuItems.Select(m => m.Id))}");
        }
    }

    private async Task AnimateLoadComplete()
    {
        // Optional: Thêm animation khi load xong
        await this.FadeTo(1, 300, Easing.CubicOut);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        await _viewModel.SaveMenuItemAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
