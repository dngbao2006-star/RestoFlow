namespace AppManagermentRestaurant;

public partial class App : Application
{
    public App(Services.AppContext appContext)
	{
      _ = appContext;
		InitializeComponent();
		MainPage = new NavigationPage(new Views.Pages.LoginPage());

		// Start on the UI synchronization context. SeedAsync populates observable
		// collections that are bound by MAUI and must not be mutated from Task.Run.
		MainThread.BeginInvokeOnMainThread(
			() => _ = ObserveInitializationAsync(appContext.InitializeOnceAsync()));
	}

	private static async Task ObserveInitializationAsync(Task initializationTask)
	{
		try
		{
			await initializationTask;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[Startup] Firebase initialization failed: {ex}");
		}
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
