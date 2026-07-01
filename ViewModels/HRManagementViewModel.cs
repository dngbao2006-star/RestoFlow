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

    private readonly FirebaseService _firebaseService
    = new FirebaseService();
    private IDisposable? _presenceSubscription;

    public HRManagementViewModel()
    {
        _appContext = AppContext.Instance;

    }

    public async Task ActivateAsync()
    {
        await LoadPresenceAsync();
        if (_presenceSubscription == null)
            StartRealtimePresence();
    }

    public void Deactivate()
    {
        _presenceSubscription?.Dispose();
        _presenceSubscription = null;
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

    public ObservableCollection<Staff> FilteredStaff { get; private set; } = new();

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

        FilteredStaff = new ObservableCollection<Staff>(filtered);
        OnPropertyChanged(nameof(FilteredStaff));

        OnPropertyChanged(nameof(TotalStaff));
        OnPropertyChanged(nameof(ActiveStaff));
        OnPropertyChanged(nameof(InactiveStaff));
        OnPropertyChanged(nameof(LockedStaff));
        OnPropertyChanged(nameof(ManagerCount));
        OnPropertyChanged(nameof(StaffCount));
    }
    private async Task LoadPresenceAsync()
    {
        var tasks = _appContext.StaffMembers
            .Where(staff => !string.IsNullOrEmpty(staff.FirebaseUid))
            .Select(async staff =>
            {
                try
                {
                    var presence = await _firebaseService.GetPresenceAsync(staff.FirebaseUid)
                        .WaitAsync(TimeSpan.FromSeconds(8));
                    staff.IsOnline = presence?.IsOnline == true;
                    staff.LastSeen = presence?.LastSeen ?? DateTime.MinValue;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HR] Presence unavailable for {staff.FirebaseUid}: {ex.Message}");
                }
            });

        await Task.WhenAll(tasks);

        FilterStaff();
        _appContext.RefreshStaffMetrics();
    }
    private void StartRealtimePresence()
    {
        _firebaseService.PresenceChanged += (uid, presence) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var staff = _appContext.StaffMembers
                    .FirstOrDefault(s => s.FirebaseUid == uid);

                if (staff == null)
                {
                    return;
                }

                staff.IsOnline = presence.IsOnline;
                staff.LastSeen = presence.LastSeen;
                _appContext.RefreshStaffMetrics();
            });
        };

        _presenceSubscription = _firebaseService.StartPresenceListener();
    }
}
