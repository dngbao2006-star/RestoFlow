// client-24520222
// server-DuongDangChinh
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class LoginPage : ContentPage
{
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
}