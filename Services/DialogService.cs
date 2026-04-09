namespace AppManagermentRestaurant.Services;

/// <summary>
/// Service for displaying dialogs and alerts to users.
/// Provides consistent dialog handling across the application.
/// </summary>
public class DialogService
{
    private readonly Page _page;

    public DialogService(Page page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    /// <summary>
    /// Shows an alert dialog.
    /// </summary>
    public async Task ShowAlertAsync(string title, string message)
    {
        await _page.DisplayAlert(title, message, "OK");
    }

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    public async Task<bool> ShowConfirmAsync(string title, string message, string acceptText = "Yes", string cancelText = "No")
    {
        return await _page.DisplayAlert(title, message, acceptText, cancelText);
    }

    /// <summary>
    /// Shows an action sheet with multiple options.
    /// </summary>
    public async Task<string?> ShowActionSheetAsync(string title, string cancelText, string? destructiveText, params string[] otherButtons)
    {
        return await _page.DisplayActionSheet(title, cancelText, destructiveText, otherButtons);
    }

    /// <summary>
    /// Shows an input prompt dialog.
    /// </summary>
    public async Task<string?> ShowInputAsync(string title, string message = "", string initialValue = "", string okText = "OK", string cancelText = "Cancel")
    {
        return await _page.DisplayPromptAsync(title, message, okText, cancelText, placeholder: null, maxLength: -1, keyboard: Keyboard.Default, initialValue: initialValue);
    }

    /// <summary>
    /// Shows a delete confirmation dialog.
    /// </summary>
    public async Task<bool> ShowDeleteConfirmAsync(string itemName)
    {
        return await ShowConfirmAsync(
            "Delete Confirmation",
            $"Are you sure you want to delete {itemName}? This action cannot be undone.",
            "Delete",
            "Cancel"
        );
    }

    /// <summary>
    /// Shows a logout confirmation dialog.
    /// </summary>
    public async Task<bool> ShowLogoutConfirmAsync()
    {
        return await ShowConfirmAsync(
            "Logout Confirmation",
            "Are you sure you want to sign out?",
            "Sign Out",
            "Cancel"
        );
    }
}
