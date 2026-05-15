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
            ClearMessages();

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập Email và Mật khẩu.";
                return;
            }

            IsLoading = true;

            // Gọi API lấy dữ liệu Token và Profile từ FirebaseService
            var result = await _firebaseService.LoginAndGetProfileAsync(Email, Password);

            IsLoading = false;

            // Xử lý các luồng lỗi đăng nhập
            if (result.Token == null)
            {
                ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
                return;
            }

            // KIỂM TRA XÁC MINH EMAIL (LUỒNG CHẶN MỚI)
            if (!result.IsEmailVerified)
            {
                bool wantResend = await Application.Current.MainPage.DisplayAlert(
                    "Chưa xác minh Email",
                    "Tài khoản của bạn chưa được xác minh. Vui lòng kiểm tra hộp thư email (và mục Spam) để bấm link kích hoạt.\n\nBạn có muốn gửi lại email xác minh không?",
                    "Gửi lại", "Đóng");

                if (wantResend)
                {
                    IsLoading = true;
                    bool sent = await _firebaseService.ResendVerificationEmailAsync(result.Token);
                    IsLoading = false;

                    if (sent)
                        await Application.Current.MainPage.DisplayAlert("Thành công", "Đã gửi lại link xác minh. Vui lòng kiểm tra email.", "OK");
                    else
                        await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể gửi lại email lúc này. Vui lòng thử lại sau.", "OK");
                }
                return; // Chặn đứng tại đây, không cho chạy xuống dưới
            }

            if (result.HoTen == null)
            {
                ErrorMessage = "Đăng nhập đúng nhưng không tìm thấy dữ liệu phân quyền!";
                return;
            }

            if (result.TrangThai == "TamKhoa" || result.TrangThai == "Khóa")
            {
                ErrorMessage = "Tài khoản của bạn đã bị khóa!";
                return;
            }

            // GÁN DỮ LIỆU FIREBASE VÀO HỆ THỐNG GLOBAL (AppContext)
            StaffRole userRole = result.Quyen == "QuanLy" ? StaffRole.Manager : StaffRole.Staff;
            StaffStatus userStatus = result.TrangThai == "HoatDong" ? StaffStatus.Active : StaffStatus.Inactive;

            AppContext.Instance.CurrentUser = new Staff
            {
                FirebaseUid = result.Uid,
                Name = result.HoTen,
                Email = Email,
                Role = userRole,
                Status = userStatus,
                LastLogin = DateTime.Now
            };

            // SINH SESSION ID MỚI CHO PHIÊN ĐĂNG NHẬP NÀY
            var sessionId = Guid.NewGuid().ToString();
            AppContext.Instance.CurrentSessionId = sessionId;

            // CẬP NHẬT ONLINE + SESSION ID LÊN FIREBASE
            await _firebaseService.SetUserOnlineAsync(
                result.Uid,
                result.HoTen,
                sessionId);

            // BẮT ĐẦU LẮNG NGHE XUNG ĐỘT PHIÊN ĐĂNG NHẬP
            // Nếu thiết bị khác đăng nhập cùng tài khoản → SessionId trên Firebase sẽ thay đổi
            // → listener phát hiện và gọi ForceLogoutAsync để đá thiết bị này ra
            AppContext.Instance.SessionConflictSubscription?.Dispose();
            AppContext.Instance.SessionConflictSubscription = _firebaseService.ListenForSessionConflict(
                result.Uid,
                sessionId,
                () =>
                {
                    // Chạy force logout trên main thread
                    _ = AppContext.Instance.ForceLogoutAsync();
                });

            // Chuyển hướng vào màn hình chính
            App.ShowAppShell();
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            await App.Current.MainPage.Navigation.PushAsync(new ForgotPasswordPage());
        }
    }
}