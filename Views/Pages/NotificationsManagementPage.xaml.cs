using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class NotificationsManagementPage : ContentPage
{
    public NotificationsManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }
}
