using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }
}
