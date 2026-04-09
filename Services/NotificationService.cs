namespace AppManagermentRestaurant.Services;

/// <summary>
/// Service for displaying toast notifications and alerts to users.
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// Shows a success notification.
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message = "")
    {
        await Application.Current!.MainPage!.DisplayAlert(title, message, "OK");
    }

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message = "")
    {
        await Application.Current!.MainPage!.DisplayAlert("❌ " + title, message, "OK");
    }

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    public static async Task ShowWarningAsync(string title, string message = "")
    {
        await Application.Current!.MainPage!.DisplayAlert("⚠️ " + title, message, "OK");
    }

    /// <summary>
    /// Shows an info notification.
    /// </summary>
    public static async Task ShowInfoAsync(string title, string message = "")
    {
        await Application.Current!.MainPage!.DisplayAlert("ℹ️ " + title, message, "OK");
    }

    /// <summary>
    /// Shows a confirmation dialog and returns user's choice.
    /// </summary>
    public static async Task<bool> ShowConfirmAsync(string title, string message = "", string acceptText = "Yes", string cancelText = "No")
    {
        return await Application.Current!.MainPage!.DisplayAlert(title, message, acceptText, cancelText);
    }

    /// <summary>
    /// Shows a toast-like snackbar message at the bottom.
    /// </summary>
    public static void ShowToast(string message)
    {
        if (Application.Current?.MainPage != null)
        {
            // Create a toast-like display using SemanticScreenReader
            SemanticScreenReader.Announce(message);
        }
    }
}
