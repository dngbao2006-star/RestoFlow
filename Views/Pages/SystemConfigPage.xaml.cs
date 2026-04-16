using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class SystemConfigPage : ContentPage
{
    public SystemConfigPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }
}
