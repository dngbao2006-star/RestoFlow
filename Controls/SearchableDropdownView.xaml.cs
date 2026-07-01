using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Controls;

public partial class SearchableDropdownView : ContentView
{
    private static SearchableDropdownView? _activeDropdown;
    private bool _isUpdatingText;
    private bool _isDropdownOpen;

    // The floating popup elements – created once and reused.
    private Border? _popupBorder;
    private CollectionView? _popupList;
    private bool _popupAttached;

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(SearchableDropdownView), string.Empty);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(SearchableDropdownView), string.Empty,
        BindingMode.TwoWay, propertyChanged: OnTextPropertyChanged);

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable<string>), typeof(SearchableDropdownView),
        Enumerable.Empty<string>(), propertyChanged: OnItemsSourceChanged);

    public ObservableCollection<string> FilteredItems { get; } = new();

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public IEnumerable<string> ItemsSource
    {
        get => (IEnumerable<string>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value ?? Enumerable.Empty<string>());
    }

    public bool IsDropdownOpen
    {
        get => _isDropdownOpen;
        private set
        {
            if (_isDropdownOpen == value) return;
            _isDropdownOpen = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler? SelectionChanged;

    public SearchableDropdownView()
    {
        InitializeComponent();
        ApplyFilter(string.Empty);
    }

    public void Clear()
    {
        Text = string.Empty;
        HideDropdown();
    }

    private static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SearchableDropdownView)bindable;
        var text = newValue?.ToString() ?? string.Empty;
        if (control.InputEntry.Text != text)
        {
            control._isUpdatingText = true;
            control.InputEntry.Text = text;
            control._isUpdatingText = false;
        }

        if (control.IsDropdownOpen)
            control.ApplyFilter(text);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SearchableDropdownView)bindable;
        control.ApplyFilter(control.IsDropdownOpen ? control.Text : string.Empty);
    }

    private void OnEntryFocused(object sender, FocusEventArgs e)
        => ShowDropdown(resetFilter: true);

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText) return;
        Text = e.NewTextValue ?? string.Empty;
        ShowDropdown(resetFilter: false);
        ApplyFilter(Text);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnEntryCompleted(object sender, EventArgs e)
    {
        HideDropdown();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDropdownClicked(object sender, EventArgs e)
    {
        if (IsDropdownOpen)
        {
            HideDropdown();
            return;
        }

        ShowDropdown(resetFilter: true);
    }

    private void OnItemClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string selected }) return;

        Text = selected;
        HideDropdown();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // ─────────────────────────────────────────────────────────
    //  Popup management – dropdown floats on top of the page
    // ─────────────────────────────────────────────────────────

    private void ShowDropdown(bool resetFilter)
    {
        if (_activeDropdown != null && _activeDropdown != this)
            _activeDropdown.HideDropdown();

        _activeDropdown = this;
        ApplyFilter(resetFilter ? string.Empty : Text);
        IsDropdownOpen = true;

        EnsurePopupCreated();
        AttachPopupToPage();
        PositionPopup();
        _popupBorder!.IsVisible = true;
    }

    private void HideDropdown()
    {
        IsDropdownOpen = false;
        if (_popupBorder != null)
            _popupBorder.IsVisible = false;
        if (_activeDropdown == this)
            _activeDropdown = null;
    }

    /// <summary>
    /// Creates the floating Border + CollectionView once.
    /// </summary>
    private void EnsurePopupCreated()
    {
        if (_popupBorder != null) return;

        Color textPrimary = Colors.Black;
        Color textMuted = Colors.Gray;
        Color surface = Colors.White;
        Color borderStrong = Color.FromArgb("#D1D5DB");

        if (Application.Current?.Resources != null)
        {
            if (Application.Current.Resources.TryGetValue("TextPrimary", out var tp) && tp is Color tpColor)
                textPrimary = tpColor;
            if (Application.Current.Resources.TryGetValue("TextMuted", out var tm) && tm is Color tmColor)
                textMuted = tmColor;
            if (Application.Current.Resources.TryGetValue("Surface", out var sf) && sf is Color sfColor)
                surface = sfColor;
            if (Application.Current.Resources.TryGetValue("BorderStrong", out var bs) && bs is Color bsColor)
                borderStrong = bsColor;
        }

        _popupList = new CollectionView
        {
            ItemsSource = FilteredItems,
            SelectionMode = SelectionMode.None,
            MaximumHeightRequest = 220,
            ItemTemplate = new DataTemplate(() =>
            {
                var btn = new Button
                {
                    BackgroundColor = Colors.Transparent,
                    TextColor = textPrimary,
                    HorizontalOptions = LayoutOptions.Fill,
                    Padding = new Thickness(10, 8),
                };
                btn.SetBinding(Button.TextProperty, ".");
                btn.SetBinding(Button.CommandParameterProperty, ".");
                btn.Clicked += OnItemClicked;
                return btn;
            }),
        };

        var emptyLabel = new Label
        {
            Text = "Không có lựa chọn phù hợp",
            Padding = new Thickness(10),
            FontSize = 12,
            TextColor = textMuted,
        };
        _popupList.EmptyView = emptyLabel;

        _popupBorder = new Border
        {
            BackgroundColor = surface,
            Stroke = new SolidColorBrush(borderStrong),
            StrokeThickness = 1,
            Padding = new Thickness(6),
            MaximumHeightRequest = 240,
            IsVisible = false,
            InputTransparent = false,
            ZIndex = 9999,
            Content = _popupList,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = new Point(0, 6),
                Radius = 16,
                Opacity = 0.12f,
            },
        };

        _popupBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 };
    }

    /// <summary>
    /// Attaches the popup Border to the page's root Grid so it floats above everything.
    /// </summary>
    private void AttachPopupToPage()
    {
        if (_popupAttached || _popupBorder == null) return;

        // Walk up the visual tree to find the page.
        var page = GetParentPage();
        if (page is not ContentPage contentPage || contentPage.Content is not Grid rootGrid)
            return;

        // The popup spans all rows/columns so it can be positioned freely via Margin.
        Grid.SetRowSpan(_popupBorder, Math.Max(1, rootGrid.RowDefinitions.Count));
        Grid.SetColumnSpan(_popupBorder, Math.Max(1, rootGrid.ColumnDefinitions.Count));
        _popupBorder.VerticalOptions = LayoutOptions.Start;
        _popupBorder.HorizontalOptions = LayoutOptions.Start;
        _popupBorder.InputTransparent = false;

        rootGrid.Children.Add(_popupBorder);
        _popupAttached = true;
    }

    /// <summary>
    /// Positions the popup directly below this control using absolute coordinates.
    /// </summary>
    private void PositionPopup()
    {
        if (_popupBorder == null) return;

        var page = GetParentPage();
        if (page == null) return;

        // Get the position of this control relative to the page.
        var controlBounds = GetControlBoundsRelativeToPage(page);

        _popupBorder.Margin = new Thickness(
            controlBounds.X,                    // left
            controlBounds.Y + controlBounds.Height + 4,  // top (below the control + 4px gap)
            0, 0);
        _popupBorder.WidthRequest = controlBounds.Width;
    }

    private Rect GetControlBoundsRelativeToPage(Page page)
    {
        // Traverse the visual tree and accumulate offsets.
        double x = 0, y = 0;
        VisualElement? current = this;

        while (current != null && current != page)
        {
            x += current.X;
            y += current.Y;

            if (current.Parent is VisualElement parentVE)
            {
                // Account for Padding on layouts.
                if (parentVE is Layout layout)
                {
                    x += layout.Padding.Left;
                    y += layout.Padding.Top;
                }
                else if (parentVE is Border border)
                {
                    x += border.Padding.Left;
                    y += border.Padding.Top;
                }
                else if (parentVE is ContentPage cp)
                {
                    x += cp.Padding.Left;
                    y += cp.Padding.Top;
                }
                current = parentVE;
            }
            else
            {
                break;
            }
        }

        return new Rect(x, y, this.Width, this.Height);
    }

    private Page? GetParentPage()
    {
        Element? current = this;
        while (current != null)
        {
            if (current is Page page)
                return page;
            current = current.Parent;
        }
        return null;
    }

    private void ApplyFilter(string search)
    {
        var items = ItemsSource
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(item => item.Contains(search, StringComparison.OrdinalIgnoreCase));

        FilteredItems.Clear();
        foreach (var item in items)
            FilteredItems.Add(item);
    }
}
