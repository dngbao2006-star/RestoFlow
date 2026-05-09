using System.Collections.ObjectModel;
using System.Diagnostics;
using AppManagermentRestaurant.Helpers;

namespace AppManagermentRestaurant.Services;

/// <summary>
/// Service for tracking and managing user activities throughout the application.
/// </summary>
public class ActivityLogService
{
    private static readonly Lazy<ActivityLogService> _instance = new(() => new ActivityLogService());
    public static ActivityLogService Instance => _instance.Value;

    private const string ActivityLogKey = "activity_log";
    private const int MaxActivityEntries = 100;
    private readonly ObservableCollection<ActivityLogEntry> _activityLog;

    public ObservableCollection<ActivityLogEntry> ActivityLog => _activityLog;

    private ActivityLogService()
    {
        _activityLog = new ObservableCollection<ActivityLogEntry>();
        LoadActivitiesFromStorage();
    }

    /// <summary>
    /// Logs a user activity to the activity log.
    /// </summary>
    public void LogActivity(string action, string? tableNumber = null)
    {
        try
        {
            var deviceInfo = GetDeviceInfo();
            var entry = new ActivityLogEntry
            {
                Action = FormatAction(action, tableNumber),
                Timestamp = DateTime.Now,
                Device = deviceInfo
            };

            // Add to collection (newest first)
            _activityLog.Insert(0, entry);

            // Keep only the latest entries
            while (_activityLog.Count > MaxActivityEntries)
            {
                _activityLog.RemoveAt(_activityLog.Count - 1);
            }

            // Persist to storage
            SaveActivitiesToStorage();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error logging activity: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs login activity.
    /// </summary>
    public void LogLogin(string email)
    {
        var userName = email?.Split('@')[0] ?? "Unknown";
        LogActivity($"Đăng nhập - {userName}");
    }

    /// <summary>
    /// Logs logout activity.
    /// </summary>
    public void LogLogout()
    {
        LogActivity("Đăng xuất");
    }

    /// <summary>
    /// Logs order creation activity.
    /// </summary>
    public void LogOrderCreation(string tableNumber)
    {
        LogActivity("Tạo đơn hàng", tableNumber);
    }

    /// <summary>
    /// Logs order update activity.
    /// </summary>
    public void LogOrderUpdate(string tableNumber)
    {
        LogActivity("Cập nhật đơn hàng", tableNumber);
    }

    /// <summary>
    /// Logs payment processing activity.
    /// </summary>
    public void LogPayment(string tableNumber)
    {
        LogActivity("Xử lý thanh toán", tableNumber);
    }

    /// <summary>
    /// Logs password change activity.
    /// </summary>
    public void LogPasswordChange()
    {
        LogActivity("Đổi mật khẩu");
    }

    /// <summary>
    /// Logs email verification activity.
    /// </summary>
    public void LogEmailVerification()
    {
        LogActivity("Xác minh Email");
    }

    /// <summary>
    /// Clears all activity logs.
    /// </summary>
    public void ClearActivityLog()
    {
        _activityLog.Clear();
        SaveActivitiesToStorage();
    }

    private string FormatAction(string action, string? tableNumber)
    {
        return !string.IsNullOrEmpty(tableNumber) ? $"{action} - Bàn {tableNumber}" : action;
    }

    private string GetDeviceInfo()
    {
        var os = DeviceInfo.Current.Platform.ToString();
        var model = DeviceInfo.Current.Model ?? "Unknown";
        return $"{os} - {model}";
    }

    private void SaveActivitiesToStorage()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                _activityLog.Select(a => new
                {
                    a.Action,
                    a.Timestamp,
                    a.Device
                }).ToList()
            );
            Preferences.Set(ActivityLogKey, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving activities: {ex.Message}");
        }
    }

    private void LoadActivitiesFromStorage()
    {
        try
        {
            var json = Preferences.Get(ActivityLogKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var activities = System.Text.Json.JsonSerializer.Deserialize<List<ActivityData>>(json);
                if (activities != null)
                {
                    foreach (var activity in activities)
                    {
                        _activityLog.Add(new ActivityLogEntry
                        {
                            Action = activity.Action,
                            Timestamp = activity.Timestamp,
                            Device = activity.Device
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading activities: {ex.Message}");
        }
    }

    private class ActivityData
    {
        public string Action { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Device { get; set; } = "";
    }
}

/// <summary>
/// Represents a single activity log entry.
/// </summary>
public class ActivityLogEntry : ObservableObject
{
    private string _action = "";
    private DateTime _timestamp = DateTime.Now;
    private string _device = "";

    public string Action
    {
        get => _action;
        set => SetProperty(ref _action, value);
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    public string Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public string ActionLabel => Action;
    public string TimestampDisplay => Timestamp.ToString("dd/MM/yyyy HH:mm");
    public string DeviceInfo => $"Thiết bị: {Device}";
    public bool ShowDivider => true;
}
