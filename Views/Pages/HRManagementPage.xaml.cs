using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;
using AppManagermentRestaurant.ViewModels;

namespace AppManagermentRestaurant.Views.Pages;

public partial class HRManagementPage : ContentPage
{
    private HRManagementViewModel _viewModel;
    private string _currentRoleFilter = "All";
    private string _currentStatusFilter = "All";

    public HRManagementPage()
    {
        InitializeComponent();
        _viewModel = new HRManagementViewModel();
        BindingContext = _viewModel;
    }

    private async void OnAddEmployeeClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Add Employee", "Add Employee functionality coming soon!", "OK");
    }

    private async void OnViewStaffClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is Staff staff)
        {
            _viewModel.ViewStaffDetails(staff);
        }
    }

    private async void OnToggleLockClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is Staff staff)
        {
            if (staff.Status == StaffStatus.Locked)
            {
                bool confirm = await DisplayAlert("Unlock Staff", $"Unlock {staff.Name}?", "Yes", "No");
                if (confirm)
                {
                    staff.Status = StaffStatus.Active;
                    _viewModel.RefreshFilteredList();
                }
            }
            else
            {
                bool confirm = await DisplayAlert("Lock Staff", $"Lock {staff.Name}? They won't be able to access the system.", "Yes", "No");
                if (confirm)
                {
                    staff.Status = StaffStatus.Locked;
                    _viewModel.RefreshFilteredList();
                }
            }
        }
    }

    private async void OnCloseDetailsClicked(object sender, EventArgs e)
    {
        _viewModel.CloseStaffDetails();
    }

    private async void OnEditStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff != null)
        {
            await DisplayAlert("Edit Staff", $"Edit {_viewModel.SelectedStaff.Name} coming soon!", "OK");
        }
    }

    private async void OnDeleteStaffClicked(object sender, EventArgs e)
    {
        if (_viewModel.SelectedStaff != null)
        {
            bool confirm = await DisplayAlert(
                "Delete Staff",
                $"Are you sure you want to permanently delete {_viewModel.SelectedStaff.Name}? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (confirm)
            {
                var staff = _viewModel.SelectedStaff;
                _viewModel.RemoveStaff(staff);
                _viewModel.CloseStaffDetails();
                await DisplayAlert("Success", $"{staff.Name} has been removed.", "OK");
            }
        }
    }

    private void OnClearSearchClicked(object sender, EventArgs e)
    {
        _viewModel.SearchText = string.Empty;
    }

    private void OnClearFiltersClicked(object sender, EventArgs e)
    {
        _viewModel.ClearFilters();
        _currentRoleFilter = "All";
        _currentStatusFilter = "All";
    }

    private void OnRoleFilterClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is string roleFilter)
        {
            _currentRoleFilter = roleFilter;
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
    }

    private void OnStatusFilterClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is string statusFilter)
        {
            _currentStatusFilter = statusFilter;
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

    private void OnRefreshFilteredStaff()
    {
        _viewModel.RefreshFilteredList();
    }
}
