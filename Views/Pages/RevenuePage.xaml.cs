using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class RevenuePage : ContentPage
{
    private readonly RevenueViewModel _vm;

    public RevenuePage()
    {
        InitializeComponent();
        _vm = new RevenueViewModel();
        BindingContext = _vm;
    }

    private void OnPeriodFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string period) return;

        _vm.SelectedPeriod = period;

        // Reset all filter buttons
        var defaultBg = Color.FromArgb("#FDFAF6");
        var activeBg = Color.FromArgb("#F5F0E8");
        BtnToday.BackgroundColor = defaultBg;
        BtnWeek.BackgroundColor = defaultBg;
        BtnYear.BackgroundColor = defaultBg;

        // Highlight the active button
        btn.BackgroundColor = activeBg;
    }
}
