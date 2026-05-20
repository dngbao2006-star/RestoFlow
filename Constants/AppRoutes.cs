namespace AppManagermentRestaurant.Constants;

public static class AppRoutes
{
    public const string ForgotPassword = "quen-mat-khau";
    public const string Profile = "trang-ca-nhan";

    public const string TableMap = "so-do-ban";
    public const string CreateOrder = "tao-don";
    public const string DishStatus = "trang-thai-mon";
    public const string Payment = "thanh-toan";
    public const string OrderHistory = "lich-su-don";
    public const string Account = "tai-khoan";
    public const string Dashboard = "tong-quan";
    public const string HR = "nhan-su";
    public const string Menu = "thuc-don";
    public const string Layout = "so-do-khu-vuc";
    public const string Revenue = "doanh-thu";
    public const string Invoice = "hoa-don";
    public const string Notifications = "thong-bao";
    public const string SystemConfig = "cau-hinh";
    public const string StaffChat = "staff-chat";
    public const string OrderManagement = "quan-ly-don-hang";
    public const string ManagementChat = "management-chat";

    public static string Absolute(string route) => $"//{route}";
}
