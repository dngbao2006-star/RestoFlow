using AppManagermentRestaurant.ViewModels;
using CommunityToolkit.Maui.Views;
using System.Collections.Specialized;

namespace AppManagermentRestaurant.Controls.Popups;

/// <summary>
/// Popup chat nội bộ — kế thừa toolkit:Popup, không ảnh hưởng layout.
/// Nhận HeaderViewModel từ AppHeaderView để tái sử dụng logic gửi tin nhắn.
/// </summary>
public partial class ChatPopup : Popup
{
    private readonly HeaderViewModel _vm;

    public ChatPopup(HeaderViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;

        // Lắng nghe tin nhắn mới để tự động scroll
        _vm.AppContext.ChatMessages.CollectionChanged += OnMessagesChanged;
        _vm.ChatMessageSent += ScrollToLatest;

        // Scroll đến tin mới nhất khi mở popup
        this.Opened += (_, _) => ScrollToLatest();
    }

    // ─── Gửi tin nhắn qua Entry.Completed (bấm Enter) ────────────────────
    private void OnChatEntryCompleted(object sender, EventArgs e)
    {
        if (_vm.CanSendChat)
            _vm.SendChatMessageCommand.Execute(null);
    }

    // ─── Gửi tin nhắn qua nút ➤ ──────────────────────────────────────────
    private void OnSendClicked(object sender, EventArgs e)
    {
        // Command đã xử lý việc gửi; chỉ cần scroll
        ScrollToLatest();
    }

    // ─── Scroll CollectionView đến tin nhắn cuối ─────────────────────────
    private void ScrollToLatest()
    {
        var messages = _vm.ChatMessages;
        if (messages.Count == 0) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PopupChatList?.ScrollTo(
                item: messages[^1],
                position: ScrollToPosition.End,
                animate: true);
        });
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ScrollToLatest();

    // ─── Dọn dẹp event khi popup đóng ────────────────────────────────────
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler is null)
        {
            _vm.AppContext.ChatMessages.CollectionChanged -= OnMessagesChanged;
            _vm.ChatMessageSent -= ScrollToLatest;
        }
    }
}
