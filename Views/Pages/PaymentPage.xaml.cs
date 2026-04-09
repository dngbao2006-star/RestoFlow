using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class PaymentPage : ContentPage
{
    private PaymentMethod? _selectedPaymentMethod;
    private Order? _selectedTableOrder;
    private HashSet<int> _selectedTableIds = new(); // Track multiple selected tables

    public PaymentPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        RefreshPage();
    }

    private void RefreshPage()
    {
        _selectedPaymentMethod = null;
        _selectedTableOrder = null;
        _selectedTableIds.Clear();
        DiscountCodeEntry.Text = "";
        DiscountErrorLabel.IsVisible = false;
        OnPropertyChanged(nameof(HasSelectedOrder));
        OnPropertyChanged(nameof(SelectedTableOrder));
        OnPropertyChanged(nameof(CanConfirmPayment));
        OnPropertyChanged(nameof(OrdersWithServedItems));
        OnPropertyChanged(nameof(SelectedTablesCount));
        OnPropertyChanged(nameof(TotalAmountForSelectedTables));
    }

    public bool HasSelectedOrder => AppContext.Instance.SelectedOrder != null;

    public Order? SelectedTableOrder
    {
        get => _selectedTableOrder;
        set
        {
            if (_selectedTableOrder != value)
            {
                _selectedTableOrder = value;
                if (value != null)
                {
                    SelectOrder(value);
                    // Add this table to selected tables
                    _selectedTableIds.Add(value.Id);
                }
                OnPropertyChanged(nameof(SelectedTableOrder));
                OnPropertyChanged(nameof(SelectedTablesCount));
                OnPropertyChanged(nameof(TotalAmountForSelectedTables));
            }
        }
    }

    public IEnumerable<Order> ActiveOrders => AppContext.Instance.Orders.Where(o => o.Status == OrderStatus.Active);

    public IEnumerable<Order> OrdersWithServedItems => AppContext.Instance.Orders.Where(o => 
        o.Status == OrderStatus.Active && 
        o.Items.Any() &&
        o.Items.All(item => item.Status == DishStatus.Served)).ToList();

    public bool IsQRSelected => _selectedPaymentMethod == PaymentMethod.Qr;

    public bool IsCashSelected => _selectedPaymentMethod == PaymentMethod.Cash;

    public bool CanConfirmPayment => _selectedPaymentMethod.HasValue && AppContext.Instance.SelectedOrder != null;

    public int SelectedTablesCount => _selectedTableIds.Count;

    public string TotalAmountForSelectedTables
    {
        get
        {
            var total = AppContext.Instance.Orders
                .Where(o => _selectedTableIds.Contains(o.Id) && o.Status == OrderStatus.Active)
                .Sum(o => o.Total);
            return Formatters.FormatCurrency(total);
        }
    }

    public string TransferContent 
    { 
        get
        {
            if (AppContext.Instance.SelectedOrder == null)
                return "";

            var date = DateTime.Now.ToString("dd/MM/yyyy");
            if (_selectedTableIds.Count > 1)
                return $"Nội dung: Thanh toán {_selectedTableIds.Count} bàn - {date}";
            return $"Nội dung: Thanh toán Bàn {AppContext.Instance.SelectedOrder.TableNumber} - {date}";
        }
    }

    public string DiscountDisplay
    {
        get
        {
            if (AppContext.Instance.SelectedOrder == null)
                return "0 VND";

            return Formatters.FormatCurrency(AppContext.Instance.SelectedOrder.Discount);
        }
    }

    private void SelectOrder(Order order)
    {
        if (order != null)
        {
            AppContext.Instance.SelectedOrder = order;
            OnPropertyChanged(nameof(HasSelectedOrder));
            OnPropertyChanged(nameof(CanConfirmPayment));
            OnPropertyChanged(nameof(TransferContent));
            OnPropertyChanged(nameof(DiscountDisplay));
        }
    }

    private void OnTableItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is Order order)
        {
            SelectOrder(order);
        }
    }

    private void OnTableButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Order order)
        {
            SelectOrder(order);
            UpdateTableButtonStyles();
        }
    }

    private void UpdateTableButtonStyles()
    {
        // This method updates visual feedback for selected table
        // The XAML will handle the styling through bindings
        OnPropertyChanged(nameof(HasSelectedOrder));
    }

    private void OnTableSelected(object sender, EventArgs e)
    {
        // This event handler is triggered when a table is selected from the Picker
        // The SelectedItem binding will automatically update SelectedTableOrder property
        OnPropertyChanged(nameof(HasSelectedOrder));
        OnPropertyChanged(nameof(CanConfirmPayment));
        OnPropertyChanged(nameof(TransferContent));
        OnPropertyChanged(nameof(DiscountDisplay));
    }

    private void OnPaymentMethodTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string method)
            return;

        _selectedPaymentMethod = method switch
        {
            "QR" => PaymentMethod.Qr,
            "Cash" => PaymentMethod.Cash,
            _ => null
        };

        OnPropertyChanged(nameof(IsQRSelected));
        OnPropertyChanged(nameof(IsCashSelected));
        OnPropertyChanged(nameof(CanConfirmPayment));
    }

    private void OnApplyDiscountClicked(object sender, EventArgs e)
    {
        var discountCode = DiscountCodeEntry.Text?.ToUpper().Trim();

        if (string.IsNullOrEmpty(discountCode))
        {
            DiscountErrorLabel.Text = "Vui lòng nhập mã giảm giá";
            DiscountErrorLabel.IsVisible = true;
            return;
        }

        var order = AppContext.Instance.SelectedOrder;
        if (order == null)
            return;

        decimal discountAmount = 0;

        // Validate and apply discount
        discountAmount = discountCode switch
        {
            "WELCOME10" => order.Subtotal * 0.1m, // 10%
            "VIP20" => order.Subtotal * 0.2m,     // 20%
            "SAVE50K" => 50000m,                   // Fixed 50k
            _ => -1
        };

        if (discountAmount < 0)
        {
            DiscountErrorLabel.Text = "Mã giảm giá không hợp lệ";
            DiscountErrorLabel.IsVisible = true;
            return;
        }

        // Apply discount
        order.Discount = (int)discountAmount;
        DiscountErrorLabel.IsVisible = false;
        DiscountCodeEntry.Text = "";

        OnPropertyChanged(nameof(DiscountDisplay));

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Thành công", $"Áp dụng mã giảm giá thành công: {Formatters.FormatCurrency(discountAmount)}", "OK");
        });
    }

    private async void OnConfirmPaymentClicked(object sender, EventArgs e)
    {
        if (!_selectedPaymentMethod.HasValue)
        {
            await DisplayAlert("Lỗi", "Vui lòng chọn phương thức thanh toán", "OK");
            return;
        }

        if (_selectedTableIds.Count == 0)
        {
            await DisplayAlert("Lỗi", "Vui lòng chọn ít nhất một bàn để thanh toán", "OK");
            return;
        }

        // Process payment for all selected tables
        var ordersToProcess = AppContext.Instance.Orders
            .Where(o => _selectedTableIds.Contains(o.Id) && o.Status == OrderStatus.Active)
            .ToList();

        foreach (var order in ordersToProcess)
        {
            // Update order status
            order.Status = OrderStatus.Paid;
            order.PaymentMethod = _selectedPaymentMethod.Value;

            // Update table status
            var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
            if (table != null)
            {
                table.Status = TableStatus.NeedsClearing;
                table.CurrentOrderId = null;

                // Log payment activity for each table
                ActivityLogService.Instance.LogPayment(table.Number.ToString());
            }
        }

        // Show success message
        var message = _selectedTableIds.Count > 1 
            ? $"✓ Đã thanh toán {_selectedTableIds.Count} bàn\nTổng: {TotalAmountForSelectedTables}"
            : $"✓ Đã thanh toán Bàn {_selectedTableOrder?.TableNumber}\nSố tiền: {_selectedTableOrder?.TotalDisplay}";

        await DisplayAlert("Thành công", message, "OK");

        // Refresh the page
        RefreshPage();
        OnPropertyChanged(nameof(OrdersWithServedItems));
        AppContext.Instance.RefreshBadges();
    }
}
