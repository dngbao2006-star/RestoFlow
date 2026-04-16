using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class ChatPage : ContentPage
{
    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        RefreshPage();
    }

    private void RefreshPage()
    {
        OnPropertyChanged(nameof(ChatTitle));
        OnPropertyChanged(nameof(OnlineMembersCount));
        ScrollToLatestMessage();
    }

    public string ChatTitle
    {
        get
        {
            var unreadCount = AppContext.Instance.UnreadMessages;
            if (unreadCount > 0)
                return $"Chat ({unreadCount})";
            return "Chat";
        }
    }

    public int OnlineMembersCount => AppContext.Instance.StaffMembers.Count(m => m.Status == StaffStatus.Active);

    public IEnumerable<ChatMessage> ChatMessages => AppContext.Instance.ChatMessages;

    private void OnSendMessageClicked(object sender, EventArgs e)
    {
        SendMessage();
    }

    private async void OnMessageEditorCompleted(object sender, EventArgs e)
    {
        if (sender is Editor editor)
        {
            // Check if Shift+Enter was pressed (new line)
            // Otherwise, send message on Enter
            // Note: This is a simplified version - MAUI doesn't have built-in Shift+Enter detection in Editor
            // For full implementation, you may need a custom control or KeyDown event
        }
    }

    private void SendMessage()
    {
        var message = MessageEditor.Text?.Trim();

        if (string.IsNullOrEmpty(message))
        {
            SendButton.IsEnabled = false;
            return;
        }

        // Create new chat message
        var chatMessage = new ChatMessage
        {
            Id = Guid.NewGuid().GetHashCode(),
            SenderId = AppContext.Instance.CurrentUser?.Id ?? 0,
            SenderName = AppContext.Instance.CurrentUser?.Name ?? "Unknown",
            SenderRole = AppContext.Instance.CurrentUser?.Role.ToString() ?? "Staff",
            Message = message,
            Timestamp = DateTime.Now,
            IsRead = true,
            IsMine = true,
            IsSystem = false
        };

        // Add to messages
        AppContext.Instance.ChatMessages.Add(chatMessage);

        // Clear input
        MessageEditor.Text = "";
        SendButton.IsEnabled = false;

        // Scroll to latest
        ScrollToLatestMessage();

        // Refresh UI
        RefreshPage();
    }

    private void ScrollToLatestMessage()
    {
        if (ChatMessages.Count() > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesList.ScrollTo(ChatMessages.Last(), position: ScrollToPosition.End, animate: true);
            });
        }
    }
}
