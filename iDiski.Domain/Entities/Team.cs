namespace iDiski.Domain.Entities;

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short club code, e.g. "ORN" for Orlando Pirates.</summary>
    public string ShortCode { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }
    public int Founded { get; set; }
    public string? HomeGround { get; set; }
    public string? City { get; set; }
    public string? PrimaryColour { get; set; }   // hex, e.g. "#000000"
    public string? SecondaryColour { get; set; }

    public Guid? DivisionId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Division? Division { get; set; }
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<MatchResult> HomeMatches { get; set; } = new List<MatchResult>();
    public ICollection<MatchResult> AwayMatches { get; set; } = new List<MatchResult>();
}
