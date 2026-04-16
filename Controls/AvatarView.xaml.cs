namespace AppManagermentRestaurant.Controls;

public enum AvatarSize
{
    Small = 32,
    Medium = 40,
    Large = 56,
    XLarge = 80
}

public partial class AvatarView : ContentView
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create("Source", typeof(string), typeof(AvatarView), null);

    public static readonly BindableProperty InitialsProperty =
        BindableProperty.Create("Initials", typeof(string), typeof(AvatarView), "AB");

    public static readonly BindableProperty AvatarSizeProperty =
        BindableProperty.Create("AvatarSize", typeof(AvatarSize), typeof(AvatarView), AvatarSize.Medium);

    public static readonly BindableProperty BackgroundColorProperty =
        BindableProperty.Create("BackgroundColor", typeof(Color), typeof(AvatarView), Color.FromArgb("#1B3A6B"));

    public static readonly BindableProperty ShowStatusProperty =
        BindableProperty.Create("ShowStatus", typeof(bool), typeof(AvatarView), false);

    public static readonly BindableProperty IsOnlineProperty =
        BindableProperty.Create("IsOnline", typeof(bool), typeof(AvatarView), false);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    public AvatarSize AvatarSize
    {
        get => (AvatarSize)GetValue(AvatarSizeProperty);
        set => SetValue(AvatarSizeProperty, value);
    }

    public Color BackgroundColor
    {
        get => (Color)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public bool ShowStatus
    {
        get => (bool)GetValue(ShowStatusProperty);
        set => SetValue(ShowStatusProperty, value);
    }

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    public AvatarView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == SourceProperty.PropertyName)
            {
                AvatarImage.Source = Source;
                AvatarImage.IsVisible = !string.IsNullOrEmpty(Source);
                InitialsBorder.IsVisible = string.IsNullOrEmpty(Source);
            }

            if (e.PropertyName == InitialsProperty.PropertyName)
                InitialsLabel.Text = Initials;

            if (e.PropertyName == AvatarSizeProperty.PropertyName)
            {
                var size = (int)AvatarSize;
                WidthRequest = size;
                HeightRequest = size;
            }

            if (e.PropertyName == BackgroundColorProperty.PropertyName)
                InitialsBorder.Background = BackgroundColor;

            if (e.PropertyName == ShowStatusProperty.PropertyName)
                StatusIndicator.IsVisible = ShowStatus;

            if (e.PropertyName == IsOnlineProperty.PropertyName)
                StatusIndicator.Fill = IsOnline ? Color.FromArgb("#22C55E") : Color.FromArgb("#9CA3AF");
        };

        InitialsBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 999 };
    }
}
