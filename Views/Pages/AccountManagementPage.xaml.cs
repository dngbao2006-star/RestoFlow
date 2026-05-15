// client-24520222
// server-DuongDangChinh
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class AccountManagementPage : ContentPage
{
    private Button? _selectedTabButton;
    private readonly FirebaseService _firebaseService = new FirebaseService();

    public AccountManagementPage()
    {
        InitializeComponent();
        BindingContext = new AccountViewModel(AppContext.Instance);
        SelectTab(ProfileTabBtn, "Profile");
        ViewModel.LoadActivityLog();
    }

    private AccountViewModel ViewModel => (AccountViewModel)BindingContext;

    private void OnTabProfile(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Profile");
    }

    private void OnTabPassword(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Password");
    }

    private void OnTabActivity(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Activity");
        ViewModel.LoadActivityLog();
    }

    private void SelectTab(Button? button, string tabName)
    {
        if (button == null)
        {
            return;
        }

        if (_selectedTabButton != null)
        {
            _selectedTabButton.Opacity = 0.6;
        }

        _selectedTabButton = button;
        _selectedTabButton.Opacity = 1.0;
        ViewModel.SelectedTab = tabName;
    }

    private void OnPasswordFieldChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.ValidatePasswordFields(
            CurrentPasswordEntry?.Text ?? string.Empty,
            NewPasswordEntry?.Text ?? string.Empty,
            ConfirmPasswordEntry?.Text ?? string.Empty);
    }

    private void OnToggleCurrentPassword(object sender, EventArgs e)
    {
        ViewModel.IsCurrentPasswordHidden = !ViewModel.IsCurrentPasswordHidden;
    }

    private void OnToggleNewPassword(object sender, EventArgs e)
    {
        ViewModel.IsNewPasswordHidden = !ViewModel.IsNewPasswordHidden;
    }

    private void OnToggleConfirmPassword(object sender, EventArgs e)
    {
        ViewModel.IsConfirmPasswordHidden = !ViewModel.IsConfirmPasswordHidden;
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        var updated = await ViewModel.UpdatePasswordAsync(
            CurrentPasswordEntry?.Text ?? string.Empty,
            NewPasswordEntry?.Text ?? string.Empty,
            ConfirmPasswordEntry?.Text ?? string.Empty);

        if (!updated)
        {
            return;
        }

        // Hiển thị thông báo thành công
        await DisplayAlert(
            "Đổi mật khẩu thành công",
            "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại với mật khẩu mới.",
            "OK");

        // === LOGOUT AN TOÀN (thứ tự chống crash đã kiểm chứng) ===

        var currentUser = AppContext.Instance.CurrentUser;

        // 1. Hủy listener xung đột phiên
        AppContext.Instance.SessionConflictSubscription?.Dispose();
        AppContext.Instance.SessionConflictSubscription = null;
        AppContext.Instance.CurrentSessionId = null;

        // 2. Cập nhật trạng thái offline lên Firebase
        if (currentUser != null)
        {
            await _firebaseService.SetUserOfflineAsync(
                currentUser.FirebaseUid,
                currentUser.Name);
        }

        // 3. Ngắt BindingContext của AppShell TRƯỚC (tránh NullReferenceException)
        if (Application.Current?.MainPage is AppShell shell)
        {
            shell.BindingContext = null;
        }

        // 4. Chuyển về màn hình đăng nhập TRƯỚC
        App.ShowLogin();

        // 5. Xóa user SAU khi binding đã ngắt
        AppContext.Instance.CurrentUser = null;
    }
}