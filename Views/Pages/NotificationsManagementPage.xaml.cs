using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class NotificationsManagementPage : ContentPage
{
    private readonly FirebaseService _firebase = new();
    private bool _isImportant;
    private bool _isSending;
    private bool _isObserving;

    public ObservableCollection<Notification> RecentNotifications { get; private set; } = new();

    public NotificationsManagementPage()
    {
        InitializeComponent();
        BindingContext = this;
        RefreshNotifications();
        UpdateImportantButton();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!_isObserving)
        {
            AppContext.Instance.Notifications.CollectionChanged += OnNotificationsChanged;
            _isObserving = true;
        }
        RefreshNotifications();
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        if (_isObserving)
        {
            AppContext.Instance.Notifications.CollectionChanged -= OnNotificationsChanged;
            _isObserving = false;
        }
        base.OnNavigatingFrom(args);
    }

    private void OnNotificationsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshNotifications();

    private void RefreshNotifications()
    {
        RecentNotifications = new ObservableCollection<Notification>(
            AppContext.Instance.Notifications
                .OrderByDescending(notification => notification.IsImportant)
                .ThenByDescending(notification => notification.Timestamp));
        OnPropertyChanged(nameof(RecentNotifications));
    }

    private void OnImportantClicked(object sender, EventArgs e)
    {
        _isImportant = !_isImportant;
        UpdateImportantButton();
    }

    private void UpdateImportantButton()
    {
        ImportantToggleButton.Text = _isImportant ? "✓ Quan trọng" : "Quan trọng";
        ImportantToggleButton.BackgroundColor = _isImportant
            ? Color.FromArgb("#DC2626")
            : Color.FromArgb("#F5F0E8");
        ImportantToggleButton.TextColor = _isImportant
            ? Colors.White
            : Color.FromArgb("#6B5B4D");
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (_isSending) return;
        var title = TitleEntry.Text?.Trim() ?? string.Empty;
        var message = MessageEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            ValidationLabel.Text = "Vui lòng nhập đầy đủ tiêu đề và nội dung.";
            ValidationLabel.IsVisible = true;
            return;
        }

        _isSending = true;
        SendButton.IsEnabled = false;
        ValidationLabel.IsVisible = false;
        var notificationId = Random.Shared.Next(1, int.MaxValue);
        while (AppContext.Instance.Notifications.Any(item => item.Id == notificationId))
            notificationId = Random.Shared.Next(1, int.MaxValue);

        var notification = new Notification
        {
            Id = notificationId,
            Type = _isImportant ? NotificationType.Danger : NotificationType.Info,
            Title = title,
            Message = message,
            Timestamp = DateTime.Now,
            Read = false,
            Audience = "All"
        };

        try
        {
            await _firebase.CreateNotificationAsync(notification).WaitAsync(TimeSpan.FromSeconds(12));
            if (!AppContext.Instance.Notifications.Any(item => item.Id == notification.Id))
                AppContext.Instance.Notifications.Add(notification);
            TitleEntry.Text = string.Empty;
            MessageEditor.Text = string.Empty;
            _isImportant = false;
            UpdateImportantButton();
            await DisplayAlert("Đã gửi", "Thông báo đã được gửi đến toàn bộ người dùng.", "Đóng");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể gửi thông báo: {ex.Message}", "Đóng");
        }
        finally
        {
            _isSending = false;
            SendButton.IsEnabled = true;
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Notification notification }) return;
        var confirmed = await DisplayAlert("Xóa thông báo", $"Xóa thông báo “{notification.Title}”?", "Xóa", "Hủy");
        if (!confirmed) return;

        try
        {
            await _firebase.DeleteNotificationAsync(notification).WaitAsync(TimeSpan.FromSeconds(12));
            var local = AppContext.Instance.Notifications.FirstOrDefault(item =>
                item.FirebaseKey == notification.FirebaseKey || item.Id == notification.Id);
            if (local != null) AppContext.Instance.Notifications.Remove(local);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể xóa thông báo: {ex.Message}", "Đóng");
        }
    }
}
