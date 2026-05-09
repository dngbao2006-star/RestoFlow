using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class MenuManagementPage : ContentPage
{
    public MenuManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }
}
