namespace iDiski.Domain.Entities;

/// <summary>
/// Drives the dynamic ordering and visibility of Angular components on any page.
/// The Angular app fetches this on init and renders components in DisplayOrder sequence.
///
/// Example rows:
///   PageName="main", ComponentName="HeroBanner",   DisplayOrder=1, IsVisible=true
///   PageName="main", ComponentName="LeagueTable",  DisplayOrder=2, IsVisible=true
///   PageName="main", ComponentName="LatestNews",   DisplayOrder=3, IsVisible=true
///   PageName="main", ComponentName="SponsorBanner",DisplayOrder=4, IsVisible=false
/// </summary>
public class PageLayoutConfig : BaseEntity
{
    /// <summary>Logical page identifier: "main" | "matches" | "news" | "teams" etc.</summary>
    public string PageName { get; set; } = "main";

    /// <summary>Must match the Angular component selector / registration key exactly.</summary>
    public string ComponentName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Optional JSON blob for component-specific configuration,
    /// e.g. { "maxItems": 5, "showImages": true }.
    /// Parse on the Angular side with JSON.parse().
    /// </summary>
    public string? ConfigJson { get; set; }

    public string ModifiedByUser { get; set; } = "admin";
}
