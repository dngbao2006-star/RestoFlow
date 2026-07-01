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

        RoleFilter.ItemsSource = new[] { "Tất cả vai trò", "Nhân viên", "Quản lý" };
        StatusFilter.ItemsSource = new[] { "Tất cả trạng thái", "Hoạt động", "Ngừng hoạt động", "Đã khóa" };
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        try
        {
            await _viewModel.ActivateAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HR] Could not activate page: {ex}");
        }
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        _viewModel.Deactivate();
        base.OnNavigatingFrom(args);
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

            BtnEditStaff.IsVisible = CanEdit(staff);
            BtnDeleteStaff.IsVisible = staff.Role == StaffRole.Staff;
        }
    }

    private void OnCloseDetailsClicked(object sender, EventArgs e)
    {
        _viewModel.CloseStaffDetails();
    }

    private void OnRoleFilterChanged(object? sender, EventArgs e)
    {
        var role = RoleFilter.Text.Trim();
        if (string.IsNullOrWhiteSpace(role) || role.StartsWith("Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            _viewModel.FilterByRole = false;
            if (role.StartsWith("Tất cả", StringComparison.OrdinalIgnoreCase))
                RoleFilter.Clear();
            return;
        }

        if (role.Contains("quản", StringComparison.OrdinalIgnoreCase))
        {
            _viewModel.SelectedRole = StaffRole.Manager;
            _viewModel.FilterByRole = true;
        }
        else if (role.Contains("nhân", StringComparison.OrdinalIgnoreCase))
        {
            _viewModel.SelectedRole = StaffRole.Staff;
            _viewModel.FilterByRole = true;
        }
    }

    private void OnStatusFilterChanged(object? sender, EventArgs e)
    {
        var status = StatusFilter.Text.Trim();
        if (string.IsNullOrWhiteSpace(status) || status.StartsWith("Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            _viewModel.FilterByStatus = false;
            if (status.StartsWith("Tất cả", StringComparison.OrdinalIgnoreCase))
                StatusFilter.Clear();
            return;
        }

        _viewModel.SelectedStatus = status switch
        {
            var value when value.Contains("khóa", StringComparison.OrdinalIgnoreCase) => StaffStatus.Locked,
            var value when value.Contains("ngừng", StringComparison.OrdinalIgnoreCase) => StaffStatus.Inactive,
            _ => StaffStatus.Active
        };
        _viewModel.FilterByStatus = true;
    }

    // ═══════════════════════════════════════════════════════════════
    //  EDIT STAFF MODAL
    // ═══════════════════════════════════════════════════════════════

    private async void OnEditStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff == null) return;

        if (!CanEdit(_viewModel.SelectedStaff))
        {
            await DisplayAlert("Không thể chỉnh sửa", "Bạn không thể chỉnh sửa thông tin của quản lý khác.", "Đã hiểu");
            return;
        }

        var staff = _viewModel.SelectedStaff;

        // Pre-fill modal fields
        EditModalTitle.Text = staff.Role == StaffRole.Manager
            ? "✏️ Chỉnh sửa thông tin của tôi"
            : "✏️ Chỉnh sửa thông tin nhân viên";
        EditName.Text = staff.Name;
        EditEmail.Text = staff.Email;
        EditPhone.Text = staff.Phone;
        EditJoinDate.Date = staff.JoinDate == DateTime.MinValue ? DateTime.Now : staff.JoinDate;

        var isActive = staff.Status == StaffStatus.Active;
        EditStatusSwitch.IsToggled = isActive;
        EditStatusLabel.Text = isActive ? "Hoạt động" : "Đã khóa";
        EditStatusLabel.TextColor = isActive ? Color.FromArgb("#22C55E") : Color.FromArgb("#EF4444");
        EditStatusSection.IsVisible = staff.Role == StaffRole.Staff;
        EditStatusSwitch.IsEnabled = staff.Role == StaffRole.Staff;

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
        if (!CanEdit(staff))
        {
            await DisplayAlert("Không thể chỉnh sửa", "Bạn không thể chỉnh sửa thông tin của quản lý khác.", "Đã hiểu");
            return;
        }

        var newName = EditName.Text?.Trim() ?? "";
        var newEmail = EditEmail.Text?.Trim() ?? "";
        var newPhone = EditPhone.Text?.Trim() ?? "";
        var newJoinDate = EditJoinDate.Date ?? DateTime.Now;
        var newStatus = staff.Role == StaffRole.Manager
            ? staff.Status
            : EditStatusSwitch.IsToggled ? StaffStatus.Active : StaffStatus.Locked;
        var statusString = newStatus == StaffStatus.Active ? "HoatDong" : "TamKhoa";

        if (string.IsNullOrWhiteSpace(newName))
        {
            await DisplayAlert("Thiếu thông tin", "Họ và tên không được để trống.", "Đóng");
            return;
        }

        try
        {
            await _firebase.UpdateStaffProfileAsync(
                staff.FirebaseUid,
                newName,
                newEmail,
                newPhone,
                newJoinDate.ToString("yyyy-MM-dd"),
                statusString);

            // Update local
            staff.Name = newName;
            staff.Email = newEmail;
            staff.Phone = newPhone;
            staff.JoinDate = newJoinDate;
            staff.Status = newStatus;

            _viewModel.RefreshFilteredList();

            // Reassign to refresh bindings
            _viewModel.SelectedStaff = null;
            _viewModel.SelectedStaff = staff;

            BtnEditStaff.IsVisible = CanEdit(staff);
            BtnDeleteStaff.IsVisible = staff.Role == StaffRole.Staff;

            if (IsCurrentUser(staff))
            {
                var current = AppContext.Instance.CurrentUser!;
                AppContext.Instance.CurrentUser = new Staff
                {
                    FirebaseUid = current.FirebaseUid,
                    Name = newName,
                    Email = current.Email,
                    Phone = newPhone,
                    JoinDate = newJoinDate,
                    Role = current.Role,
                    Status = current.Status,
                    LastLogin = current.LastLogin,
                    Permissions = current.Permissions,
                    IsOnline = current.IsOnline,
                    LastSeen = current.LastSeen
                };

                if (!string.IsNullOrWhiteSpace(AppContext.Instance.CurrentSessionId))
                {
                    try
                    {
                        await _firebase.SetUserOnlineAsync(
                            current.FirebaseUid,
                            newName,
                            AppContext.Instance.CurrentSessionId!);
                    }
                    catch (Exception presenceError)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HR] Profile saved but presence name refresh failed: {presenceError.Message}");
                    }
                }
            }

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
        if (staff.Role == StaffRole.Manager)
        {
            await DisplayAlert("Không thể xóa", "Không thể xóa tài khoản quản lý tại đây.", "Đóng");
            return;
        }
        var confirm = await DisplayAlert(
            "Xóa nhân viên",
            $"Bạn chắc chắn muốn xóa {staff.Name}? Hành động này không thể hoàn tác.",
            "Xóa", "Hủy");

        if (!confirm) return;

        _viewModel.RemoveStaff(staff);
        _viewModel.CloseStaffDetails();
        await DisplayAlert("Thành công", $"Đã xóa {staff.Name} khỏi danh sách nhân sự.", "Đã hiểu");
    }

    private static bool IsCurrentUser(Staff staff)
        => !string.IsNullOrWhiteSpace(staff.FirebaseUid)
           && staff.FirebaseUid == AppContext.Instance.CurrentUser?.FirebaseUid;

    private static bool CanEdit(Staff staff)
        => staff.Role == StaffRole.Staff || IsCurrentUser(staff);
}
