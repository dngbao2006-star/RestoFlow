using AppManagermentRestaurant.Models;
using Firebase.Database;
using Firebase.Database.Query;

public class FirebaseChatService
{
    private readonly FirebaseClient _firebase;

    public FirebaseChatService()
    {
        _firebase = new FirebaseClient("https://doanquanan-6a948-default-rtdb.firebaseio.com/");
    }

    public async Task SendMessage(ChatMessage msg)
    {
        await _firebase
            .Child("chats")
            .PostAsync(msg);
    }

    public void ListenForMessages(Action<ChatMessage> onMessageReceived)
    {
        _firebase
            .Child("chats")
            .AsObservable<ChatMessage>()
            .Subscribe(d =>
            {
                if (d.Object != null)
                {
                    onMessageReceived(d.Object);
                }
            });
    }
}