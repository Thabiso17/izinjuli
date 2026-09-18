namespace iDiski.Domain.Entities;

/// <summary>
/// Something teams actually play: a league, a cup, a group stage feeding a bracket.
///
/// This stands on its own. A division is a collection of teams — Division 1, Division 2,
/// Division 3 — and those teams go and contest competitions; the two are not the same thing
/// and neither owns the other. A league is a competition whose entrants happen to be one
/// division's clubs, and a cup is a competition contested by clubs drawn from several.
///
/// It was not always so. A division used to be the competition, carrying the format with every
/// one of its teams automatically in it, which could express neither "eight of the twenty" nor
/// "and these four from Division 3". Then the competition became a child of a division, which
/// still read as though a division owned what its clubs played. Now who plays in one is the
/// entry list and nothing else.
/// </summary>
public class Competition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int Season { get; set; }

    /// <summary>
    /// How this competition is played.
    /// </summary>
    public CompetitionFormat Format { get; set; } = CompetitionFormat.League;

    /// <summary>
    /// Who it is for. Declared here rather than inferred from whoever happens to be entered,
    /// because it is what the entry list is checked against: a women's club cannot be entered
    /// into a boys competition, and a rule that reads itself off its own entrants cannot
    /// refuse the first wrong one.
    ///
    /// It used to be read from the division running the competition. Nothing runs one now.
    /// </summary>
    public Gender Gender { get; set; } = Gender.Male;

    /// <summary>
    /// Who it is for by age — "U17", "Open". Free text and informational: age has never been
    /// enforced at entry, because a club playing up an age group is ordinary.
    /// </summary>
    public string? AgeGroup { get; set; }

    /// <summary>
    /// How many clubs are meant to play in this, when the organiser has decided up front — a
    /// top-eight cup is eight, however many clubs exist. Null means no limit was set, which is
    /// the ordinary case for a league: it is played by whoever is entered.
    ///
    /// A cap on the entry list rather than a description of it. Entering a ninth club into an
    /// eight-club cup is refused, because the alternative is a bracket quietly built around a
    /// number nobody meant.
    /// </summary>
    public int? MaxTeams { get; set; }

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
