namespace SharedEntities;

public class NavigationStep
{
    public string Direction { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Typed landmark: can be a textual description or a store location
    public NavigationLandmark? Landmark { get; set; }
}

public class NavigationLandmark
{
    // Optional textual description (e.g., "Customer Service Desk")
    public string? Description { get; set; }

    // Optional geo-location (if available)
    public Location? Location { get; set; }

    public override string? ToString()
    {
        if (!string.IsNullOrEmpty(Description)) return Description;
        if (Location != null) return $"({Location.Lat:F4}, {Location.Lon:F4})";
        return null;
    }
}