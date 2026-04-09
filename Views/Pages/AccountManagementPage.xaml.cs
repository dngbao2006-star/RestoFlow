using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Helpers;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

public partial class AccountManagementPage : ContentPage
{
    private Button? _selectedTabButton;
    private string _currentSelectedTab = "Profile";

    public AccountManagementPage()
    {
        InitializeComponent();
        var viewModel = new AccountViewModel(AppContext.Instance);
        BindingContext = viewModel;
        InitializeActivityLog();
    }

    private void InitializeActivityLog()
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            viewModel.LoadActivityLog();
        }
    }

    private void OnTabProfile(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Profile");
    }

    private void OnTabPassword(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Password");
    }

    private void OnTabVerification(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Verification");
    }

    private void OnTabActivity(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Activity");

        // Reload activity log when the Activity tab is selected
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            viewModel.LoadActivityLog();
        }
    }

    private void SelectTab(Button? button, string tabName)
    {
        if (button == null)
            return;

        // Deselect previous button
        if (_selectedTabButton != null)
        {
            _selectedTabButton.Opacity = 0.6;
        }

        // Select new button
        _selectedTabButton = button;
        button.Opacity = 1.0;

        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            viewModel.SelectedTab = tabName;
            _currentSelectedTab = tabName;
        }
    }

    private void OnPasswordFieldChanged(object sender, TextChangedEventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            var currentPass = CurrentPasswordEntry?.Text ?? "";
            var newPass = NewPasswordEntry?.Text ?? "";
            var confirmPass = ConfirmPasswordEntry?.Text ?? "";
            viewModel.ValidatePasswordFields(currentPass, newPass, confirmPass);
        }
    }

    private void OnToggleCurrentPassword(object sender, EventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            viewModel.IsCurrentPasswordHidden = !viewModel.IsCurrentPasswordHidden;
        }
    }

    private void OnToggleNewPassword(object sender, EventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            viewModel.IsNewPasswordHidden = !viewModel.IsNewPasswordHidden;
        }
    }

    private void OnToggleConfirmPassword(object sender, EventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            viewModel.IsConfirmPasswordHidden = !viewModel.IsConfirmPasswordHidden;
        }
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            var currentPass = CurrentPasswordEntry?.Text ?? "";
            var newPass = NewPasswordEntry?.Text ?? "";
            var confirmPass = ConfirmPasswordEntry?.Text ?? "";

            var result = viewModel.UpdatePassword(currentPass, newPass, confirmPass);

            if (result)
            {
                // Clear fields
                if (CurrentPasswordEntry != null) CurrentPasswordEntry.Text = "";
                if (NewPasswordEntry != null) NewPasswordEntry.Text = "";
                if (ConfirmPasswordEntry != null) ConfirmPasswordEntry.Text = "";

                // Show success for 3 seconds
                await Task.Delay(3000);
                viewModel.ClearPasswordSuccess();
            }
        }
    }

    private async void OnSendVerificationClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            await DisplayAlert("Gửi mã xác minh", $"Mã xác minh đã được gửi đến {AppContext.Instance.CurrentUser?.Email}", "OK");
            viewModel.SendVerificationCode();
        }
    }

    private void OnVerificationCodeChanged(object sender, TextChangedEventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null && e.NewTextValue != null)
        {
            viewModel.VerificationCode = e.NewTextValue;
            viewModel.ValidateVerificationCode();
        }
    }

    private async void OnVerifyEmailClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as AccountViewModel;
        if (viewModel != null)
        {
            if (viewModel.VerifyEmail())
            {
                if (VerificationCodeEntry != null) VerificationCodeEntry.Text = "";
                await Task.Delay(2000);
                viewModel.ResetVerification();
            }
            else
            {
                await DisplayAlert("Lỗi", "Mã xác minh không đúng. Vui lòng thử lại.", "OK");
            }
        }
    }
}

public class AccountViewModel : ObservableObject
{
    private readonly AppContext _appContext;
    private string _selectedTab = "Profile";
    private string _passwordError = "";
    private bool _hasPasswordError;
    private bool _hasPasswordSuccess;
    private string _verificationError = "";
    private bool _hasVerificationError;
    private string _verificationCode = "";
    private int _verificationStage = 1;
    private bool _isCurrentPasswordHidden = true;
    private bool _isNewPasswordHidden = true;
    private bool _isConfirmPasswordHidden = true;

    public ObservableCollection<ActivityLogEntry> ActivityLog { get; } = new();

