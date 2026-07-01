// client-24520222
// server-DuongDangChinh
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Views.Pages;
using System.Threading.Tasks;
using System;

namespace AppManagermentRestaurant.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public void ClearMessages() => ErrorMessage = string.Empty;

        [RelayCommand]
        private void SelectStaffRole()
        {
            Email = "24520222@gm.uit.edu.vn";
            ClearMessages();
        }

        [RelayCommand]
        private void SelectManagerRole()
        {
            Email = "24520214@gm.uit.edu.vn";
            ClearMessages();
        }

        [RelayCommand]
        private async Task SignInAsync()
        {
            if (IsLoading) return;

            ClearMessages();

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập Email và Mật khẩu.";
                return;
            }

            Email = Email.Trim();
            IsLoading = true;
            try
            {
                var result = await _firebaseService.LoginAndGetProfileAsync(Email, Password);

                if (result.Token == null || result.Uid == null)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                if (!result.IsEmailVerified)
                {
                    var page = Application.Current?.MainPage;
                    if (page == null) return;

                    bool wantResend = await page.DisplayAlert(
                        "Chưa xác minh Email",
                        "Tài khoản chưa được xác minh. Hãy kiểm tra hộp thư và thư mục Spam.\n\nBạn có muốn gửi lại email xác minh không?",
                        "Gửi lại", "Đóng");

                    if (wantResend)
                    {
                        bool sent = await _firebaseService.ResendVerificationEmailAsync(result.Token);
                        await page.DisplayAlert(
                            sent ? "Thành công" : "Lỗi",
                            sent ? "Đã gửi lại email xác minh." : "Không thể gửi email lúc này. Vui lòng thử lại sau.",
                            "OK");
                    }
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.HoTen) || string.IsNullOrWhiteSpace(result.Quyen))
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Hồ sơ người dùng chưa có đầy đủ tên hoặc phân quyền."
                        : result.ErrorMessage;
                    return;
                }

                if (result.TrangThai is "TamKhoa" or "Khóa")
                {
                    ErrorMessage = "Tài khoản của bạn đã bị khóa.";
                    return;
                }

                StaffRole userRole = result.Quyen switch
                {
                    "QuanLy" => StaffRole.Manager,
                    "NhanVien" => StaffRole.Staff,
                    _ => throw new InvalidOperationException("Phân quyền tài khoản không hợp lệ.")
                };

                if (result.TrangThai != "HoatDong")
                {
                    ErrorMessage = "Tài khoản hiện không ở trạng thái hoạt động.";
                    return;
                }

                AppContext.Instance.CurrentUser = new Staff
                {
                    FirebaseUid = result.Uid,
                    Name = result.HoTen,
                    Email = Email,
                    Phone = result.Phone,
                    JoinDate = result.JoinDate ?? DateTime.MinValue,
                    Role = userRole,
                    Status = StaffStatus.Active,
                    LastLogin = DateTime.Now
                };

                var sessionId = Guid.NewGuid().ToString();
                AppContext.Instance.CurrentSessionId = sessionId;

                try
                {
                    await _firebaseService.SetUserOnlineAsync(result.Uid, result.HoTen, sessionId);
                    AppContext.Instance.SessionConflictSubscription?.Dispose();
                    AppContext.Instance.SessionConflictSubscription = _firebaseService.ListenForSessionConflict(
                        result.Uid,
                        sessionId,
                        () => _ = AppContext.Instance.ForceLogoutAsync());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] Presence unavailable: {ex.Message}");
                }

                ActivityLogService.Instance.LogLogin(Email);
                await AppContext.Instance.InitializeOnceAsync();
                App.ShowAppShell();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch
            {
                ErrorMessage = "Không thể hoàn tất đăng nhập. Vui lòng thử lại.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            await App.Current.MainPage.Navigation.PushAsync(new ForgotPasswordPage());
        }
    }
}
