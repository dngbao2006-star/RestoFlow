using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class InvoiceManagementPage : ContentPage
{
    public InvoiceManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }
}
