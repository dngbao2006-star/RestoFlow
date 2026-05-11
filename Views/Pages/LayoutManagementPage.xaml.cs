using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.Constants;

namespace AppManagermentRestaurant.Views.Pages;

public partial class LayoutManagementPage : ContentPage
{
    public LayoutManagementPage()
    {
        InitializeComponent();
        BindingContext = AppContext.Instance;
    }

    private async void OnTableTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Table table)
            return;

        await NavigateToTable(table);
    }

    private async void OnTableDetailsTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Table table)
            return;

        await NavigateToTable(table);
    }

    private async Task NavigateToTable(Table table)
    {
        // Set the selected order for the table
        var order = AppContext.Instance.Orders.FirstOrDefault(o => o.Id == table.CurrentOrderId);

        if (table.Status == TableStatus.Available)
        {
            // Create a new order for available table
            AppContext.Instance.SelectedOrder = new Order
            {
                Id = Guid.NewGuid().GetHashCode(),
                TableNumber = table.Number,
                Items = new System.Collections.ObjectModel.ObservableCollection<OrderItem>(),
                Status = OrderStatus.Active,
                CreatedAt = DateTime.Now,
                ServerName = AppContext.Instance.CurrentUser?.Name ?? "Unknown"
            };
        }
        else
        {
            // Load existing order for occupied/reserved tables
            AppContext.Instance.SelectedOrder = order;
        }

        // Navigate to OrderCreationPage
        await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.CreateOrder));
    }
}
