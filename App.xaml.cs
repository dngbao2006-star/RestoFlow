namespace AppManagermentRestaurant;

public partial class App : Application
{
    public App(Services.AppContext appContext)
	{
      _ = appContext;
		InitializeComponent();
		MainPage = new NavigationPage(new Views.Pages.LoginPage());
	}

	public static void ShowAppShell()
	{
		if (Current != null)
		{
			Current.MainPage = new AppShell();
		}
	}

	public static void ShowLogin()
	{
		if (Current != null)
		{
			Current.MainPage = new NavigationPage(new Views.Pages.LoginPage());
		}
	}
}