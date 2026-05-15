using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.ViewModels;

public class AccountViewModel : ObservableObject
{
    private readonly AppContext _appContext;
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private string _selectedTab = "Profile";
    private string _passwordError = string.Empty;
    private bool _hasPasswordError;
    private bool _hasPasswordSuccess;
    private string _verificationError = string.Empty;
    private bool _hasVerificationError;
    private string _verificationCode = string.Empty;
    private int _verificationStage = 1;
    private bool _isCurrentPasswordHidden = true;
    private bool _isNewPasswordHidden = true;
    private bool _isConfirmPasswordHidden = true;
    private bool _isUpdatingPassword;

    public AccountViewModel(AppContext appContext)
    {
        _appContext = appContext;
    }

    public ObservableCollection<ActivityLogEntry> ActivityLog { get; } = new();

    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (!SetProperty(ref _selectedTab, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsProfileTab));
            OnPropertyChanged(nameof(IsPasswordTab));
            OnPropertyChanged(nameof(IsVerificationTab));
            OnPropertyChanged(nameof(IsActivityTab));
        }
    }

    public bool IsProfileTab => SelectedTab == "Profile";
    public bool IsPasswordTab => SelectedTab == "Password";
    public bool IsVerificationTab => SelectedTab == "Verification";
    public bool IsActivityTab => SelectedTab == "Activity";

    public string CurrentUserName => _appContext.CurrentUser?.Name ?? "Không xác định";
    public string CurrentUserEmail => _appContext.CurrentUser?.Email ?? string.Empty;
    public string CurrentUserPhone => _appContext.CurrentUser?.Phone ?? "Chưa cập nhật";
    public string CurrentUserRole => _appContext.CurrentUser?.Role.ToString() ?? "Nhân viên";
    public string CurrentUserRoleLabel => _appContext.CurrentUser?.Role == StaffRole.Manager ? "Quản lý" : "Nhân viên";

    public string CurrentUserInitials
    {
        get
        {
            var name = _appContext.CurrentUser?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            return string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0])).ToUpperInvariant();
        }
    }

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

    /// <summary>
    /// Trạng thái loading khi đang gọi API đổi mật khẩu.
    /// </summary>
    public bool IsUpdatingPassword
    {
        get => _isUpdatingPassword;
        set => SetProperty(ref _isUpdatingPassword, value);
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
        set
        {
            if (!SetProperty(ref _verificationCode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanVerifyEmail));
        }
    }

    public bool IsVerificationStage1 => _verificationStage == 1;
    public bool IsVerificationStage2 => _verificationStage == 2;
    public bool IsVerificationStage3 => _verificationStage == 3;

    public bool IsCurrentPasswordHidden
    {
        get => _isCurrentPasswordHidden;
        set
        {
            if (!SetProperty(ref _isCurrentPasswordHidden, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentPasswordShowText));
        }
    }

    public bool IsNewPasswordHidden
    {
        get => _isNewPasswordHidden;
        set
        {
            if (!SetProperty(ref _isNewPasswordHidden, value))
            {
                return;
            }

            OnPropertyChanged(nameof(NewPasswordShowText));
        }
    }

    public bool IsConfirmPasswordHidden
    {
        get => _isConfirmPasswordHidden;
        set
        {
            if (!SetProperty(ref _isConfirmPasswordHidden, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ConfirmPasswordShowText));
        }
    }

    public string CurrentPasswordShowText => IsCurrentPasswordHidden ? "👁" : "🙈";
    public string NewPasswordShowText => IsNewPasswordHidden ? "👁" : "🙈";
    public string ConfirmPasswordShowText => IsConfirmPasswordHidden ? "👁" : "🙈";

    public bool CanUpdatePassword => !HasPasswordError && !IsUpdatingPassword;
    public bool CanVerifyEmail => VerificationCode.Length == 6;

    public void ValidatePasswordFields(string current, string newPass, string confirm)
    {
        HasPasswordError = false;
        PasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirm))
        {
            return;
        }

        if (newPass.Length < 6)
        {
            HasPasswordError = true;
            PasswordError = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return;
        }

        if (newPass != confirm)
        {
            HasPasswordError = true;
            PasswordError = "Mật khẩu xác nhận không khớp.";
        }

        OnPropertyChanged(nameof(CanUpdatePassword));
    }

    /// <summary>
    /// Đổi mật khẩu thông qua Firebase Auth REST API.
    /// Bước 1: Validate input local.
    /// Bước 2: Gọi ChangePasswordAsync → xác thực mật khẩu cũ + đổi mật khẩu mới.
    /// </summary>
    public async Task<bool> UpdatePasswordAsync(string current, string newPass, string confirm)
    {
        ValidatePasswordFields(current, newPass, confirm);

        if (HasPasswordError)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirm))
        {
            HasPasswordError = true;
            PasswordError = "Vui lòng nhập đầy đủ thông tin.";
            return false;
        }

        // Lấy email của user hiện tại
        var email = _appContext.CurrentUser?.Email;
        if (string.IsNullOrEmpty(email))
        {
            HasPasswordError = true;
            PasswordError = "Không tìm thấy thông tin tài khoản.";
            return false;
        }

        // Gọi Firebase Auth API
        IsUpdatingPassword = true;
        OnPropertyChanged(nameof(CanUpdatePassword));

        try
        {
            var result = await _firebaseService.ChangePasswordAsync(email, current, newPass);

            if (!result.IsSuccess)
            {
                HasPasswordError = true;
                PasswordError = result.ErrorMessage;
                return false;
            }

            // Thành công
            HasPasswordSuccess = true;
            HasPasswordError = false;
            PasswordError = string.Empty;
            ActivityLogService.Instance.LogPasswordChange();
            return true;
        }
        finally
        {
            IsUpdatingPassword = false;
            OnPropertyChanged(nameof(CanUpdatePassword));
        }
    }

    public void ClearPasswordSuccess()
    {
        HasPasswordSuccess = false;
    }

    public void SendVerificationCode()
    {
        // TODO: [BACKEND] - Chỗ này gọi API gửi OTP/mã xác minh về email người dùng.
        _verificationStage = 2;
        RaiseVerificationStageChanged();
    }

    public void ValidateVerificationCode()
    {
        if (VerificationCode.Length == 6 && VerificationCode.All(char.IsDigit))
        {
            HasVerificationError = false;
            VerificationError = string.Empty;
            return;
        }

        HasVerificationError = true;
        VerificationError = "Mã xác minh phải gồm 6 chữ số.";
    }

    public bool VerifyEmail()
    {
        ValidateVerificationCode();

        if (HasVerificationError)
        {
            return false;
        }

        // TODO: [BACKEND] - Chỗ này gọi API xác minh email bằng mã OTP.
        if (VerificationCode != "123456")
        {
            HasVerificationError = true;
            VerificationError = "Mã xác minh không đúng.";
            return false;
        }

        _verificationStage = 3;
        HasVerificationError = false;
        VerificationError = string.Empty;
        RaiseVerificationStageChanged();

        ActivityLogService.Instance.LogEmailVerification();
        return true;
    }

    public void ResetVerification()
    {
        _verificationStage = 1;
        VerificationCode = string.Empty;
        HasVerificationError = false;
        VerificationError = string.Empty;
        RaiseVerificationStageChanged();
    }

    public void LoadActivityLog()
    {
        ActivityLog.Clear();

        foreach (var activity in ActivityLogService.Instance.ActivityLog)
        {
            ActivityLog.Add(activity);
        }
    }

    private void RaiseVerificationStageChanged()
    {
        OnPropertyChanged(nameof(IsVerificationStage1));
        OnPropertyChanged(nameof(IsVerificationStage2));
        OnPropertyChanged(nameof(IsVerificationStage3));
    }
}
