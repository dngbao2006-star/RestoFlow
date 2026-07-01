using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using AppManagermentRestaurant.Constants;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.ViewModels;

/// <summary>
/// ViewModel cho AppHeaderView và các Popup con.
///
/// THAY ĐỔI SO VỚI PHIÊN BẢN CŨ:
/// ─ Đã xoá các property toggle: NotifOpen, ChatOpen, ProfileOpen,
///   IsAnyDropdownOpen cùng các phương thức ToggleNotifications/Chat/Profile.
/// ─ Lý do: Popup nay được mở bằng ShowPopupAsync() từ code-behind, không
///   cần state boolean để điều khiển IsVisible inline nữa.
/// ─ Giữ nguyên: TodayLabel, chat send logic, derived properties, commands.
/// </summary>
public class HeaderViewModel : ObservableObject
{
    private string _chatInput = string.Empty;
    private bool _isSendingChat;
    private readonly FirebaseService _firebaseService = new();
    private Command? _sendChatCommand;

    public HeaderViewModel(AppContext appContext)
    {
        AppContext = appContext;

        var vietnamCulture = new CultureInfo("vi-VN");
        TodayLabel = DateTime.Now.ToString("dddd, dd/MM/yyyy", vietnamCulture);

        AppContext.PropertyChanged += OnAppContextPropertyChanged;

        // Commands
        _sendChatCommand = new Command(async () => await SendChatMessageAsync(), () => CanSendChat);
        SendChatMessageCommand = _sendChatCommand;
        OpenExpandedChatCommand = new Command(async () => await OpenExpandedChatAsync());
        OpenAccountCommand = new Command(async () => await OpenAccountAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
    }

    // ─── Dữ liệu ─────────────────────────────────────────────────────────

    public AppContext AppContext { get; }
    public string TodayLabel { get; }
    public ObservableCollection<Notification> Notifications => AppContext.Notifications;
    public ObservableCollection<ChatMessage> ChatMessages => AppContext.ChatMessages;

    // ─── Badge / trạng thái ───────────────────────────────────────────────

    public bool HasUnreadNotifications => AppContext.UnreadNotifications > 0;
    public bool HasUnreadMessages => AppContext.UnreadMessages > 0;
    public int OnlineCount => AppContext.StaffMembers.Count(m => m.IsOnline);

    // ─── Chat input ───────────────────────────────────────────────────────

    public string ChatInput
    {
        get => _chatInput;
        set
        {
            if (SetProperty(ref _chatInput, value))
            {
                OnPropertyChanged(nameof(CanSendChat));
                _sendChatCommand?.ChangeCanExecute();
            }
        }
    }

    public bool CanSendChat =>
        !_isSendingChat &&
        AppContext.CurrentUser != null &&
        !string.IsNullOrWhiteSpace(ChatInput);

    // ─── Thông tin user hiện tại ──────────────────────────────────────────

    public string CurrentUserName => AppContext.CurrentUser?.Name ?? "Tài khoản";
    public string CurrentUserEmail => AppContext.CurrentUser?.Email ?? string.Empty;

    public string CurrentUserDisplayName
    {
        get
        {
            var parts = CurrentUserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? $"{parts[^2]} {parts[^1]}" : CurrentUserName;
        }
    }

    public string CurrentUserInitial
        => string.IsNullOrWhiteSpace(CurrentUserName)
            ? "?"
            : CurrentUserName.Trim()[0].ToString().ToUpperInvariant();

    public string CurrentUserAvatarColor
        => AppContext.IsManager ? "#1B3A6B" : "#22C55E";

    // ─── Commands ─────────────────────────────────────────────────────────

    public ICommand SendChatMessageCommand { get; }
    public ICommand OpenExpandedChatCommand { get; }
    public ICommand OpenAccountCommand { get; }
    public ICommand LogoutCommand { get; }

    /// <summary>
    /// Event thông báo cho ChatPopup biết có tin nhắn mới để scroll.
    /// </summary>
    public event Action? ChatMessageSent;

    // ─── Xử lý gửi tin nhắn ──────────────────────────────────────────────

    private async Task SendChatMessageAsync()
    {
        var message = ChatInput?.Trim();
        var user = AppContext.CurrentUser;
        if (string.IsNullOrWhiteSpace(message) || user == null || _isSendingChat) return;

        var firebaseMessage = new FirebaseChatMessage
        {
            SenderId = user.FirebaseUid,
            SenderName = user.Name,
            SenderRole = user.Role.ToString(),
            Message = message,
            Timestamp = DateTime.Now,
            IsSystem = false
        };

        try
        {
            _isSendingChat = true;
            OnPropertyChanged(nameof(CanSendChat));
            _sendChatCommand?.ChangeCanExecute();

            var key = await _firebaseService.SendMessageAsync(firebaseMessage);
            AppContext.AddOrUpdateChatMessage(key, firebaseMessage);
            AppContext.RefreshBadges();
            ChatInput = string.Empty;
            ChatMessageSent?.Invoke();
        }
        catch
        {
            if (Application.Current?.MainPage is Page page)
                await page.DisplayAlert("Lỗi", "Không thể gửi tin nhắn. Vui lòng kiểm tra kết nối.", "OK");
        }
        finally
        {
            _isSendingChat = false;
            OnPropertyChanged(nameof(CanSendChat));
            _sendChatCommand?.ChangeCanExecute();
        }
    }

    private async Task OpenExpandedChatAsync()
    {
        // TODO [BACKEND]: Route đúng theo vai trò
        var route = AppContext.IsManager ? AppRoutes.ManagementChat : AppRoutes.StaffChat;
        await Shell.Current.GoToAsync(AppRoutes.Absolute(route));
    }

    private async Task OpenAccountAsync()
    {
        await Shell.Current.GoToAsync(AppRoutes.Absolute(AppRoutes.Account));
    }

    private async Task LogoutAsync()
    {
        var currentUser = AppContext.CurrentUser;

        // Hủy listener xung đột phiên trước khi logout
        AppContext.SessionConflictSubscription?.Dispose();
        AppContext.SessionConflictSubscription = null;
        AppContext.CurrentSessionId = null;

        if (currentUser != null)
        {
            try
            {
                await _firebaseService.SetUserOfflineAsync(currentUser.FirebaseUid, currentUser.Name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logout] Could not update presence: {ex.Message}");
            }
        }

        ActivityLogService.Instance.LogLogout();
        if (Application.Current?.MainPage is AppShell shell)
            shell.BindingContext = null;

        await MainThread.InvokeOnMainThreadAsync(App.ShowLogin);
        AppContext.CurrentUser = null;
    }

    // ─── Refresh ─────────────────────────────────────────────────────────

    private void OnAppContextPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppContext.UnreadNotifications):
                OnPropertyChanged(nameof(HasUnreadNotifications));
                break;
            case nameof(AppContext.UnreadMessages):
                OnPropertyChanged(nameof(HasUnreadMessages));
                break;
            case nameof(AppContext.OnlineStaffCount):
                OnPropertyChanged(nameof(OnlineCount));
                break;
            case nameof(AppContext.CurrentUser):
            case nameof(AppContext.IsManager):
            case nameof(AppContext.IsStaff):
                OnPropertyChanged(nameof(CurrentUserName));
                OnPropertyChanged(nameof(CurrentUserEmail));
                OnPropertyChanged(nameof(CurrentUserDisplayName));
                OnPropertyChanged(nameof(CurrentUserInitial));
                OnPropertyChanged(nameof(CurrentUserAvatarColor));
                OnPropertyChanged(nameof(CanSendChat));
                _sendChatCommand?.ChangeCanExecute();
                break;
        }
    }
}
