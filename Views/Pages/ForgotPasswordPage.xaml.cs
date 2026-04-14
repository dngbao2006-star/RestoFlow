using System.Text.RegularExpressions;

namespace AppManagermentRestaurant.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
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

        // Kiểm tra định dạng email
        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
        {
            await DisplayAlert("Lỗi", "Định dạng email không hợp lệ.", "Đóng");
            return;
        }

        // TODO: [BACKEND] - Gọi API kiểm tra email có tồn tại trong Database hay không
        /*
        bool isEmailExist = await _authService.CheckEmailExistsAsync(email);
        if (!isEmailExist)
        {
            await DisplayAlert("Lỗi", "Email này chưa từng được đăng ký trong hệ thống.", "Đóng");
            return;
        }
        */

        await DisplayAlert("Thành công", "Link khôi phục mật khẩu đã được gửi đến email của bạn.", "OK");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}