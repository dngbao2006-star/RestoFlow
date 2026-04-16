namespace AppManagermentRestaurant.Controls;

/// <summary>
/// Control for displaying progress through multi-step operations.
/// </summary>
public partial class StepProgressIndicator : ContentView
{
    public static readonly BindableProperty CurrentStepProperty =
        BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(StepProgressIndicator), 1,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((StepProgressIndicator)bindable).UpdateProgress();
            });

    public static readonly BindableProperty TotalStepsProperty =
        BindableProperty.Create(nameof(TotalSteps), typeof(int), typeof(StepProgressIndicator), 1,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((StepProgressIndicator)bindable).UpdateProgress();
            });

    public static readonly BindableProperty StepLabelsProperty =
        BindableProperty.Create(nameof(StepLabels), typeof(List<string>), typeof(StepProgressIndicator), new List<string>(),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((StepProgressIndicator)bindable).UpdateLabels();
            });

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public int TotalSteps
    {
        get => (int)GetValue(TotalStepsProperty);
        set => SetValue(TotalStepsProperty, value);
    }

    public List<string> StepLabels
    {
        get => (List<string>)GetValue(StepLabelsProperty);
        set => SetValue(StepLabelsProperty, value);
    }

    public StepProgressIndicator()
    {
        InitializeComponent();
    }

    private void UpdateProgress()
    {
        double progress = TotalSteps > 0 ? (double)(CurrentStep - 1) / (TotalSteps - 1) : 0;
        if (Content is Grid grid && grid.Children.OfType<ProgressBar>().FirstOrDefault() is ProgressBar progressBar)
        {
            progressBar.Progress = progress;
        }
    }

    private void UpdateLabels()
    {
        // Labels will be displayed in the XAML
    }
}
