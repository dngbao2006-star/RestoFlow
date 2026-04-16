namespace AppManagermentRestaurant.Controls;

public partial class ProgressRingView : ContentView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create("Progress", typeof(double), typeof(ProgressRingView), 0.0);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create("Subtitle", typeof(string), typeof(ProgressRingView), null);

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create("Color", typeof(Color), typeof(ProgressRingView), Color.FromArgb("#1B3A6B"));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ProgressRingView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == ProgressProperty.PropertyName)
                UpdateProgress();
            
            if (e.PropertyName == SubtitleProperty.PropertyName)
            {
                SubtitleLabel.Text = Subtitle;
                SubtitleLabel.IsVisible = !string.IsNullOrEmpty(Subtitle);
            }

            if (e.PropertyName == ColorProperty.PropertyName)
                ProgressPath.Stroke = Color;
        };

        UpdateProgress();
    }

    private void UpdateProgress()
    {
        var clampedProgress = Math.Clamp(Progress, 0, 1);
        PercentageLabel.Text = $"{(int)(clampedProgress * 100)}%";
        
        // Simple visual update - in production, you might use a proper circular progress animation
        ProgressPath.Opacity = clampedProgress;
    }
}
