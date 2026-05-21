using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class HRManagementPage : ContentPage
{
    private readonly HRManagementViewModel _viewModel;
    private readonly FirebaseService _firebase = new();

    public HRManagementPage()
    {
        InitializeComponent();
        _viewModel = new HRManagementViewModel();
        BindingContext = _viewModel;
    }

    private async void OnAddEmployeeClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterStaffPage());
    }

    private void OnViewStaffClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Staff staff })
        {
            _viewModel.ViewStaffDetails(staff);

            // Show/hide edit button based on role
            BtnEditStaff.IsVisible = staff.Role == StaffRole.Staff;
        }
    }

    private void OnCloseDetailsClicked(object sender, EventArgs e)
    {
        _viewModel.CloseStaffDetails();
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        _viewModel.SearchText = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════
    //  EDIT STAFF MODAL
    // ═══════════════════════════════════════════════════════════════

    private void OnEditStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff == null) return;

        // Only staff role can be edited
        if (_viewModel.SelectedStaff.Role != StaffRole.Staff)
        {
            DisplayAlert("Thông báo", "Không thể chỉnh sửa tài khoản quản lý.", "Đã hiểu");
            return;
        }

        var staff = _viewModel.SelectedStaff;

        // Pre-fill modal fields
        EditEmail.Text = staff.Email;
        EditPhone.Text = staff.Phone;
        EditJoinDate.Date = staff.JoinDate == DateTime.MinValue ? DateTime.Now : staff.JoinDate;

        var isActive = staff.Status == StaffStatus.Active;
        EditStatusSwitch.IsToggled = isActive;
        EditStatusLabel.Text = isActive ? "Hoạt động" : "Đã khóa";
        EditStatusLabel.TextColor = isActive ? Color.FromArgb("#22C55E") : Color.FromArgb("#EF4444");

        EditStaffModal.IsVisible = true;
    }

    private void OnCloseEditModalTapped(object? sender, EventArgs e)
    {
        EditStaffModal.IsVisible = false;
    }

    private void OnStatusToggled(object? sender, ToggledEventArgs e)
    {
        EditStatusLabel.Text = e.Value ? "Hoạt động" : "Đã khóa";
        EditStatusLabel.TextColor = e.Value ? Color.FromArgb("#22C55E") : Color.FromArgb("#EF4444");
    }

    private async void OnSaveEditClicked(object? sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff == null) return;

        var staff = _viewModel.SelectedStaff;
        var newEmail = EditEmail.Text?.Trim() ?? "";
        var newPhone = EditPhone.Text?.Trim() ?? "";
        var newJoinDate = EditJoinDate.Date ?? DateTime.Now;
        var newStatus = EditStatusSwitch.IsToggled ? StaffStatus.Active : StaffStatus.Locked;
        var statusString = newStatus == StaffStatus.Active ? "HoatDong" : "TamKhoa";

        try
        {
            await _firebase.UpdateStaffProfileAsync(
                staff.FirebaseUid,
                newEmail,
                newPhone,
                newJoinDate.ToString("yyyy-MM-dd"),
                statusString);

            // Update local
            staff.Email = newEmail;
            staff.Phone = newPhone;
            staff.JoinDate = newJoinDate;
            staff.Status = newStatus;

            _viewModel.RefreshFilteredList();

            // Reassign to refresh bindings
            _viewModel.SelectedStaff = null;
            _viewModel.SelectedStaff = staff;

            // Re-evaluate edit button visibility
            BtnEditStaff.IsVisible = staff.Role == StaffRole.Staff;

            EditStaffModal.IsVisible = false;
            await DisplayAlert("Thành công", "Đã cập nhật thông tin nhân viên.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể cập nhật: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff == null) return;

        var staff = _viewModel.SelectedStaff;
        var confirm = await DisplayAlert(
            "Xóa nhân viên",
            $"Bạn chắc chắn muốn xóa {staff.Name}? Hành động này không thể hoàn tác.",
            "Xóa", "Hủy");

        if (!confirm) return;

        _viewModel.RemoveStaff(staff);
        _viewModel.CloseStaffDetails();
        await DisplayAlert("Thành công", $"Đã xóa {staff.Name} khỏi danh sách nhân sự.", "Đã hiểu");
    }
}