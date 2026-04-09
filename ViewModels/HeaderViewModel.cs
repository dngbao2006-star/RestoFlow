using AppManagermentRestaurant.Helpers;
namespace AppManagermentRestaurant.ViewModels;

public class HeaderViewModel : ObservableObject
{
    private bool _isNotificationsOpen;

    public AppManagermentRestaurant.Services.AppContext AppContext { get; }

    public HeaderViewModel(AppManagermentRestaurant.Services.AppContext appContext)
    {
        AppContext = appContext;
        TodayLabel = DateTime.Now.ToString("dddd, MMMM d, yyyy");
    }

    public string TodayLabel { get; }

    public bool IsNotificationsOpen
    {
        get => _isNotificationsOpen;
        set => SetProperty(ref _isNotificationsOpen, value);
    }

    public void ToggleNotifications()
    {
        IsNotificationsOpen = !IsNotificationsOpen;
        if (IsNotificationsOpen)
        {
            AppContext.MarkNotificationsRead();
        }
    }
}
