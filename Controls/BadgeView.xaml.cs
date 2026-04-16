namespace AppManagermentRestaurant.Controls;

public enum BadgeType
{
    Primary,
    Success,
    Warning,
    Danger,
    Info,
    Muted
}

public partial class BadgeView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create("Text", typeof(string), typeof(BadgeView), "Badge");

    public static readonly BindableProperty BadgeTypeProperty =
        BindableProperty.Create("BadgeType", typeof(BadgeType), typeof(BadgeView), BadgeType.Primary);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public BadgeType BadgeType
    {
        get => (BadgeType)GetValue(BadgeTypeProperty);
        set => SetValue(BadgeTypeProperty, value);
    }

    public BadgeView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == TextProperty.PropertyName)
                BadgeLabel.Text = Text;
            
            if (e.PropertyName == BadgeTypeProperty.PropertyName)
                UpdateBadgeStyle();
        };

        UpdateBadgeStyle();
    }

    private void UpdateBadgeStyle()
    {
        var (backgroundColor, textColor) = BadgeType switch
        {
            BadgeType.Success => (Color.FromArgb("#D1FAE5"), Color.FromArgb("#065F46")),
            BadgeType.Warning => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#92400E")),
            BadgeType.Danger => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#991B1B")),
            BadgeType.Info => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#1E40AF")),
            BadgeType.Muted => (Color.FromArgb("#F3F4F6"), Color.FromArgb("#6B7280")),
            _ => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#1E40AF")) // Primary
        };

        BadgeBorder.Background = backgroundColor;
        BadgeLabel.TextColor = textColor;
        BadgeBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 999 };
    }
}
