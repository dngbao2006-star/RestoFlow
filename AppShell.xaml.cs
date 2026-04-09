using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Views.Pages;

namespace AppManagermentRestaurant;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		BindingContext = AppContext.Instance;

		// ✅ Đăng ký route
		Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
		Routing.RegisterRoute("ordercreation", typeof(OrderCreationPage));
		Routing.RegisterRoute("staff/orders", typeof(OrderCreationPage));
		Routing.RegisterRoute("staff/dish-status", typeof(DishStatusPage));
		Routing.RegisterRoute("staff/tables", typeof(TableManagementPage));
		Routing.RegisterRoute("staff/dashboard", typeof(DashboardPage));
	}

	private async void OnSignOutClicked(object sender, EventArgs e)
	{
		var dialogService = new Services.DialogService(Shell.Current);
		var confirmed = await dialogService.ShowLogoutConfirmAsync();

		if (confirmed)
		{
			// Log logout activity before clearing user
			ActivityLogService.Instance.LogLogout();

			AppContext.Instance.CurrentUser = null;
			App.ShowLogin();
		}
	}

	private async void OnBackButtonClicked(object sender, EventArgs e)
	{
		// Clear selected table and order when going back
		AppContext.Instance.SelectedTable = null;
		AppContext.Instance.SelectedOrder = null;

		// Navigate back or show flyout menu
		if (Shell.Current.Navigation.NavigationStack.Count > 1)
		{
			await Shell.Current.Navigation.PopAsync();
		}
		else
		{
			// If no navigation history, show flyout menu
			Shell.Current.FlyoutIsPresented = true;
		}
	}
}
