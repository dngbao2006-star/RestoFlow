using AppManagermentRestaurant.Constants;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;
using CommunityToolkit.Maui.Views;

namespace AppManagermentRestaurant.Controls.Popups;

/// <summary>
/// Popup menu tài khoản — kế thừa toolkit:Popup.
/// Bind HeaderViewModel để hiển thị tên và email của user hiện tại.
/// </summary>
public partial class AccountPopup : Popup
{
    public AccountPopup()
    {
        InitializeComponent();
        // Bind HeaderViewModel để lấy CurrentUserName, CurrentUserEmail
        BindingContext = new HeaderViewModel(AppContext.Instance);
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        Close();
        var route = AppContext.Instance.IsManager ? AppRoutes.SystemConfig : AppRoutes.Account;
        await Shell.Current.GoToAsync(AppRoutes.Absolute(route)); // => //tai-khoan or //cau-hinh
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Close();
        // TODO [BACKEND]: Xoá token lưu trong SecureStorage khi kết nối backend
        AppContext.Instance.CurrentUser = null;
        await MainThread.InvokeOnMainThreadAsync(App.ShowLogin);
    }
}
