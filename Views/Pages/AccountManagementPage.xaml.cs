// client-24520222
// server-DuongDangChinh
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class AccountManagementPage : ContentPage
{
    private Button? _selectedTabButton;

    public AccountManagementPage()
    {
        InitializeComponent();
        BindingContext = new AccountViewModel(AppContext.Instance);
        SelectTab(ProfileTabBtn, "Profile");
        ViewModel.LoadActivityLog();
    }

    private AccountViewModel ViewModel => (AccountViewModel)BindingContext;

    private void OnTabProfile(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Profile");
    }

    private void OnTabPassword(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Password");
    }

    private void OnTabActivity(object sender, EventArgs e)
    {
        SelectTab(sender as Button, "Activity");
        ViewModel.LoadActivityLog();
    }

    private void SelectTab(Button? button, string tabName)
    {
        if (button == null)
        {
            return;
        }

        if (_selectedTabButton != null)
        {
            _selectedTabButton.Opacity = 0.6;
        }

        _selectedTabButton = button;
        _selectedTabButton.Opacity = 1.0;
        ViewModel.SelectedTab = tabName;
    }

    private void OnPasswordFieldChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.ValidatePasswordFields(
            CurrentPasswordEntry?.Text ?? string.Empty,
            NewPasswordEntry?.Text ?? string.Empty,
            ConfirmPasswordEntry?.Text ?? string.Empty);
    }

    private void OnToggleCurrentPassword(object sender, EventArgs e)
    {
        ViewModel.IsCurrentPasswordHidden = !ViewModel.IsCurrentPasswordHidden;
    }

    private void OnToggleNewPassword(object sender, EventArgs e)
    {
        ViewModel.IsNewPasswordHidden = !ViewModel.IsNewPasswordHidden;
    }

    private void OnToggleConfirmPassword(object sender, EventArgs e)
    {
        ViewModel.IsConfirmPasswordHidden = !ViewModel.IsConfirmPasswordHidden;
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        var updated = ViewModel.UpdatePassword(
            CurrentPasswordEntry?.Text ?? string.Empty,
            NewPasswordEntry?.Text ?? string.Empty,
            ConfirmPasswordEntry?.Text ?? string.Empty);

        if (!updated)
        {
            return;
        }

        if (CurrentPasswordEntry != null) CurrentPasswordEntry.Text = string.Empty;
        if (NewPasswordEntry != null) NewPasswordEntry.Text = string.Empty;
        if (ConfirmPasswordEntry != null) ConfirmPasswordEntry.Text = string.Empty;

        await Task.Delay(3000);
        ViewModel.ClearPasswordSuccess();
    }
}