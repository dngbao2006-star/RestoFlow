namespace AppManagermentRestaurant.Models;

public class Staff
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public StaffStatus Status { get; set; }
    public DateTime LastLogin { get; set; }
    public DateTime JoinDate { get; set; }
    public List<string> Permissions { get; set; } = new();

    public string Initials => string.Join("", Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0])).ToUpperInvariant();

    /// <summary>Lấy 2 từ cuối của họ tên đầy đủ (ví dụ "Nguyễn Thị Hương" → "Thị Hương")</summary>
    public string ShortName
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? string.Join(" ", parts.TakeLast(2))
                : Name;
        }
    }

    public string RoleLabel => Role == StaffRole.Manager ? "Quản lý" : "Nhân viên";

    public string StatusLabel => Status switch
    {
        StaffStatus.Active => "Hoạt động",
        StaffStatus.Inactive => "Ngừng hoạt động",
        StaffStatus.Locked => "Đã khóa",
        _ => "Không xác định"
    };
}
