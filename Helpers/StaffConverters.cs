using System.Globalization;
using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Helpers;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StaffStatus status)
        {
            return status switch
            {
                StaffStatus.Active => Color.FromArgb("#10B981"),      // Green
                StaffStatus.Inactive => Color.FromArgb("#F59E0B"),    // Amber
                StaffStatus.Locked => Color.FromArgb("#EF4444"),      // Red
                _ => Color.FromArgb("#6B7280")                         // Gray
            };
        }
        return Color.FromArgb("#6B7280");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LockUnlockButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StaffStatus status)
        {
            return status == StaffStatus.Locked ? "Unlock" : "Lock";
        }
        return "Lock";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PermissionLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string permission)
        {
            return permission switch
            {
                "dashboard" => "Dashboard Access",
                "hr" => "HR Management",
                "menu" => "Menu Management",
                "revenue" => "Revenue Reports",
                "system" => "System Configuration",
                "tables" => "Table Management",
                "orders" => "Order Management",
                "payment" => "Payment Processing",
                "chat" => "Staff Chat",
                _ => permission
            };
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
