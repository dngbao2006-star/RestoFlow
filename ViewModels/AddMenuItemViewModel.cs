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
    private bool _isServing = true;
    private bool _isLoading;
    private FoodItem? _editingItem;

    // Validation error fields
    private string _nameError = string.Empty;
    private string _categoryError = string.Empty;
    private string _priceError = string.Empty;
    private bool _hasNameError;
    private bool _hasCategoryError;
    private bool _hasPriceError;
    private string _formError = string.Empty;
    private bool _hasFormError;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                // Xóa lỗi khi user bắt đầu nhập
                if (!string.IsNullOrWhiteSpace(value) && HasNameError)
                {
                    HasNameError = false;
                    NameError = string.Empty;
                }
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            if (SetProperty(ref _category, value))
            {
                // Xóa lỗi khi user chọn danh mục
                if (!string.IsNullOrWhiteSpace(value) && HasCategoryError)
                {
                    HasCategoryError = false;
                    CategoryError = string.Empty;
                }
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
            {
                // Xóa lỗi khi user nhập giá hợp lệ
                if (value >= 1000 && HasPriceError)
                {
                    HasPriceError = false;
                    PriceError = string.Empty;
                }
                OnPropertyChanged(nameof(CanSave));
            }
        }
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

    /// <summary>
    /// Toggle đơn: true = "Đang phục vụ", false = "Hết món".
    /// </summary>
    public bool IsServing
    {
        get => _isServing;
        set
        {
            if (SetProperty(ref _isServing, value))
            {
                OnPropertyChanged(nameof(StatusLabel));
            }
        }
    }

    public string StatusLabel => IsServing ? "Đang phục vụ" : "Hết món";

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
                OnPropertyChanged(nameof(CanSave));
        }
    }

    public FoodItem? EditingItem
    {
        get => _editingItem;
        set => SetProperty(ref _editingItem, value);
    }

    public bool IsEditing => EditingItem != null;

    // === Validation Properties ===

    public string NameError
    {
        get => _nameError;
        set => SetProperty(ref _nameError, value);
    }

    public bool HasNameError
    {
        get => _hasNameError;
        set
        {
            if (SetProperty(ref _hasNameError, value))
                OnPropertyChanged(nameof(CanSave));
        }
    }

    public string CategoryError
    {
        get => _categoryError;
        set => SetProperty(ref _categoryError, value);
    }

    public bool HasCategoryError
    {
        get => _hasCategoryError;
        set
        {
            if (SetProperty(ref _hasCategoryError, value))
                OnPropertyChanged(nameof(CanSave));
        }
    }

    public string PriceError
    {
        get => _priceError;
        set => SetProperty(ref _priceError, value);
    }

    public bool HasPriceError
    {
        get => _hasPriceError;
        set
        {
            if (SetProperty(ref _hasPriceError, value))
                OnPropertyChanged(nameof(CanSave));
        }
    }

    public string FormError
    {
        get => _formError;
        set => SetProperty(ref _formError, value);
    }

    public bool HasFormError
    {
        get => _hasFormError;
        set => SetProperty(ref _hasFormError, value);
    }

    /// <summary>
    /// Nút Lưu chỉ được bật khi không có lỗi validation và không đang loading.
    /// </summary>
    public bool CanSave => !HasNameError && !HasCategoryError && !HasPriceError && !IsLoading;

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

    // === On-blur Validation Methods ===

    public void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            HasNameError = true;
            NameError = "Tên món ăn không được để trống.";
        }
        else
        {
            HasNameError = false;
            NameError = string.Empty;
        }
    }

    public void ValidatePrice()
    {
        if (Price < 1000)
        {
            HasPriceError = true;
            PriceError = "Giá tiền phải từ 1,000 VNĐ trở lên.";
        }
        else
        {
            HasPriceError = false;
            PriceError = string.Empty;
        }
    }

    public void ValidateCategory()
    {
        if (string.IsNullOrWhiteSpace(Category))
        {
            HasCategoryError = true;
            CategoryError = "Vui lòng chọn ít nhất 1 danh mục.";
        }
        else
        {
            HasCategoryError = false;
            CategoryError = string.Empty;
        }
    }

    /// <summary>
    /// Validate toàn bộ form khi nhấn Lưu.
    /// Trả về true nếu hợp lệ, false nếu có lỗi.
    /// </summary>
    public bool ValidateAll()
    {
        ValidateName();
        ValidatePrice();
        ValidateCategory();

        bool hasAnyError = HasNameError || HasCategoryError || HasPriceError;

        if (hasAnyError)
        {
            HasFormError = true;
            FormError = "Vui lòng nhập đầy đủ thông tin cho món ăn.";
        }
        else
        {
            HasFormError = false;
            FormError = string.Empty;
        }

        return !hasAnyError;
    }

    public async Task SaveMenuItemAsync()
    {
        // On-submit validation
        if (!ValidateAll())
        {
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
                Available = IsServing,
                OutOfStock = !IsServing
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
        IsServing = item.Available && !item.OutOfStock;

        // Force refresh UI bindings
        OnPropertyChanged(nameof(EditingItem));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Category));
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ImageUrl));
        OnPropertyChanged(nameof(IsServing));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(CanSave));
    }

    private int GenerateNewId()
    {
        return AppContext.Instance.MenuItems.Count > 0 
            ? AppContext.Instance.MenuItems.Max(m => m.Id) + 1 
            : 1;
    }
}
