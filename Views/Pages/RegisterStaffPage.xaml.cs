// client-24520222
// server-DuongDangChinh
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class RegisterStaffPage : ContentPage
{
    public RegisterStaffPage()
    {
        InitializeComponent();
        BindingContext = new RegisterStaffViewModel();
    }
}