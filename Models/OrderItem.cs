using AppManagermentRestaurant.Helpers;
using Microsoft.Maui.Graphics;

namespace AppManagermentRestaurant.Models;

public class OrderItem : ObservableObject
{
    private int _id;
    private int _menuItemId;
    private string _name = string.Empty;
    private decimal _price;
    private int _quantity;
    private string? _notes;
    private DishStatus _status;
    private string _image = string.Empty;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public int MenuItemId
    {
        get => _menuItemId;
        set => SetProperty(ref _menuItemId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(LineTotalDisplay));
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public DishStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    public decimal LineTotal => Price * Quantity;
    public string LineTotalDisplay => Formatters.FormatCurrency(LineTotal);

    public string StatusLabel => Status switch
    {
        DishStatus.Pending => "Chờ xử lý",
        DishStatus.Preparing => "Đang làm",
        DishStatus.Ready => "Sẵn sàng",
        DishStatus.Served => "Đã phục vụ",
        _ => "Không xác định"
    };

    public Color StatusColor => Status switch
    {
        DishStatus.Pending => Color.FromArgb("#F59E0B"),
        DishStatus.Preparing => Color.FromArgb("#4A90D9"),
        DishStatus.Ready => Color.FromArgb("#22C55E"),
        DishStatus.Served => Color.FromArgb("#6B7280"),
        _ => Color.FromArgb("#7B6A57")
    };

    public bool IsReady => Status == DishStatus.Ready;

    public int TableNumberDisplay => 0; // Will be set from Order context
}
