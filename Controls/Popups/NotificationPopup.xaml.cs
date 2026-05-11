using AppManagermentRestaurant.Services;
using CommunityToolkit.Maui.Views;

namespace AppManagermentRestaurant.Controls.Popups;

/// <summary>
/// Popup thông báo — kế thừa CommunityToolkit.Maui.Views.Popup.
/// Render như window-level overlay, không nằm trong luồng layout của trang.
/// </summary>
public partial class NotificationPopup : Popup
{
    public NotificationPopup()
    {
        InitializeComponent();
        // Bind trực tiếp vào AppContext để lấy danh sách thông báo thực
        // TODO [BACKEND]: Thay AppContext.Instance bằng dữ liệu từ API khi kết nối backend
        BindingContext = AppContext.Instance;

        // Đánh dấu đã đọc khi mở popup
        AppContext.Instance.MarkNotificationsRead();
    }
}
