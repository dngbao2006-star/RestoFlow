using AppManagermentRestaurant.Helpers;
namespace AppManagermentRestaurant.ViewModels;

public class AccountViewModel : ObservableObject
{
    public AppManagermentRestaurant.Services.AppContext Context { get; }

    private string _selectedTab = "Profile";

    public AccountViewModel(AppManagermentRestaurant.Services.AppContext context)
    {
        Context = context;
    }

    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(IsProfileTab));
                OnPropertyChanged(nameof(IsPasswordTab));
                OnPropertyChanged(nameof(IsVerificationTab));
                OnPropertyChanged(nameof(IsActivityTab));
            }
        }
    }

    public bool IsProfileTab => SelectedTab == "Profile";
    public bool IsPasswordTab => SelectedTab == "Password";
    public bool IsVerificationTab => SelectedTab == "Verification";
    public bool IsActivityTab => SelectedTab == "Activity";
}
