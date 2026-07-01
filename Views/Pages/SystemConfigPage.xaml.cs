using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class SystemConfigPage : ContentPage
{
    private readonly FirebaseService _firebase = new();
    private SystemConfiguration _configuration = new();
    private bool _isSaving;

    public SystemConfiguration Configuration
    {
        get => _configuration;
        private set { _configuration = value; OnPropertyChanged(); }
    }

    public SystemConfigPage()
    {
        InitializeComponent();
        BindingContext = this;
        Configuration = AppContext.Instance.SystemConfiguration.Clone();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        Configuration = AppContext.Instance.SystemConfiguration.Clone();
        ValidationLabel.IsVisible = false;
    }

    private async void OnSectionClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string section }) return;
        var target = section switch
        {
            "Payment" => PaymentSection,
            "Invoice" => InvoiceSection,
            _ => RestaurantSection
        };
        await ConfigScroll.ScrollToAsync(target, ScrollToPosition.Start, true);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_isSaving) return;
        if (!TryValidate(out var error))
        {
            ValidationLabel.Text = error;
            ValidationLabel.IsVisible = true;
            return;
        }

        _isSaving = true;
        SaveButton.IsEnabled = false;
        ValidationLabel.IsVisible = false;
        try
        {
            await _firebase.SaveSystemConfigurationAsync(Configuration).WaitAsync(TimeSpan.FromSeconds(12));
            AppContext.Instance.SystemConfiguration = Configuration.Clone();
            await DisplayAlert("Đã lưu", "Cấu hình nhà hàng, hóa đơn và tài khoản VietQR đã được cập nhật.", "Đóng");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể lưu cấu hình: {ex.Message}", "Đóng");
        }
        finally
        {
            _isSaving = false;
            SaveButton.IsEnabled = true;
        }
    }

    private bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Configuration.RestaurantName))
        {
            error = "Tên nhà hàng không được để trống.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Configuration.BankBin) ||
            Configuration.BankBin.Length != 6 ||
            !Configuration.BankBin.All(char.IsDigit))
        {
            error = "Mã BIN ngân hàng phải gồm đúng 6 chữ số.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Configuration.AccountNumber) ||
            string.IsNullOrWhiteSpace(Configuration.AccountName))
        {
            error = "Vui lòng nhập đầy đủ số tài khoản và tên chủ tài khoản.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
