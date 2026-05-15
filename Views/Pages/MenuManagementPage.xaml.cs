using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class MenuManagementPage : ContentPage
{
    private FirebaseService _firebaseService = new();

    public MenuManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
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
                // Vô hiệu hóa button trong khi xử lý
                button.IsEnabled = false;

                // Animation khi nhấn button
                await button.ScaleTo(0.95, 100);
                await button.ScaleTo(1, 100);

                // Toggle trạng thái
                item.OutOfStock = !item.OutOfStock;

                // Lưu lên Firebase
                await _firebaseService.SaveMenuItemAsync(item);

                // Hiệu ứng thành công
                button.BackgroundColor = item.OutOfStock ? Color.FromArgb("#F44336") : Color.FromArgb("#4CAF50");
                await button.ScaleTo(1.05, 100);
                await button.ScaleTo(1, 100);

                // Reset màu
                await Task.Delay(500);
                button.BackgroundColor = null;

                // Thông báo
                string status = item.OutOfStock ? "Hết hàng" : "Còn hàng";
                await DisplayAlert("Thành công", $"Cập nhật thành công: {status}", "OK");

                // Force refresh UI để hiển thị text button mới
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    button.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi cập nhật trạng thái: {ex.Message}", "OK");
                item.OutOfStock = !item.OutOfStock; // Revert
                button.IsEnabled = true;
            }
        }
    }
}

