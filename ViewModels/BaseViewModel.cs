using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.ViewModels;

/// <summary>
/// Base ViewModel with common UX features like loading states, error handling, and notifications.
/// </summary>
public class BaseViewModel : ObservableObject
{
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>
    /// Clears all messages.
    /// </summary>
    public void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    /// <summary>
    /// Sets an error message.
    /// </summary>
    public void SetError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = string.Empty;
    }

    /// <summary>
    /// Sets a success message.
    /// </summary>
    public void SetSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Executes an async operation with loading state.
    /// </summary>
    public async Task ExecuteAsync(Func<Task> operation)
    {
        try
        {
            IsLoading = true;
            ClearMessages();
            await operation();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes an async operation with loading state and returns a result.
    /// </summary>
    public async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            IsLoading = true;
            ClearMessages();
            return await operation();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
