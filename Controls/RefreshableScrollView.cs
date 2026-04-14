using System.Windows.Input;

namespace AppManagermentRestaurant.Controls;

/// <summary>
/// Helper for pull-to-refresh functionality using RefreshView.
/// Can be used as a wrapper for refresh operations.
/// </summary>
public static class RefreshHelper
{
    /// <summary>
    /// Executes a refresh command when pull-to-refresh is triggered.
    /// </summary>
    public static async Task RefreshAsync(ICommand refreshCommand)
    {
        if (refreshCommand?.CanExecute(null) == true)
        {
            refreshCommand.Execute(null);
            await Task.Delay(500); // Small delay for visual feedback
        }
    }
}
