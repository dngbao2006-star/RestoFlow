using AppManagermentRestaurant.Constants;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Views.Pages;

namespace AppManagermentRestaurant;

public partial class AppShell : Shell
{
    public static readonly BindableProperty IsSidebarExpandedProperty = BindableProperty.Create(
        nameof(IsSidebarExpanded), typeof(bool), typeof(AppShell), true,
        propertyChanged: (b, o, n) =>
        {
            if (b is AppShell shell)
            {
                shell.FlyoutWidth = (bool)n ? 260 : 72;
            }
        });

    public bool IsSidebarExpanded
    {
        get => (bool)GetValue(IsSidebarExpandedProperty);
        set => SetValue(IsSidebarExpandedProperty, value);
    }

    public AppShell()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;

        Routing.RegisterRoute(AppRoutes.ForgotPassword, typeof(ForgotPasswordPage));
        Routing.RegisterRoute(AppRoutes.Profile, typeof(AccountManagementPage));
    }

    private async Task HandleSignOutAsync()
    {
        var hostPage = Shell.Current ?? this;
        var dialogService = new DialogService(hostPage);
        var confirmed = await dialogService.ShowLogoutConfirmAsync();

        if (!confirmed)
        {
            return;
        }

        ActivityLogService.Instance.LogLogout();
        this.BindingContext = null;
        await MainThread.InvokeOnMainThreadAsync(App.ShowLogin);
        AppContext.Instance.CurrentUser = null;
    }

    private async void OnSignOutTapped(object sender, TappedEventArgs e)
    {
        await HandleSignOutAsync();
    }
}
