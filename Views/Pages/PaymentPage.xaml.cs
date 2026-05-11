using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class PaymentPage : ContentPage
{
    private PaymentMethod? _selectedPaymentMethod;
    private Order? _selectedTableOrder;
    private readonly FirebaseService _firebase = new();

    private ObservableCollection<Order> _activeOrders = new();

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
        DiscountCodeEntry.Text = "";
        DiscountErrorLabel.IsVisible = false;

        // 1. Tải bàn CÓ THỂ THANH TOÁN và sắp xếp (Bàn số nhỏ nhất đứng đầu)
        _activeOrders.Clear();
        var billable = AppContext.Instance.Orders
            .Where(o => o.Status == OrderStatus.Active && o.Items.Any())
            .OrderBy(o => o.TableNumber)
            .ToList();

        foreach (var o in billable)
        {
            _activeOrders.Add(o);
        }

        OnPropertyChanged(nameof(HasBillableTables));

        Order targetOrder = null;

        // 2. Xét xem có bàn nào đang được truyền từ trang sơ đồ qua không
        if (AppContext.Instance.SelectedTable != null)
        {
            targetOrder = _activeOrders.FirstOrDefault(o => o.TableId == AppContext.Instance.SelectedTable.Id);
        }

        // 3. TỰ ĐỘNG CHỌN BÀN SỐ NHỎ NHẤT (Nếu không có bàn nào đang chọn)
        if (targetOrder == null && _activeOrders.Any())
        {
            targetOrder = _activeOrders.First(); // Ưu tiên T1, T2...
        }

        // 4. Cập nhật giao diện
        SelectedTableOrder = targetOrder;

        OnPropertyChanged(nameof(IsQRSelected));
        OnPropertyChanged(nameof(IsCashSelected));
        OnPropertyChanged(nameof(CanConfirmPayment));
    }

    public bool HasSelectedOrder => _selectedTableOrder != null;

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
                    AppContext.Instance.SelectedOrder = value;
                    AppContext.Instance.SelectedTable = AppContext.Instance.Tables.FirstOrDefault(t => t.Id == value.TableId);
                }

                OnPropertyChanged(nameof(SelectedTableOrder));
                OnPropertyChanged(nameof(HasSelectedOrder));
                OnPropertyChanged(nameof(CanConfirmPayment));
                OnPropertyChanged(nameof(TransferContent));
                OnPropertyChanged(nameof(DiscountDisplay));
            }
        }
    }

    public ObservableCollection<Order> ActiveOrders => _activeOrders;
    public bool HasBillableTables => _activeOrders.Any();

    public bool IsQRSelected => _selectedPaymentMethod == PaymentMethod.Qr;
    public bool IsCashSelected => _selectedPaymentMethod == PaymentMethod.Cash;
    public bool CanConfirmPayment => _selectedPaymentMethod.HasValue && SelectedTableOrder != null;

    public string TransferContent
    {
        get
        {
            if (SelectedTableOrder == null)
                return "";

            var date = DateTime.Now.ToString("dd/MM/yyyy");
            return $"Nội dung: Thanh toán Bàn {SelectedTableOrder.TableNumber} - {date}";
        }
    }

    public string DiscountDisplay
    {
        get
        {
            if (SelectedTableOrder == null)
                return "0 đ";

            return Formatters.FormatCurrency(SelectedTableOrder.Discount);
        }
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

        if (SelectedTableOrder == null)
            return;

        decimal discountAmount = 0;

        discountAmount = discountCode switch
        {
            "WELCOME10" => SelectedTableOrder.Subtotal * 0.1m,
            "VIP20" => SelectedTableOrder.Subtotal * 0.2m,
            "SAVE50K" => 50000m,
            _ => -1
        };

        if (discountAmount < 0)
        {
            DiscountErrorLabel.Text = "Mã giảm giá không hợp lệ";
            DiscountErrorLabel.IsVisible = true;
            return;
        }

        SelectedTableOrder.Discount = (int)discountAmount;
        DiscountErrorLabel.IsVisible = false;
        DiscountCodeEntry.Text = "";

        OnPropertyChanged(nameof(SelectedTableOrder));
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

        if (SelectedTableOrder == null) return;

        var order = SelectedTableOrder;

        order.Status = OrderStatus.Paid;
        order.PaymentMethod = _selectedPaymentMethod.Value;

        var table = AppContext.Instance.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
        if (table != null)
        {
            table.Status = TableStatus.NeedsClearing;
            table.CurrentOrderId = null;
            table.HasOrdered = false;
            table.OrderTotal = string.Empty;
            table.OrderItemCount = 0;

            ActivityLogService.Instance.LogPayment(table.Number.ToString());
            _ = _firebase.UpdateTableAsync(table);
        }
        _ = _firebase.UpdateOrderStatusAsync(order);

        AppContext.Instance.SelectedOrder = null;
        AppContext.Instance.SelectedTable = null;

        await DisplayAlert("Thành công", $"✓ Đã thanh toán Bàn {order.TableNumber}\nSố tiền: {order.TotalDisplay}", "OK");

        RefreshPage();
        AppContext.Instance.RefreshBadges();
    }
}