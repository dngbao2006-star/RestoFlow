using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AppManagermentRestaurant.Views.Pages;

using System.Linq;

public partial class ChatPage : ContentPage
{
    private readonly FirebaseService firebaseService = new();

    private bool _isObservingMessages;

    public ChatPage()
    {
        InitializeComponent();

        BindingContext = this;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (!_isObservingMessages)
        {
            ChatMessages.CollectionChanged += OnChatMessagesChanged;
            _isObservingMessages = true;
        }

        AppContext.Instance.MarkChatMessagesRead();
        RefreshPage();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_isObservingMessages)
        {
            ChatMessages.CollectionChanged -= OnChatMessagesChanged;
            _isObservingMessages = false;
        }
    }

    private void OnChatMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AppContext.Instance.MarkChatMessagesRead();
            RefreshPage();
        });
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

    public int OnlineMembersCount =>
        AppContext.Instance.StaffMembers.Count(m => m.IsOnline);

    public ObservableCollection<ChatMessage> ChatMessages =>
        AppContext.Instance.ChatMessages;

    private void OnSendMessageClicked(object sender, EventArgs e)
    {
        SendMessage();
    }

    private void OnMessageTextChanged(object sender, TextChangedEventArgs e)
    {
        SendButton.IsEnabled =
            !string.IsNullOrWhiteSpace(MessageEditor.Text);
    }

    private async void SendMessage()
    {
        try
        {
            var messageText = MessageEditor.Text?.Trim();

            if (string.IsNullOrWhiteSpace(messageText))
            {
                SendButton.IsEnabled = false;
                return;
            }

            Console.WriteLine("===== SEND MESSAGE =====");
            Console.WriteLine($"CURRENT USER NAME: {AppContext.Instance.CurrentUser?.Name}");
            Console.WriteLine($"CURRENT USER ID: {AppContext.Instance.CurrentUser?.FirebaseUid}");

            var firebaseMessage = new FirebaseChatMessage
            {
                SenderId =
                    AppContext.Instance.CurrentUser?.FirebaseUid ?? "",

                SenderName =
                    AppContext.Instance.CurrentUser?.Name
                    ?? "Unknown",

                SenderRole =
                    AppContext.Instance.CurrentUser?.Role
                        .ToString()
                    ?? "Staff",

                Message = messageText,

                Timestamp = DateTime.Now,

                IsSystem = false
            };

            var key = await firebaseService.SendMessageAsync(firebaseMessage);
            AppContext.Instance.AddOrUpdateChatMessage(key, firebaseMessage);
            AppContext.Instance.RefreshBadges();

            MessageEditor.Text = "";

            SendButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SEND MESSAGE ERROR: {ex}");
        }
    }

    private void ScrollToLatestMessage()
    {
        if (ChatMessages.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesList.ScrollTo(
                    ChatMessages.Last(),
                    position: ScrollToPosition.End,
                    animate: true);
            });
        }
    }
}
