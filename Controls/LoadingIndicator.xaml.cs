namespace AppManagermentRestaurant.Controls;

/// <summary>
/// Control for displaying loading state with spinner animation.
/// </summary>
public partial class LoadingIndicator : ContentView
{
    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(LoadingIndicator), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((LoadingIndicator)bindable).UpdateVisibility();
            });

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(LoadingIndicator), "Loading...",
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((LoadingIndicator)bindable).LoadingMessage.Text = (string)newValue;
            });

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private Label LoadingMessage { get; set; } = null!;

    public LoadingIndicator()
    {
        InitializeComponent();
        LoadingMessage = this.FindByName<Label>("LoadingMessageLabel");
    }

    private void UpdateVisibility()
    {
        this.IsVisible = IsLoading;
    }
}
