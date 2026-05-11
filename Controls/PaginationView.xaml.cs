namespace AppManagermentRestaurant.Controls;

public partial class PaginationView : ContentView
{
    public event EventHandler<int>? PageChanged;

    public static readonly BindableProperty CurrentPageProperty =
        BindableProperty.Create("CurrentPage", typeof(int), typeof(PaginationView), 1);

    public static readonly BindableProperty TotalPagesProperty =
        BindableProperty.Create("TotalPages", typeof(int), typeof(PaginationView), 1);

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public PaginationView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == CurrentPageProperty.PropertyName || 
                e.PropertyName == TotalPagesProperty.PropertyName)
            {
                UpdateUI();
            }
        };
    }

    private void UpdateUI()
    {
        PageInfoLabel.Text = $"Page {CurrentPage} of {TotalPages}";
        PreviousButton.IsEnabled = CurrentPage > 1;
        NextButton.IsEnabled = CurrentPage < TotalPages;
    }

    private void OnPreviousClicked(object sender, EventArgs e)
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            PageChanged?.Invoke(this, CurrentPage);
        }
    }

    private void OnNextClicked(object sender, EventArgs e)
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            PageChanged?.Invoke(this, CurrentPage);
        }
    }
}
