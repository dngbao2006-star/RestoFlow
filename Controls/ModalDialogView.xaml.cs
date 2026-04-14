namespace AppManagermentRestaurant.Controls;

public partial class ModalDialogView : ContentView
{
    public event EventHandler<EventArgs>? OnConfirmed;
    public event EventHandler<EventArgs>? OnCancelled;

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create("Title", typeof(string), typeof(ModalDialogView), "Dialog Title");

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create("Subtitle", typeof(string), typeof(ModalDialogView), null);

    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create("IsVisible", typeof(bool), typeof(ModalDialogView), false);

    public static readonly BindableProperty ContentProperty =
        BindableProperty.Create("Content", typeof(View), typeof(ModalDialogView), null);

    public static readonly BindableProperty ConfirmTextProperty =
        BindableProperty.Create("ConfirmText", typeof(string), typeof(ModalDialogView), "Confirm");

    public static readonly BindableProperty CancelTextProperty =
        BindableProperty.Create("CancelText", typeof(string), typeof(ModalDialogView), "Cancel");

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public View Content
    {
        get => (View)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public string ConfirmText
    {
        get => (string)GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public ModalDialogView()
    {
        InitializeComponent();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == TitleProperty.PropertyName)
                TitleLabel.Text = Title;
            
            if (e.PropertyName == SubtitleProperty.PropertyName)
            {
                SubtitleLabel.Text = Subtitle;
                SubtitleLabel.IsVisible = !string.IsNullOrEmpty(Subtitle);
            }

            if (e.PropertyName == ContentProperty.PropertyName)
            {
                if (Content != null)
                    ContentPresenter.Content = Content;
            }

            if (e.PropertyName == ConfirmTextProperty.PropertyName)
                ConfirmButton.Text = ConfirmText;

            if (e.PropertyName == CancelTextProperty.PropertyName)
                CancelButton.Text = CancelText;
        };
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        IsVisible = false;
        OnCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        IsVisible = false;
        OnCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        IsVisible = false;
        OnConfirmed?.Invoke(this, EventArgs.Empty);
    }

    public void Show()
    {
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }
}
