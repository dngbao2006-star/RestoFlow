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

        LoadingOverlay.IsVisible = true;

        bool isEmailExist = await _firebaseService.CheckEmailExistsAsync(email);

        if (!isEmailExist)
        {
            LoadingOverlay.IsVisible = false;
            await DisplayAlert("Lỗi", "Email này chưa được đăng ký trong hệ thống.", "Đóng");
            return;
        }

        bool isSuccess = await _firebaseService.SendPasswordResetEmailAsync(email);

        LoadingOverlay.IsVisible = false;

        if (isSuccess)
        {
            await DisplayAlert("Thành công", "Link khôi phục mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (và mục Spam).", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Lỗi", "Có lỗi kết nối khi gửi email khôi phục.", "Đóng");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}