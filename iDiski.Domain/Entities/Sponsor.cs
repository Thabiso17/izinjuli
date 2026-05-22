namespace iDiski.Domain.Entities;

public enum SponsorTier
{
    Title,
    Gold,
    Silver,
    Bronze
}

public enum AdPlacement
{
    Header,
    Sidebar,
    Footer,
    MatchDay,
    Homepage,
    NewsPage
}

public class Sponsor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    /// <summary>The actual banner/creative served in the Angular component.</summary>
    public string? AdImageUrl { get; set; }

    /// <summary>Where the ad click leads (may differ from WebsiteUrl for campaign tracking).</summary>
    public string? AdLinkUrl { get; set; }

    public SponsorTier Tier { get; set; } = SponsorTier.Bronze;
    public AdPlacement Placement { get; set; } = AdPlacement.Homepage;
    public bool IsActive { get; set; } = true;

    public DateTime? ContractStart { get; set; }
    public DateTime? ContractEnd { get; set; }

    /// <summary>Lower number = higher priority in display order within the same Placement.</summary>
    public int DisplayOrder { get; set; }
}
