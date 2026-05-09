// client-24520222
// server-DuongDangChinh
using System.Text.RegularExpressions;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();

    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    private async void OnSendResetClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim();

        if (string.IsNullOrEmpty(email))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập địa chỉ email.", "Đóng");
            return;
        }

        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
        {
            await DisplayAlert("Lỗi", "Định dạng email không hợp lệ.", "Đóng");
            return;
        }

        // Gọi API Firebase gửi mail
        bool isSuccess = await _firebaseService.SendPasswordResetEmailAsync(email);

        if (isSuccess)
        {
            await DisplayAlert("Thành công", "Link khôi phục mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (và mục Spam).", "OK");
            await Navigation.PopAsync(); // Quay lại trang đăng nhập
        }
        else
        {
            await DisplayAlert("Lỗi", "Email này chưa được đăng ký hoặc có lỗi kết nối.", "Đóng");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}