    public AccountViewModel(AppContext appContext)
    {
        _appContext = appContext;
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                // Notify all tab visibility properties when tab changes
                OnPropertyChanged(nameof(IsProfileTab));
                OnPropertyChanged(nameof(IsPasswordTab));
                OnPropertyChanged(nameof(IsVerificationTab));
                OnPropertyChanged(nameof(IsActivityTab));
                OnPropertyChanged(nameof(CanUpdatePassword));
            }
        }
    }

    public bool IsProfileTab => SelectedTab == "Profile";
    public bool IsPasswordTab => SelectedTab == "Password";
    public bool IsVerificationTab => SelectedTab == "Verification";
    public bool IsActivityTab => SelectedTab == "Activity";

    public string CurrentUserName => _appContext.CurrentUser?.Name ?? "Unknown";
    public string CurrentUserEmail => _appContext.CurrentUser?.Email ?? "";
    public string CurrentUserPhone => _appContext.CurrentUser?.Phone ?? "Not provided";
    public string CurrentUserRole => _appContext.CurrentUser?.Role.ToString() ?? "Staff";
    public string CurrentUserRoleLabel => _appContext.CurrentUser?.Role == StaffRole.Manager ? "Quản lý" : "Nhân viên";
    public string CurrentUserInitials => _appContext.CurrentUser?.Name?.Split(' ').Select(w => w[0]).Aggregate("", (a, b) => a + b) ?? "?";

    public string PasswordError
    {
        get => _passwordError;
        set => SetProperty(ref _passwordError, value);
    }

    public bool HasPasswordError
    {
        get => _hasPasswordError;
        set => SetProperty(ref _hasPasswordError, value);
    }

    public bool HasPasswordSuccess
    {
        get => _hasPasswordSuccess;
        set => SetProperty(ref _hasPasswordSuccess, value);
    }

    public string VerificationError
    {
        get => _verificationError;
        set => SetProperty(ref _verificationError, value);
    }

    public bool HasVerificationError
    {
        get => _hasVerificationError;
        set => SetProperty(ref _hasVerificationError, value);
    }

    public string VerificationCode
    {
        get => _verificationCode;
        set => SetProperty(ref _verificationCode, value);
    }

    public bool IsVerificationStage1 => _verificationStage == 1;
    public bool IsVerificationStage2 => _verificationStage == 2;
    public bool IsVerificationStage3 => _verificationStage == 3;

    public bool IsCurrentPasswordHidden
    {
        get => _isCurrentPasswordHidden;
        set => SetProperty(ref _isCurrentPasswordHidden, value);
    }

    public bool IsNewPasswordHidden
    {
        get => _isNewPasswordHidden;
        set => SetProperty(ref _isNewPasswordHidden, value);
    }

    public bool IsConfirmPasswordHidden
    {
        get => _isConfirmPasswordHidden;
        set => SetProperty(ref _isConfirmPasswordHidden, value);
    }

    public string CurrentPasswordShowIcon => IsCurrentPasswordHidden ? "👁" : "👁‍🗨";
    public string NewPasswordShowIcon => IsNewPasswordHidden ? "👁" : "👁‍🗨";
    public string ConfirmPasswordShowIcon => IsConfirmPasswordHidden ? "👁" : "👁‍🗨";

    public bool CanUpdatePassword => !string.IsNullOrEmpty(PasswordError) == false && 
                                     !HasPasswordError;

    public bool CanVerifyEmail => VerificationCode.Length == 6;

    public void ValidatePasswordFields(string current, string newPass, string confirm)
    {
        HasPasswordError = false;
        PasswordError = "";

        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirm))
        {
            return;
        }

        if (newPass.Length < 6)
        {
            HasPasswordError = true;
            PasswordError = "Mật khẩu mới phải ít nhất 6 ký tự";
            return;
        }

        if (newPass != confirm)
        {
            HasPasswordError = true;
            PasswordError = "Mật khẩu xác nhận không khớp";
            return;
        }

        OnPropertyChanged(nameof(CanUpdatePassword));
    }

    public bool UpdatePassword(string current, string newPass, string confirm)
    {
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirm))
        {
            HasPasswordError = true;
            PasswordError = "Vui lòng điền tất cả các trường";
            return false;
        }

        if (newPass.Length < 6)
        {
            HasPasswordError = true;
            PasswordError = "Mật khẩu mới phải ít nhất 6 ký tự";
            return false;
        }

        if (newPass != confirm)
        {
            HasPasswordError = true;
            PasswordError = "Mật khẩu xác nhận không khớp";
            return false;
        }

        // Simulate password update
        HasPasswordSuccess = true;
        HasPasswordError = false;
        PasswordError = "";

        // Log password change activity
        ActivityLogService.Instance.LogPasswordChange();

        return true;
    }

    public void ClearPasswordSuccess()
    {
        HasPasswordSuccess = false;
    }

    public void SendVerificationCode()
    {
        _verificationStage = 2;
        OnPropertyChanged(nameof(IsVerificationStage1));
        OnPropertyChanged(nameof(IsVerificationStage2));
        OnPropertyChanged(nameof(IsVerificationStage3));
    }

    public void ValidateVerificationCode()
    {
        if (VerificationCode.Length == 6 && VerificationCode.All(char.IsDigit))
        {
            HasVerificationError = false;
            VerificationError = "";
        }
        OnPropertyChanged(nameof(CanVerifyEmail));
    }

    public bool VerifyEmail()
    {
        // Simulate verification (in real app, validate against server)
        if (VerificationCode == "123456") // Demo code
        {
            _verificationStage = 3;
            OnPropertyChanged(nameof(IsVerificationStage1));
            OnPropertyChanged(nameof(IsVerificationStage2));
            OnPropertyChanged(nameof(IsVerificationStage3));

            // Log email verification activity
            ActivityLogService.Instance.LogEmailVerification();

            return true;
        }

        HasVerificationError = true;
        VerificationError = "Mã xác minh không đúng";
        return false;
    }

    public void ResetVerification()
    {
        _verificationStage = 1;
        VerificationCode = "";
        HasVerificationError = false;
        VerificationError = "";
        OnPropertyChanged(nameof(IsVerificationStage1));
        OnPropertyChanged(nameof(IsVerificationStage2));
        OnPropertyChanged(nameof(IsVerificationStage3));
    }

    public void LoadActivityLog()
    {
        ActivityLog.Clear();

        // Load activities from the ActivityLogService
        var activityLogService = ActivityLogService.Instance;
        foreach (var activity in activityLogService.ActivityLog)
        {
            ActivityLog.Add(activity);
        }
    }
}
