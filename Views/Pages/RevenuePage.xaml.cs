using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class RevenuePage : ContentPage
{
    public RevenuePage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }
}
