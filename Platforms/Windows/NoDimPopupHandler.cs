// File: Platforms/Windows/NoDimPopupHandler.cs
//
// NGUYÊN NHÂN DIM:
// CommunityToolkit.Maui v9.0.0 trên Windows dùng Microsoft.UI.Xaml.Controls.Popup
// làm native view. Khi CanBeDismissedByTappingOutsideOfPopup="True", toolkit tự
// bật LightDismissOverlayMode.On → WinUI thêm một lớp dim ở cấp độ native,
// HOÀN TOÀN bỏ qua thuộc tính Color="Transparent" của MAUI.
//
// GIẢI PHÁP: Override ConnectHandler để ngắt LightDismissOverlayMode = Off
// ngay sau khi native popup được tạo và trước khi nó hiển thị.

#if WINDOWS

using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace AppManagermentRestaurant.Platforms.Windows;

/// <summary>
/// Custom PopupHandler cho Windows: tắt WinUI LightDismissOverlay (lớp dim)
/// trong khi vẫn giữ nguyên hành vi đóng khi tap bên ngoài.
/// </summary>
public partial class NoDimPopupHandler : PopupHandler
{
    protected override void ConnectHandler(Popup platformView)
    {
        base.ConnectHandler(platformView);

        // Tắt lớp dim native của WinUI — đây là nguyên nhân gốc rễ của vấn đề.
        // LightDismissOverlayMode.Off = không có overlay mờ, nhưng popup vẫn
        // đóng khi người dùng click bên ngoài (do IsLightDismissEnabled vẫn true).
        platformView.LightDismissOverlayMode = LightDismissOverlayMode.Off;
    }
}

#endif
