using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Controls;

public partial class SidebarHeaderView : ContentView
{
    // ── IsExpanded ──────────────────────────────────────────────────
    public static readonly BindableProperty IsExpandedProperty =
        BindableProperty.Create(nameof(IsExpanded), typeof(bool),
            typeof(SidebarHeaderView), true);

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    // ── RoleSubtitle (derived from AppContext) ──────────────────────
    public string RoleSubtitle
    {
        get
        {
            try
            {
                return AppContext.Instance.IsManager ? "Cổng quản lý" : "Cổng nhân viên";
            }
            catch { return "Cổng điều hành"; }
        }
    }

    public SidebarHeaderView()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }

    private void OnToggleSidebarTapped(object sender, TappedEventArgs e)
    {
        if (Application.Current?.MainPage is AppShell shell)
            shell.IsSidebarExpanded = !shell.IsSidebarExpanded;
    }
}
