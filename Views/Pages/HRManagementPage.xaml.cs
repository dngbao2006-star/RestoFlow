using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class HRManagementPage : ContentPage
{
    private readonly HRManagementViewModel _viewModel;

    public HRManagementPage()
    {
        InitializeComponent();
        _viewModel = new HRManagementViewModel();
        BindingContext = _viewModel;
    }

    private async void OnAddEmployeeClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Nhân sự", "Tính năng thêm nhân viên sẽ được cập nhật ở phiên bản tiếp theo.", "Đã hiểu");
    }

    private void OnViewStaffClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Staff staff })
        {
            _viewModel.ViewStaffDetails(staff);
        }
    }

    private async void OnToggleLockClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Staff staff })
        {
            return;
        }

        if (staff.Status == StaffStatus.Locked)
        {
            var confirmUnlock = await DisplayAlert("Mở khóa tài khoản", $"Bạn có muốn mở khóa cho {staff.Name}?", "Mở khóa", "Hủy");
            if (!confirmUnlock)
            {
                return;
            }

            _viewModel.UnlockStaff(staff);
            return;
        }

        var confirmLock = await DisplayAlert("Khóa tài khoản", $"Khóa tài khoản {staff.Name}? Nhân viên sẽ không thể đăng nhập hệ thống.", "Khóa", "Hủy");
        if (!confirmLock)
        {
            return;
        }

        _viewModel.LockStaff(staff);
    }

    private void OnCloseDetailsClicked(object sender, EventArgs e)
    {
        _viewModel.CloseStaffDetails();
    }

    private async void OnEditStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff != null)
        {
            await DisplayAlert("Nhân sự", $"Chức năng chỉnh sửa { _viewModel.SelectedStaff.Name } sẽ được bổ sung sau.", "Đã hiểu");
        }
    }

    private async void OnDeleteStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff == null)
        {
            return;
        }

        var staff = _viewModel.SelectedStaff;
        var confirm = await DisplayAlert("Xóa nhân viên", $"Bạn chắc chắn muốn xóa {staff.Name}? Hành động này không thể hoàn tác.", "Xóa", "Hủy");

        if (!confirm)
        {
            return;
        }

        _viewModel.RemoveStaff(staff);
        _viewModel.CloseStaffDetails();
        await DisplayAlert("Thành công", $"Đã xóa {staff.Name} khỏi danh sách nhân sự.", "Đã hiểu");
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        _viewModel.SearchText = string.Empty;
    }

    private void OnClearFiltersClicked(object sender, EventArgs e)
    {
        _viewModel.ClearFilters();
    }

    private void OnRoleFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string roleFilter })
        {
            return;
        }

        switch (roleFilter)
        {
            case "All":
                _viewModel.FilterByRole = false;
                break;
            case "Staff":
                _viewModel.SelectedRole = StaffRole.Staff;
                _viewModel.FilterByRole = true;
                break;
            case "Manager":
                _viewModel.SelectedRole = StaffRole.Manager;
                _viewModel.FilterByRole = true;
                break;
        }
    }

    private void OnStatusFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string statusFilter })
        {
            return;
        }

        switch (statusFilter)
        {
            case "All":
                _viewModel.FilterByStatus = false;
                break;
            case "Active":
                _viewModel.SelectedStatus = StaffStatus.Active;
                _viewModel.FilterByStatus = true;
                break;
            case "Inactive":
                _viewModel.SelectedStatus = StaffStatus.Inactive;
                _viewModel.FilterByStatus = true;
                break;
            case "Locked":
                _viewModel.SelectedStatus = StaffStatus.Locked;
                _viewModel.FilterByStatus = true;
                break;
        }
    }
}
