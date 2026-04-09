using System.Collections.ObjectModel;
using AppManagermentRestaurant.Helpers;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.ViewModels;

public class HRManagementViewModel : ObservableObject
{
    private string _searchText = string.Empty;
    private StaffRole _selectedRole = StaffRole.Staff;
    private StaffStatus _selectedStatus = StaffStatus.Active;
    private bool _filterByRole = false;
    private bool _filterByStatus = false;
    private Staff? _selectedStaff;
    private bool _showStaffDetails = false;

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

    private readonly AppContext _appContext;

    public HRManagementViewModel()
    {
        _appContext = AppContext.Instance;
        FilterStaff();
    }

    private void FilterStaff()
    {
        var filtered = _appContext.StaffMembers
            .Where(s =>
            {
                // Search filter
                if (!string.IsNullOrEmpty(_searchText))
                {
                    var search = _searchText.ToLower();
                    if (!s.Name.ToLower().Contains(search) && !s.Email.ToLower().Contains(search))
                    {
                        return false;
                    }
                }

                // Role filter
                if (_filterByRole && s.Role != _selectedRole)
                {
                    return false;
                }

                // Status filter
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
    }

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

    public async Task UnlockStaff(Staff staff)
    {
        if (staff.Status == StaffStatus.Locked)
        {
            staff.Status = StaffStatus.Active;
            OnPropertyChanged(nameof(FilteredStaff));
        }
    }

    public async Task LockStaff(Staff staff)
    {
        if (staff.Status != StaffStatus.Locked)
        {
            staff.Status = StaffStatus.Locked;
            OnPropertyChanged(nameof(FilteredStaff));
        }
    }

    public void RemoveStaff(Staff staff)
    {
        _appContext.StaffMembers.Remove(staff);
        FilterStaff();
    }

    public int TotalStaff => _appContext.StaffMembers.Count;
    public int ActiveStaff => _appContext.StaffMembers.Count(s => s.Status == StaffStatus.Active);
    public int InactiveStaff => _appContext.StaffMembers.Count(s => s.Status == StaffStatus.Inactive);
    public int LockedStaff => _appContext.StaffMembers.Count(s => s.Status == StaffStatus.Locked);
    public int ManagerCount => _appContext.StaffMembers.Count(s => s.Role == StaffRole.Manager);
    public int StaffCount => _appContext.StaffMembers.Count(s => s.Role == StaffRole.Staff);
}
