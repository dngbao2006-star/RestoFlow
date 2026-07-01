using AppManagermentRestaurant.Controls.Popups;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;
using CommunityToolkit.Maui.Views;

namespace AppManagermentRestaurant.Controls;

/// <summary>
/// Code-behind cho AppHeaderView.
///
/// NGUYÊN NHÂN VẤN ĐỀ VỊ TRÍ:
/// HorizontalOptions/VerticalOptions trên thẻ &lt;toolkit:Popup&gt; không định vị
/// popup tương đối với button — chúng chỉ căn chỉnh nội dung BÊN TRONG lớp
/// overlay toàn màn hình. Vì vậy popup luôn dính lên đầu màn hình.
///
/// GIẢI PHÁP: Gán thuộc tính Anchor = button nguồn trước khi gọi ShowPopupAsync.
/// CommunityToolkit.Maui dùng Anchor để tính toán và đặt popup ngay bên dưới
/// (hoặc bên cạnh) view được chỉ định — đúng chuẩn hành vi dropdown.
/// </summary>
public partial class AppHeaderView : ContentView
{
    private static HeaderViewModel? _sharedViewModel;
    public static HeaderViewModel SharedViewModel
        => _sharedViewModel ??= new HeaderViewModel(AppContext.Instance);
    public HeaderViewModel ViewModel { get; }

    public AppHeaderView()
    {
        InitializeComponent();
        ViewModel = SharedViewModel;
        BindingContext = ViewModel;
    }

    // ─── Nút Thông báo (Bell) ─────────────────────────────────────────────
    private async void OnBellClicked(object sender, EventArgs e)
    {
        var page = FindParentPage();
        if (page is null) return;

        var popup = new NotificationPopup();

        // QUAN TRỌNG: Gán Anchor = button đang được nhấn.
        // Thiếu dòng này là nguyên nhân popup hiện ở đầu màn hình thay vì bên dưới nút.
        popup.Anchor = (View)sender;

        await ShowPopupSafelyAsync(page, popup);
    }

    // ─── Nút Chat ─────────────────────────────────────────────────────────
    private async void OnChatClicked(object sender, EventArgs e)
    {
        var page = FindParentPage();
        if (page is null) return;

        var popup = new ChatPopup(ViewModel);

        // Gán Anchor để popup hiện ngay bên dưới nút chat
        popup.Anchor = (View)sender;

        await ShowPopupSafelyAsync(page, popup);
    }

    // ─── Nút Tài khoản ────────────────────────────────────────────────────
    private async void OnAccountClicked(object sender, EventArgs e)
    {
        var page = FindParentPage();
        if (page is null) return;

        var popup = new AccountPopup();

        // Gán Anchor để popup hiện ngay bên dưới nút tài khoản
        popup.Anchor = (View)sender;

        await ShowPopupSafelyAsync(page, popup);
    }

    // ─── Helper: tìm Page cha để gọi ShowPopupAsync ───────────────────────
    // ShowPopupAsync là extension method trên Page, không phải ContentView.
    private Page? FindParentPage()
    {
        Element? current = this.Parent;
        while (current is not null)
        {
            if (current is Page page) return page;
            current = current.Parent;
        }
        return Application.Current?.MainPage;
    }

    private static async Task ShowPopupSafelyAsync(Page page, Popup popup)
    {
        try
        {
            await page.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HeaderPopup] Could not open popup: {ex}");
            await page.DisplayAlert("Lỗi", "Không thể mở cửa sổ này. Vui lòng thử lại.", "OK");
        }
    }
}
