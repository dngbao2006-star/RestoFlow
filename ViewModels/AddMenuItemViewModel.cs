using System.Diagnostics;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.ViewModels;

public class AddMenuItemViewModel : BaseViewModel
{
    private readonly FirebaseService _firebaseService = new();
    private int _id;
    private string _name = string.Empty;
    private string _category = string.Empty;
    private decimal _price;
    private string _description = string.Empty;
    private string _imageUrl = string.Empty;
    private bool _available = true;
    private bool _outOfStock;
    private bool _isLoading;
    private FoodItem? _editingItem;

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

    public string ImageUrl
    {
        get => _imageUrl;
        set => SetProperty(ref _imageUrl, value);
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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public FoodItem? EditingItem
    {
        get => _editingItem;
        set => SetProperty(ref _editingItem, value);
    }

    public List<string> Categories { get; } = new()
    {
        "Khai vị",
        "Món chính",
        "Canh/Súp",
        "Hải sản",
        "Tráng miệng",
        "Đồ uống"
    };

    public Command<string> SelectCategoryCommand { get; }

    public AddMenuItemViewModel()
    {
        SelectCategoryCommand = new Command<string>(category => Category = category);
    }

    public async Task SaveMenuItemAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || Price <= 0 || string.IsNullOrWhiteSpace(Category))
        {
            await Application.Current?.MainPage?.DisplayAlert("Lỗi", "Vui lòng điền đầy đủ thông tin", "OK")!;
            return;
        }

        try
        {
            IsLoading = true;

            var menuItem = new FoodItem
            {
                Id = EditingItem?.Id ?? GenerateNewId(),
                Name = Name,
                Category = Category,
                Price = Price,
                Description = Description,
                Image = ImageUrl,
                Available = Available,
                OutOfStock = OutOfStock
            };

            // Save to Firebase
            await _firebaseService.SaveMenuItemAsync(menuItem);

            // Update AppContext
            if (EditingItem != null)
            {
                // Update existing
                var existingItem = AppContext.Instance.MenuItems.FirstOrDefault(m => m.Id == menuItem.Id);
                if (existingItem != null)
                {
                    existingItem.Name = menuItem.Name;
                    existingItem.Category = menuItem.Category;
                    existingItem.Price = menuItem.Price;
                    existingItem.Description = menuItem.Description;
                    existingItem.Image = menuItem.Image;
                    existingItem.Available = menuItem.Available;
                    existingItem.OutOfStock = menuItem.OutOfStock;
                }
            }
            else
            {
                // Add new
                AppContext.Instance.MenuItems.Add(menuItem);
            }

            await Application.Current?.MainPage?.DisplayAlert("Thành công", "Lưu món ăn thành công!", "OK")!;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving menu item: {ex.Message}");
            await Application.Current?.MainPage?.DisplayAlert("Lỗi", $"Lỗi lưu món ăn: {ex.Message}", "OK")!;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void LoadMenuItem(FoodItem item)
    {
        EditingItem = item;
        Id = item.Id;
        Name = item.Name;
        Category = item.Category;
        Price = item.Price;
        Description = item.Description;
        ImageUrl = item.Image;
        Available = item.Available;
        OutOfStock = item.OutOfStock;

        // Force refresh UI bindings
        OnPropertyChanged(nameof(EditingItem));
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Category));
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ImageUrl));
        OnPropertyChanged(nameof(Available));
        OnPropertyChanged(nameof(OutOfStock));
    }

    private int GenerateNewId()
    {
        return AppContext.Instance.MenuItems.Count > 0 
            ? AppContext.Instance.MenuItems.Max(m => m.Id) + 1 
            : 1;
    }
}
