using Microsoft.Extensions.Logging;

namespace AppManagermentRestaurant;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("PlayfairDisplay-Regular.ttf", "Playfair Display");
				fonts.AddFont("Inter-Regular.ttf", "Inter");
			});

		// Register services
		builder.Services.AddSingleton<AppContext>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
