using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class AddMenuItemPage : ContentPage
{
    private AddMenuItemViewModel _viewModel;

    /// <summary>
    /// Static property để truyền trực tiếp FoodItem object khi navigate.
    /// Đặt giá trị TRƯỚC khi GoToAsync, page sẽ tự đọc trong OnNavigatedTo.
    /// Cách này đảm bảo 100% data được truyền đúng, không phụ thuộc URL parsing.
    /// </summary>
    public static FoodItem? PendingEditItem { get; set; }

    public AddMenuItemPage()
    {
        InitializeComponent();
        _viewModel = new AddMenuItemViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Nếu có item đang chờ chỉnh sửa → load dữ liệu vào form
        if (PendingEditItem != null)
        {
            _viewModel.LoadMenuItem(PendingEditItem);
            PendingEditItem = null; // Xóa sau khi đã load, tránh load lại khi quay lại trang
        }
    }

    // === On-blur Validation Handlers ===

    private void OnNameUnfocused(object sender, FocusEventArgs e)
    {
        _viewModel.ValidateName();
    }

    private void OnPriceUnfocused(object sender, FocusEventArgs e)
    {
        _viewModel.ValidatePrice();
    }

    // === Action Handlers ===

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        await _viewModel.SaveMenuItemAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
