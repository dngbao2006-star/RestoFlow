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
        try
        {
            // Không kiểm tra email có tồn tại hay không để tránh làm lộ danh sách tài khoản.
            // Firebase vẫn chỉ gửi thư nếu địa chỉ thực sự đã được đăng ký.
            bool accepted = await _firebaseService.SendPasswordResetEmailAsync(email);
            if (!accepted)
                throw new HttpRequestException("Firebase rejected the password-reset request.");
            await DisplayAlert(
                "Đã tiếp nhận yêu cầu",
                "Nếu email này đã được đăng ký, Firebase sẽ gửi link đặt lại mật khẩu. Vui lòng kiểm tra cả thư mục Spam.",
                "OK");
            await Navigation.PopAsync();
        }
        catch
        {
            await DisplayAlert("Lỗi", "Không thể gửi yêu cầu lúc này. Vui lòng kiểm tra kết nối và thử lại.", "Đóng");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
