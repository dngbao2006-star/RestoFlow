using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.ViewModels;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

/// <summary>
/// Login page for restaurant staff and managers.
/// Supports quick access with Staff/Manager buttons on the left sidebar.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel = new();

    public LoginPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    /// <summary>
    /// Auto-fills the email field with staff credentials when "Staff" button is clicked.
    /// </summary>
    private void OnSelectStaffRole(object sender, EventArgs e)
    {
        _viewModel.Email = "tuan.staff@goldenplate.vn";
        _viewModel.ClearMessages();
    }

    /// <summary>
    /// Auto-fills the email field with manager credentials when "Manager" button is clicked.
    /// </summary>
    private void OnSelectManagerRole(object sender, EventArgs e)
    {
        _viewModel.Email = "quan.manager@goldenplate.vn";
        _viewModel.ClearMessages();
    }

    /// <summary>
    /// Handles the sign-in process. Validates email and user status before logging in.
    /// </summary>
    private async void OnSignInClicked(object sender, EventArgs e)
    {
        // Clear any previous errors
        _viewModel.ClearMessages();
        _viewModel.IsLoading = true;

        try
        {
            // Simulate network delay
            await Task.Delay(300);

            // Validate email is not empty
            if (string.IsNullOrWhiteSpace(_viewModel.Email))
            {
                _viewModel.ErrorMessage = "Please enter your email address.";
                return;
            }

            // Find user by email
            var user = AppContext.Instance.StaffMembers.FirstOrDefault(s =>
                string.Equals(s.Email, _viewModel.Email?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                _viewModel.ErrorMessage = "Email not found. Please check and try again.";
                return;
            }

            // Check if account is locked
            if (user.Status == StaffStatus.Locked)
            {
                _viewModel.ErrorMessage = "This account is locked. Contact administrator.";
                return;
            }

            // Check if account is inactive
            if (user.Status == StaffStatus.Inactive)
            {
                _viewModel.ErrorMessage = "This account is inactive. Contact administrator.";
                return;
            }

            // Successfully authenticated - set current user and navigate
            AppContext.Instance.CurrentUser = user;

            // Log login activity
            ActivityLogService.Instance.LogLogin(user.Email);

            App.ShowAppShell();
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    /// <summary>
    /// Navigates to the forgot password page.
    /// </summary>
    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ForgotPasswordPage());
    }
}