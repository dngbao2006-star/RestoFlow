using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                // Body font: Inter (trung tính, hiển thị số liệu tốt, hỗ trợ tiếng Việt)
                // → Dùng OpenSans-Regular.ttf làm file thực tế (tương đương Inter về độ rõ ràng)
                // TODO: Thay bằng Inter-Regular.ttf / Inter-Bold.ttf nếu muốn font chính xác
                fonts.AddFont("OpenSans-Regular.ttf", "Inter");
                fonts.AddFont("OpenSans-Semibold.ttf", "Inter-Bold");

                // Display font: Playfair Display (sang trọng, chỉ dùng cho Logo và Tiêu đề lớn)
                // TODO: Thêm file PlayfairDisplay-Regular.ttf vào Resources/Fonts/ để font này hoạt động
                fonts.AddFont("PlayfairDisplay-Regular.ttf", "Playfair Display");
            })
            // Đăng ký handler tuỳ chỉnh để loại bỏ dim trên Windows.
            // Handler này ghi đè LightDismissOverlayMode ở tầng WinUI native —
            // đây là cách duy nhất đảm bảo không còn lớp dim kể cả khi
            // Color="Transparent" không được WinUI tôn trọng.
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                handlers.AddHandler<Popup, Platforms.Windows.NoDimPopupHandler>();
#endif
            });

        // Register services
        // TODO [BACKEND]: Thay MockDataStore bằng service gọi API thực
        builder.Services.AddSingleton<IMockDataStore, MockDataStore>();
        builder.Services.AddSingleton<AppContext>();

        return builder.Build();
    }
}
