using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.Views.Pages;

public partial class ChatPage : ContentPage
{
    
    private FirebaseChatService _chatService = new FirebaseChatService();

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        _chatService.ListenForMessages((msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // tránh duplicate
                if (!AppContext.Instance.ChatMessages.Any(x => x.Id == msg.Id))
                {
                    msg.IsMine = msg.SenderId == AppContext.Instance.CurrentUser?.Id;

                    AppContext.Instance.ChatMessages.Add(msg);
                    ScrollToLatestMessage();
                }
            });
        });

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
    }

    private async void SendMessage()
    {
        var message = MessageEditor.Text?.Trim();

        if (string.IsNullOrEmpty(message))
        {
            SendButton.IsEnabled = false;
            return;
        }

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

        await _chatService.SendMessage(chatMessage);

        MessageEditor.Text = "";
        SendButton.IsEnabled = false;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        SendButton.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
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