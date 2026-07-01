// client-24520222
// server-DuongDangChinh
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class RegisterStaffPage : ContentPage
{
    public RegisterStaffPage()
    {
        InitializeComponent();
        BindingContext = new RegisterStaffViewModel();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        if (sender is Button button)
            button.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
    }
}
