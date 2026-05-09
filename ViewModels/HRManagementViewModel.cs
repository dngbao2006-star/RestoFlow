using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;
using AppManagermentRestaurant.Services;

namespace AppManagermentRestaurant.ViewModels;

public class HRManagementViewModel : ObservableObject
{
    private string _searchText = string.Empty;
    private StaffRole _selectedRole = StaffRole.Staff;
    private StaffStatus _selectedStatus = StaffStatus.Active;
    private bool _filterByRole;
    private bool _filterByStatus;
    private Staff? _selectedStaff;
    private bool _showStaffDetails;

    private readonly AppContext _appContext;

    public HRManagementViewModel()
    {
        _appContext = AppContext.Instance;
        FilterStaff();
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterStaff();
            }
        }
    }

    public StaffRole SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value))
            {
                FilterStaff();
            }
        }
    }

    public StaffStatus SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                FilterStaff();
            }
        }
    }

    public bool FilterByRole
    {
        get => _filterByRole;
        set
        {
            if (SetProperty(ref _filterByRole, value))
            {
                FilterStaff();
            }
        }
    }

    public bool FilterByStatus
    {
        get => _filterByStatus;
        set
        {
            if (SetProperty(ref _filterByStatus, value))
            {
                FilterStaff();
            }
        }
    }

    public Staff? SelectedStaff
    {
        get => _selectedStaff;
        set => SetProperty(ref _selectedStaff, value);
    }

    public bool ShowStaffDetails
    {
        get => _showStaffDetails;
        set => SetProperty(ref _showStaffDetails, value);
    }

    public ObservableCollection<Staff> FilteredStaff { get; } = new();

    public int TotalStaff => _appContext.StaffMembers.Count;
    public int ActiveStaff => _appContext.StaffMembers.Count(s => s.Status == StaffStatus.Active);
    public int InactiveStaff => _appContext.StaffMembers.Count(s => s.Status == StaffStatus.Inactive);
    public int LockedStaff => _appContext.StaffMembers.Count(s => s.Status == StaffStatus.Locked);
    public int ManagerCount => _appContext.StaffMembers.Count(s => s.Role == StaffRole.Manager);
    public int StaffCount => _appContext.StaffMembers.Count(s => s.Role == StaffRole.Staff);

    public void RefreshFilteredList()
    {
        FilterStaff();
    }

    public void ClearFilters()
    {
        SearchText = string.Empty;
        FilterByRole = false;
        FilterByStatus = false;
        SelectedRole = StaffRole.Staff;
        SelectedStatus = StaffStatus.Active;
        FilterStaff();
    }

    public void ViewStaffDetails(Staff staff)
    {
        SelectedStaff = staff;
        ShowStaffDetails = true;
    }

    public void CloseStaffDetails()
    {
        ShowStaffDetails = false;
        SelectedStaff = null;
    }

    public void UnlockStaff(Staff staff)
    {
        if (staff.Status != StaffStatus.Locked)
        {
            return;
        }

        // TODO: [BACKEND] - Chỗ này gọi API mở khóa tài khoản nhân viên và đồng bộ trạng thái.
        staff.Status = StaffStatus.Active;
        OnPropertyChanged(nameof(FilteredStaff));
    }

    public void LockStaff(Staff staff)
    {
        if (staff.Status == StaffStatus.Locked)
        {
            return;
        }

        // TODO: [BACKEND] - Chỗ này gọi API khóa tài khoản nhân viên.
        staff.Status = StaffStatus.Locked;
        OnPropertyChanged(nameof(FilteredStaff));
    }

    public void RemoveStaff(Staff staff)
    {
        // TODO: [BACKEND] - Chỗ này gọi API xóa nhân viên khỏi hệ thống.
        _appContext.StaffMembers.Remove(staff);
        FilterStaff();
    }

    private void FilterStaff()
    {
        var filtered = _appContext.StaffMembers
            .Where(s =>
            {
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    var search = _searchText.ToLowerInvariant();
                    if (!s.Name.ToLowerInvariant().Contains(search) && !s.Email.ToLowerInvariant().Contains(search))
                    {
                        return false;
                    }
                }

                if (_filterByRole && s.Role != _selectedRole)
                {
                    return false;
                }

                if (_filterByStatus && s.Status != _selectedStatus)
                {
                    return false;
                }

                return true;
            })
            .ToList();

        FilteredStaff.Clear();
        foreach (var staff in filtered)
        {
            FilteredStaff.Add(staff);
        }

        OnPropertyChanged(nameof(TotalStaff));
        OnPropertyChanged(nameof(ActiveStaff));
        OnPropertyChanged(nameof(InactiveStaff));
        OnPropertyChanged(nameof(LockedStaff));
        OnPropertyChanged(nameof(ManagerCount));
        OnPropertyChanged(nameof(StaffCount));
    }
}
