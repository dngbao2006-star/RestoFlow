namespace AppManagermentRestaurant.Helpers;

public static class MenuCategoryHelper
{
    public static bool Matches(string? itemCategory, string filter)
    {
        if (filter == "All") return true;
        var normalized = (itemCategory ?? string.Empty).Trim();
        return filter switch
        {
            "Appetizers" => IsAny(normalized, "Appetizers", "Khai vị"),
            "Main Course" => IsAny(normalized, "Main Course", "Món chính"),
            "Soups" => IsAny(normalized, "Soups", "Canh/Súp", "Canh", "Súp"),
            "Seafood" => IsAny(normalized, "Seafood", "Hải sản"),
            "Desserts" => IsAny(normalized, "Desserts", "Tráng miệng"),
            "Drinks" => IsAny(normalized, "Drinks", "Đồ uống", "Nước uống"),
            _ => string.Equals(normalized, filter, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool IsAny(string value, params string[] candidates)
        => candidates.Any(candidate => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));
}
