using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Models;

public class FoodItem : ObservableObject
{
    private int _id;
    private string _name = string.Empty;
    private string _category = string.Empty;
    private decimal _price;
    private string _description = string.Empty;
    private string _image = string.Empty;
    private bool _available = true;
    private bool _outOfStock;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    public bool Available
    {
        get => _available;
        set => SetProperty(ref _available, value);
    }

    public bool OutOfStock
    {
        get => _outOfStock;
        set => SetProperty(ref _outOfStock, value);
    }
}
