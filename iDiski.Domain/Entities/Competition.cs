namespace iDiski.Domain.Entities;

/// <summary>
/// Something the teams of a division actually play: a league, a cup, a group stage feeding a
/// bracket.
///
/// A division used to be the competition — it carried the format, and its entrants were simply
/// every team in it. That allowed exactly one competition per division per season, which is not
/// how a season works. An under-seventeen division of twenty clubs runs its league, a top-eight
/// cup for the eight that earned it, and a sponsor's tournament for twelve of them plus four
/// clubs invited from elsewhere — all at once, all season.
///
/// So a division is now a pool of teams and this is the competition. Who plays in one is the
/// entry list rather than the division's membership, which is what makes both "twelve of the
/// twenty" and "and these four from outside" expressible at all.
/// </summary>
public class Competition : BaseEntity
{
    /// <summary>
    /// The division this competition was created from and belongs to. It decides who
    /// administers the competition, and it is the division a visitor finds it under — not a
    /// statement that every entrant comes from it.
    /// </summary>
    public Guid DivisionId { get; set; }
    public Division Division { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int Season { get; set; }

    /// <summary>
    /// How this competition is played. It sits here rather than on the division because two
    /// competitions in the same division are routinely different shapes.
    /// </summary>
    public CompetitionFormat Format { get; set; } = CompetitionFormat.League;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CompetitionEntry> Entries { get; set; } = new List<CompetitionEntry>();
    public ICollection<MatchResult> Matches { get; set; } = new List<MatchResult>();
}

/// <summary>
/// One club's place in one competition.
///
/// This is the whole point of the change. Entrants used to be "every team whose DivisionId
/// matches", which cannot say twelve of twenty and cannot say a club from another division.
/// An explicit entry list says both, and says them the same way.
/// </summary>
public class CompetitionEntry : BaseEntity
{
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
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
