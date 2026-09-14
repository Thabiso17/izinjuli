namespace iDiski.Domain.Entities;

public class Division : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int Season { get; set; }
    public string? AgeGroup { get; set; }
    public Gender? Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// How this competition is played. A league is a round-robin settled on points; a knockout
    /// is a bracket settled by winning; a group stage feeds a bracket. It sits on the division
    /// rather than being chosen when fixtures are generated, so a division cannot end up
    /// holding a mixture nobody asked for.
    /// </summary>
    public CompetitionFormat Format { get; set; } = CompetitionFormat.League;

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<MatchResult> Matches { get; set; } = new List<MatchResult>();
}

/// <summary>
/// The three shapes a competition takes. Named Format rather than Type to stay clear of
/// System.Type in a codebase where entities are reflected over.
/// </summary>
public enum CompetitionFormat
{
    /// <summary>Everyone plays everyone; the table decides it.</summary>
    League = 0,

    /// <summary>A straight bracket: lose and you are out.</summary>
    Knockout = 1,

    /// <summary>Round-robin groups, then the qualifiers play a bracket.</summary>
    GroupAndKnockout = 2
}

public enum Gender
{
    Male = 0,
    Female = 1,
    Mixed = 2
}
