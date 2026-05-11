// client-24520222
// server-DuongDangChinh
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppManagermentRestaurant.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Controls; // Thêm thư viện này để dùng Pop-up

namespace AppManagermentRestaurant.ViewModels
{
    public partial class RegisterStaffViewModel : ObservableObject
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();

        [ObservableProperty] private string fullName;
        [ObservableProperty] private string email;
        [ObservableProperty] private string password;

        // Mặc định chọn quyền Nhân viên (Index = 0)
        [ObservableProperty] private int selectedRoleIndex = 0;

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private string statusMessage;
        [ObservableProperty] private bool isError;

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                IsError = true;
                StatusMessage = "Vui lòng nhập đầy đủ thông tin!";
                return;
            }

            IsLoading = true;
            StatusMessage = "Đang tạo tài khoản...";
            IsError = false;

            // Xác định quyền dựa vào Picker (0 = Nhân viên, 1 = Quản lý)
            string quyenFirebase = SelectedRoleIndex == 1 ? "QuanLy" : "NhanVien";

            // Gọi API
            var result = await _firebaseService.RegisterNewUserAsync(Email, Password, FullName, quyenFirebase);

            IsLoading = false;

            if (result.IsSuccess)
            {
                // 1. Nếu thành công: Hiển thị Pop-up rõ ràng
                StatusMessage = string.Empty;
                await Application.Current.MainPage.DisplayAlert("Thành công", $"Đã tạo tài khoản cho {FullName} ({quyenFirebase})!", "OK");

                // Xóa trắng Form để sẵn sàng tạo người tiếp theo
                FullName = string.Empty;
                Email = string.Empty;
                Password = string.Empty;
                SelectedRoleIndex = 0;
            }
            else
            {
                // 2. Nếu lỗi (Bao gồm cả lỗi Email đã tồn tại)
                IsError = true;
                StatusMessage = result.ErrorMessage; // Hiện chữ đỏ bên dưới nút

                // Đồng thời hiện Pop-up cảnh báo
                await Application.Current.MainPage.DisplayAlert("Lỗi đăng ký", result.ErrorMessage, "Đóng");
            }
        }
    }
}