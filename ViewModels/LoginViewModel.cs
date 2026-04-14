using AppManagermentRestaurant.Helpers;
// using AppManagermentRestaurant.Models; // Tạm tắt khi chưa dùng backend
// using AppManagermentRestaurant.Services; // Tạm tắt khi chưa dùng backend

namespace AppManagermentRestaurant.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ClearMessages();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ClearMessages();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public void ClearMessages()
    {
        ErrorMessage = string.Empty;
    }

    public async Task<bool> ExecuteLoginAsync()
    {
        ClearMessages();
        IsLoading = true;

        try
        {
            // Giả lập thời gian chờ để test UI (Vòng xoay Loading)
            await Task.Delay(1000);

            // 1. Kiểm tra UI cơ bản (Frontend cần làm)
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return false;
            }

            // ==========================================
            // TODO: [BACKEND] - LOGIC KIỂM TRA DATABASE 
            // ==========================================
            /*
            var user = AppContext.Instance.StaffMembers.FirstOrDefault(s =>
                string.Equals(s.Email, Email?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                s.Password == Password);

            if (user == null)
            {
                ErrorMessage = "Sai thông tin đăng nhập hoặc tài khoản không tồn tại.";
                return false;
            }

            if (user.Status == StaffStatus.Locked)
            {
                ErrorMessage = "Tài khoản đang bị khóa. Liên hệ quản trị viên.";
                return false;
            }

            AppContext.Instance.CurrentUser = user;
            ActivityLogService.Instance.LogLogin(user.Email);
            */
            // ==========================================

            // 2. Logic tạm cho Frontend: Cứ nhập đúng 1 trong 2 email demo thì cho qua trang chính
            if (Email == "tuan.staff@goldenplate.vn" || Email == "quan.manager@goldenplate.vn")
            {
                return true; // Cho phép chuyển trang
            }
            else
            {
                ErrorMessage = "Sai email. Vui lòng dùng tài khoản demo để test giao diện.";
                return false; // Chặn lại ở màn hình Login
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}