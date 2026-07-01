// client-24520222
// server-DuongDangChinh
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class LoginPage : ContentPage
{
    private static readonly Color IconDefaultColor = Color.FromArgb("#9CA3AF");
    private static readonly Color IconFocusedColor = Color.FromArgb("#1B3A6B");
    private static readonly Color BorderFocusedColor = Color.FromArgb("#1B3A6B");
    private static readonly Color BorderDefaultColor = Color.FromArgb("#DDDDDD");

    public LoginPage()
    {
        InitializeComponent();
        BindingContext = new LoginViewModel();
    }

    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        if (BindingContext is LoginViewModel vm && vm.SignInCommand.CanExecute(null))
            vm.SignInCommand.Execute(null);
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        EyeButton.Source = PasswordEntry.IsPassword ? "eye_open.png" : "eye_closed.png";
    }

    // ── Focus state: đổi icon sang màu xanh chủ đạo ──
    private void OnEmailEntryFocused(object sender, FocusEventArgs e)
    {
        EmailIcon.TextColor = IconFocusedColor;
        EmailBorder.Stroke = new SolidColorBrush(BorderFocusedColor);
    }

    private void OnEmailEntryUnfocused(object sender, FocusEventArgs e)
    {
        EmailIcon.TextColor = IconDefaultColor;
        EmailBorder.Stroke = new SolidColorBrush(BorderDefaultColor);
    }

    private void OnPasswordEntryFocused(object sender, FocusEventArgs e)
    {
        PasswordIcon.TextColor = IconFocusedColor;
        PasswordBorder.Stroke = new SolidColorBrush(BorderFocusedColor);
    }

    private void OnPasswordEntryUnfocused(object sender, FocusEventArgs e)
    {
        PasswordIcon.TextColor = IconDefaultColor;
        PasswordBorder.Stroke = new SolidColorBrush(BorderDefaultColor);
    }
}