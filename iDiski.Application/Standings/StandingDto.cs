namespace iDiski.Application.Standings;

/// <summary>
/// A single row in the league table.
/// Returned by <see cref="Queries.GetLeagueStandingsQuery"/>.
/// </summary>
public sealed record StandingDto(
    int     Position,
    Guid    TeamId,
    string  TeamName,
    string  ShortCode,
    string? LogoUrl,
    int     Played,
    int     Won,
    int     Drawn,
    int     Lost,
    int     GoalsFor,
    int     GoalsAgainst,
    int     GoalDifference,
    int     Points,
    /// <summary>
    /// Last 5 results newest-first, comma-separated: "W,W,D,L,W".
    /// Angular renders this as coloured dots in the form guide column.
    /// </summary>
    string  Form
);

/// <summary>Wrapper returned by the query so Angular can display season metadata.</summary>
public sealed record LeagueTableDto(
    int                     Season,
    int                     MatchweeksPlayed,
    IReadOnlyList<StandingDto> Table
);
