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

    public string RoleLabel => Role == StaffRole.Manager ? "Manager" : "Staff";

    public string StatusLabel => Status switch
    {
        StaffStatus.Active => "Active",
        StaffStatus.Inactive => "Inactive",
        StaffStatus.Locked => "Locked",
        _ => "Unknown"
    };
}
