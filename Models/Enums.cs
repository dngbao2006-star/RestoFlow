namespace AppManagermentRestaurant.Models;

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    NeedsClearing
}

public enum DishStatus
{
    Pending,
    Preparing,
    Ready,
    Served
}

public enum OrderStatus
{
    Active,
    Paid
}

public enum StaffRole
{
    Staff,
    Manager
}

public enum StaffStatus
{
    Active,
    Inactive,
    Locked
}

public enum NotificationType
{
    Info,
    Warning,
    Success,
    Danger
}

public enum PaymentMethod
{
    Cash,
    Qr
}
