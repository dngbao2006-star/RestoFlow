namespace AppManagermentRestaurant.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }


    private async void OnSendResetClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Success", "Reset link sent!", "OK");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}