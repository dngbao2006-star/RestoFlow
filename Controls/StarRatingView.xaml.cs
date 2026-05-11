namespace AppManagermentRestaurant.Controls;

public partial class StarRatingView : ContentView
{
    public event EventHandler<int>? RatingChanged;

    public static readonly BindableProperty RatingProperty =
        BindableProperty.Create("Rating", typeof(int), typeof(StarRatingView), 0);

    public static readonly BindableProperty MaxRatingProperty =
        BindableProperty.Create("MaxRating", typeof(int), typeof(StarRatingView), 5);

    public static readonly BindableProperty IsReadOnlyProperty =
        BindableProperty.Create("IsReadOnly", typeof(bool), typeof(StarRatingView), false);

    public int Rating
    {
        get => (int)GetValue(RatingProperty);
        set => SetValue(RatingProperty, value);
    }

    public int MaxRating
    {
        get => (int)GetValue(MaxRatingProperty);
        set => SetValue(MaxRatingProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public StarRatingView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == RatingProperty.PropertyName || 
                e.PropertyName == MaxRatingProperty.PropertyName ||
                e.PropertyName == IsReadOnlyProperty.PropertyName)
            {
                RenderStars();
            }
        };
    }

    private void RenderStars()
    {
        StarContainer.Children.Clear();

        for (int i = 1; i <= MaxRating; i++)
        {
            var starButton = new Button
            {
                Text = i <= Rating ? "★" : "☆",
                TextColor = i <= Rating ? Color.FromArgb("#F59E0B") : Color.FromArgb("#D1D5DB"),
                FontSize = 24,
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(4),
                IsEnabled = !IsReadOnly,
                CommandParameter = i
            };

            if (!IsReadOnly)
            {
                starButton.Clicked += (s, e) =>
                {
                    int newRating = (int)((Button)s).CommandParameter;
                    Rating = newRating;
                    RatingChanged?.Invoke(this, Rating);
                    RenderStars();
                };
            }

            StarContainer.Add(starButton);
        }
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        RenderStars();
    }
}
