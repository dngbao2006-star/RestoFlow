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
    private readonly FirebaseService _firebaseService
    = new FirebaseService();
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

        var currentUser = AppContext.Instance.CurrentUser;

        // Hủy listener xung đột phiên trước khi logout
        AppContext.Instance.SessionConflictSubscription?.Dispose();
        AppContext.Instance.SessionConflictSubscription = null;
        AppContext.Instance.CurrentSessionId = null;

        // Cập nhật OFFLINE lên Firebase
        if (currentUser != null)
        {
            await _firebaseService.SetUserOfflineAsync(
                currentUser.FirebaseUid,
                currentUser.Name);
        }

        // Xóa user hiện tại khỏi app
        AppContext.Instance.CurrentUser = null;

        // Quay về màn hình login
        await MainThread.InvokeOnMainThreadAsync(App.ShowLogin);
    }
}
