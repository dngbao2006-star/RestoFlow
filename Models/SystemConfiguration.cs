using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Models;

public class SystemConfiguration : ObservableObject
{
    private string _restaurantName = "The Golden Plate";
    private string _phone = "0900 999 888";
    private string _email = "hello@goldenplate.vn";
    private string _taxId = "0312345678";
    private string _address = "Quận 1, Thành phố Hồ Chí Minh";
    private string _bankName = "Vietcombank";
    private string _bankBin = "970436";
    private string _accountNumber = "1034973536";
    private string _accountName = "DANG NGUYEN GIA BAO";
    private string _branch = "Bến Thành";
    private string _invoiceFooter = "Cảm ơn quý khách và hẹn gặp lại.";

    public string RestaurantName { get => _restaurantName; set => SetProperty(ref _restaurantName, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string TaxId { get => _taxId; set => SetProperty(ref _taxId, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public string BankName { get => _bankName; set => SetProperty(ref _bankName, value); }
    public string BankBin { get => _bankBin; set => SetProperty(ref _bankBin, value); }
    public string AccountNumber { get => _accountNumber; set => SetProperty(ref _accountNumber, value); }
    public string AccountName { get => _accountName; set => SetProperty(ref _accountName, value); }
    public string Branch { get => _branch; set => SetProperty(ref _branch, value); }
    public string InvoiceFooter { get => _invoiceFooter; set => SetProperty(ref _invoiceFooter, value); }

    public SystemConfiguration Clone() => new()
    {
        RestaurantName = RestaurantName,
        Phone = Phone,
        Email = Email,
        TaxId = TaxId,
        Address = Address,
        BankName = BankName,
        BankBin = BankBin,
        AccountNumber = AccountNumber,
        AccountName = AccountName,
        Branch = Branch,
        InvoiceFooter = InvoiceFooter
    };
}
