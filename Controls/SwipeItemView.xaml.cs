using System.Windows.Input;

namespace AppManagermentRestaurant.Controls;

/// <summary>
/// Helper control for swipe actions on list items.
/// Provides consistent swipe-to-delete or swipe-to-edit functionality.
/// </summary>
public partial class SwipeItemView : ContentView
{
    public static readonly BindableProperty DeleteCommandProperty =
        BindableProperty.Create(nameof(DeleteCommand), typeof(ICommand), typeof(SwipeItemView), null);

    public static readonly BindableProperty EditCommandProperty =
        BindableProperty.Create(nameof(EditCommand), typeof(ICommand), typeof(SwipeItemView), null);

    public static readonly BindableProperty ContentProperty =
        BindableProperty.Create(nameof(MainContent), typeof(View), typeof(SwipeItemView), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((SwipeItemView)bindable).UpdateContent((View)newValue);
            });

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public View MainContent
    {
        get => (View)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    private Grid _contentContainer = null!;

    public SwipeItemView()
    {
        InitializeComponent();
        _contentContainer = (Grid)Content;
    }

    private void UpdateContent(View? view)
    {
        if (_contentContainer != null && view != null)
        {
            _contentContainer.Clear();
            _contentContainer.Add(view);
        }
    }
}
