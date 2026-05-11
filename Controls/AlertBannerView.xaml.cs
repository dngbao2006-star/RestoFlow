namespace AppManagermentRestaurant.Controls;

public enum AlertType
{
    Info,
    Success,
    Warning,
    Danger
}

public partial class AlertBannerView : ContentView
{
    public event EventHandler<EventArgs>? Closed;

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create("Title", typeof(string), typeof(AlertBannerView), "Alert");

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create("Message", typeof(string), typeof(AlertBannerView), null);

    public static readonly BindableProperty AlertTypeProperty =
        BindableProperty.Create("AlertType", typeof(AlertType), typeof(AlertBannerView), AlertType.Info);

    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create("IsVisible", typeof(bool), typeof(AlertBannerView), false);

    public static readonly BindableProperty IsClosableProperty =
        BindableProperty.Create("IsClosable", typeof(bool), typeof(AlertBannerView), false);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public AlertType AlertType
    {
        get => (AlertType)GetValue(AlertTypeProperty);
        set => SetValue(AlertTypeProperty, value);
    }

    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public AlertBannerView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == TitleProperty.PropertyName)
                TitleLabel.Text = Title;

            if (e.PropertyName == MessageProperty.PropertyName)
            {
                MessageLabel.Text = Message;
                MessageLabel.IsVisible = !string.IsNullOrEmpty(Message);
            }

            if (e.PropertyName == AlertTypeProperty.PropertyName)
                UpdateAlertStyle();

            if (e.PropertyName == IsVisibleProperty.PropertyName)
                UpdateIsVisible();

            if (e.PropertyName == IsClosableProperty.PropertyName)
                CloseButton.IsVisible = IsClosable;
        };

        UpdateAlertStyle();
    }

    private void UpdateAlertStyle()
    {
        var (backgroundColor, textColor, borderColor, icon) = AlertType switch
        {
            AlertType.Success => (Color.FromArgb("#D1FAE5"), Color.FromArgb("#065F46"), Color.FromArgb("#22C55E"), "✓"),
            AlertType.Warning => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#92400E"), Color.FromArgb("#F59E0B"), "⚠"),
            AlertType.Danger => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#991B1B"), Color.FromArgb("#EF4444"), "✕"),
            _ => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#1E40AF"), Color.FromArgb("#4A90D9"), "ℹ")
        };

        AlertBorder.Background = backgroundColor;
        AlertBorder.Stroke = borderColor;
        TitleLabel.TextColor = textColor;
        MessageLabel.TextColor = textColor;
        CloseButton.TextColor = textColor;
        IconLabel.Text = icon;
        IconLabel.TextColor = textColor;
    }

    private void UpdateIsVisible()
    {
        AlertBorder.IsVisible = IsVisible;
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        IsVisible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Show(string title, string message = null, AlertType type = AlertType.Info)
    {
        Title = title;
        Message = message;
        AlertType = type;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }
}
