namespace AppManagermentRestaurant.Controls;

public partial class ChipView : ContentView
{
    public event EventHandler<EventArgs>? Removed;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create("Text", typeof(string), typeof(ChipView), "Chip");

    public static readonly BindableProperty IsRemovableProperty =
        BindableProperty.Create("IsRemovable", typeof(bool), typeof(ChipView), false);

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create("Color", typeof(Color), typeof(ChipView), Color.FromArgb("#E5E7EB"));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsRemovable
    {
        get => (bool)GetValue(IsRemovableProperty);
        set => SetValue(IsRemovableProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ChipView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == TextProperty.PropertyName)
                ChipLabel.Text = Text;
            
            if (e.PropertyName == IsRemovableProperty.PropertyName)
                RemoveButton.IsVisible = IsRemovable;

            if (e.PropertyName == ColorProperty.PropertyName)
            {
                ChipBorder.Background = Color;
            }
        };

        ChipBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 };
    }

    private void OnRemoveClicked(object sender, EventArgs e)
    {
        Removed?.Invoke(this, EventArgs.Empty);
    }
}
