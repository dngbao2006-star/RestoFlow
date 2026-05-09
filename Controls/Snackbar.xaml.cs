using System.Windows.Input;

namespace AppManagermentRestaurant.Controls;

/// <summary>
/// Snackbar control for displaying toast-like notifications at the bottom of the screen.
/// </summary>
public partial class Snackbar : ContentView
{
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(Snackbar), "");

    public static readonly BindableProperty MessageTypeProperty =
        BindableProperty.Create(nameof(MessageType), typeof(SnackbarType), typeof(Snackbar), SnackbarType.Info,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((Snackbar)bindable).UpdateStyle();
            });

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(Snackbar), "");

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(Snackbar), null);

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public SnackbarType MessageType
    {
        get => (SnackbarType)GetValue(MessageTypeProperty);
        set => SetValue(MessageTypeProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public Snackbar()
    {
        InitializeComponent();
    }

    public async Task ShowAsync(int duration = 3000)
    {
        this.IsVisible = true;
        await Task.Delay(duration);
        this.IsVisible = false;
    }

    private void UpdateStyle()
    {
        if (Content is not Border border) return;

        border.Background = MessageType switch
        {
            SnackbarType.Success => (Color)Application.Current!.Resources["Success"],
            SnackbarType.Error => (Color)Application.Current!.Resources["Danger"],
            SnackbarType.Warning => (Color)Application.Current!.Resources["Warning"],
            _ => (Color)Application.Current!.Resources["Primary"],
        };
    }
}

public enum SnackbarType
{
    Info,
    Success,
    Warning,
    Error
}
