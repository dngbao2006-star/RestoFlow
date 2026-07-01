using AppManagermentRestaurant.Constants;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;
using AppManagermentRestaurant.Controls;
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
        BindingContext = AppHeaderView.SharedViewModel;
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        Close();
        try
        {
            await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.Account));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AccountPopup] Navigation failed: {ex}");
        }
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
            try
            {
                await _firebaseService.SetUserOfflineAsync(
                    currentUser.FirebaseUid,
                    currentUser.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logout] Could not update presence: {ex.Message}");
            }
        }

        ActivityLogService.Instance.LogLogout();

        if (Application.Current?.MainPage is AppShell shell)
            shell.BindingContext = null;

        // Quay về màn hình login
        await MainThread.InvokeOnMainThreadAsync(App.ShowLogin);
        AppContext.Instance.CurrentUser = null;
    }
}
