using AppManagermentRestaurant.Helpers;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Models;

public class Order : ObservableObject
{
    private int _id;
    private int _tableId;
    private int _tableNumber;
    private ObservableCollection<OrderItem> _items = new();
    private OrderStatus _status;
    private DateTime _createdAt;
    private string _serverName = string.Empty;
    private string _serverId = string.Empty;
    private decimal _discount;
    private PaymentMethod _paymentMethod;
    private bool _isExpanded;
    private string _discountCode = "";
    private bool _hasPendingEdits;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public int TableId
    {
        get => _tableId;
        set => SetProperty(ref _tableId, value);
    }

    public int TableNumber
    {
        get => _tableNumber;
        set => SetProperty(ref _tableNumber, value);
    }

    public ObservableCollection<OrderItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public OrderStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public string ServerName
    {
        get => _serverName;
        set => SetProperty(ref _serverName, value);
    }

    public string ServerId
    {
        get => _serverId;
        set => SetProperty(ref _serverId, value);
    }

    public decimal Discount
    {
        get => _discount;
        set
        {
            if (SetProperty(ref _discount, value))
            {
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalDisplay));
                OnPropertyChanged(nameof(HasDiscount));
                OnPropertyChanged(nameof(DiscountAmount));
            }
        }
    }

    public PaymentMethod PaymentMethod
    {
        get => _paymentMethod;
        set => SetProperty(ref _paymentMethod, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public string DiscountCode
    {
        get => _discountCode;
        set => SetProperty(ref _discountCode, value);
    }

    public bool HasPendingEdits
    {
        get => _hasPendingEdits;
        set => SetProperty(ref _hasPendingEdits, value);
    }

    public int ItemsCount
    {
        get
        {
            var count = Items.Sum(item => item.Quantity);
            return count;
        }
    }

    public decimal Subtotal
    {
        get => Items.Sum(item => item.LineTotal);
    }

    public decimal Total => Math.Max(0, Subtotal - Discount);

    public string TotalDisplay => Formatters.FormatCurrency(Total);

    public string CreatedAtDisplay => Formatters.FormatDateTime(CreatedAt);

    public string StatusLabel => Status == OrderStatus.Paid ? "Đã thanh toán" : "Đang hoạt động";

    public bool HasDiscount => Discount > 0;

    public string DiscountAmount => Formatters.FormatCurrency(Discount);

    public string DiscountCodeDisplay => !string.IsNullOrEmpty(DiscountCode) ? $"Giảm giá (mã: {DiscountCode})" : "";

    public string TableDisplay => $"Bàn {TableNumber}";

    public string StaffName => ServerName;

    public string PaymentMethodDisplay => PaymentMethod switch
    {
        PaymentMethod.Qr => "💳 QR Code",
        PaymentMethod.Cash => "💵 Tiền mặt",
        _ => "Chưa thanh toán"
    };

    public string PaymentDisplayText => $"Bàn {TableNumber} - {TotalDisplay} ({ItemsCount} món)";

    public void NotifyItemsChanged()
    {
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalDisplay));
        OnPropertyChanged(nameof(TableDisplay));
    }
}
