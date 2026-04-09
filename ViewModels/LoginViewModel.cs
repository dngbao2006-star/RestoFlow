using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }
}
