using AppManagermentRestaurant.Services;
using CommunityToolkit.Maui.Views;

namespace AppManagermentRestaurant.Controls.Popups;

/// <summary>
/// Popup thông báo — kế thừa CommunityToolkit.Maui.Views.Popup.
/// Render như window-level overlay, không nằm trong luồng layout của trang.
/// </summary>
public partial class NotificationPopup : Popup
{
    public IReadOnlyList<Models.Notification> Notifications { get; }
    public IReadOnlyList<Models.Notification> ImportantNotifications { get; }
    public IReadOnlyList<Models.Notification> NormalNotifications { get; }
    public bool HasNoImportantNotifications => ImportantNotifications.Count == 0;
    public bool HasNoNormalNotifications => NormalNotifications.Count == 0;

    public NotificationPopup()
    {
        InitializeComponent();
        Notifications = AppContext.Instance.VisibleNotifications
            .OrderByDescending(notification => notification.Timestamp)
            .ToList();
        ImportantNotifications = Notifications
            .Where(notification => notification.IsImportant)
            .ToList();
        NormalNotifications = Notifications
            .Where(notification => !notification.IsImportant)
            .ToList();
        BindingContext = this;

        var unread = Notifications
            .Where(notification => !AppContext.Instance.HasCurrentUserRead(notification))
            .ToList();
        AppContext.Instance.MarkNotificationsRead();
        _ = PersistReadStateSafelyAsync(unread);
    }

    private static async Task PersistReadStateSafelyAsync(IReadOnlyCollection<Models.Notification> notifications)
    {
        try
        {
            await new FirebaseService().MarkNotificationsReadAsync(notifications)
                .WaitAsync(TimeSpan.FromSeconds(12));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notifications] Mark read failed safely: {ex.Message}");
        }
    }
}
