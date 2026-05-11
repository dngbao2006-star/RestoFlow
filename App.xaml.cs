namespace AppManagermentRestaurant;

public partial class App : Application
{
    public App(Services.AppContext appContext)
	{
      _ = appContext;
		InitializeComponent();
		MainPage = new NavigationPage(new Views.Pages.LoginPage());

		// Load data from Firebase asynchronously
		Task.Run(async () =>
		{
			await appContext.InitializeAsync();
		});
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