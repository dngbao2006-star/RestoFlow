using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using System.Collections.ObjectModel;

namespace AppManagermentRestaurant.Views.Pages;

using System.Linq;

public partial class ChatPage : ContentPage
{
    private readonly FirebaseService firebaseService = new();

    private IDisposable? chatSubscription;

    public ChatPage()
    {
        InitializeComponent();

        BindingContext = this;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        RefreshPage();

        // Tránh subscribe nhiều lần
        chatSubscription?.Dispose();

        chatSubscription = firebaseService.ListenForMessages(message =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Debug
                Console.WriteLine("===== NEW MESSAGE =====");
                Console.WriteLine($"MESSAGE: {message.Message}");
                Console.WriteLine($"SENDER ID: {message.SenderId}");
                Console.WriteLine($"CURRENT USER ID: {AppContext.Instance.CurrentUser?.FirebaseUid}");
                Console.WriteLine($"CURRENT USER NAME: {AppContext.Instance.CurrentUser?.Name}");

                bool alreadyExists = ChatMessages.Any(x =>
                    x.Message == message.Message &&
                    x.Timestamp == message.Timestamp &&
                    x.SenderName == message.SenderName);

                if (alreadyExists)
                    return;


                ChatMessages.Add(new ChatMessage
                {
                    SenderId = message.SenderId,

                    SenderName = message.SenderName,

                    SenderRole = message.SenderRole,

                    Message = message.Message,

                    Timestamp = message.Timestamp,

                    IsSystem = message.IsSystem
                });

                AppContext.Instance.RefreshBadges();

                ScrollToLatestMessage();
            });
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        chatSubscription?.Dispose();
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

            await firebaseService.SendMessageAsync(firebaseMessage);

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