namespace iDiski.Domain.Entities;

public enum MatchStatus
{
    Scheduled,
    InProgress,
    Completed,
    Postponed,
    Cancelled
}

/// <summary>Which part of a competition a fixture belongs to.</summary>
public enum MatchStage
{
    /// <summary>A round-robin league fixture, settled on points.</summary>
    League = 0,

    /// <summary>A group-stage fixture, which is a league in miniature.</summary>
    Group = 1,

    /// <summary>A bracket fixture: the winner goes through.</summary>
    Knockout = 2
}

/// <summary>Which side of a fixture a qualifying team occupies.</summary>
public enum MatchSlot
{
    Home = 0,
    Away = 1
}

public class MatchResult : BaseEntity
{
    public DateTime MatchDate { get; set; }
    public int MatchweekNumber { get; set; }
    public int Season { get; set; }             // e.g. 2025
    public string? Venue { get; set; }
    public string? Referee { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    public int HomeScore { get; set; }
    public int AwayScore { get; set; }

    /// <summary>Free-text match summary, hat-tricks, red cards, etc.</summary>
    public string? Notes { get; set; }

    public Guid? DivisionId { get; set; }

    // ── Home team ─────────────────────────────────────────────────────────────
    //
    // Nullable because a bracket exists before its teams do: the semi-final is scheduled while
    // the quarter-finals are still being played, and the slot is filled when a winner emerges.
    // Everywhere outside a knockout these are always set.
    public Guid? HomeTeamId { get; set; }
    public Team? HomeTeam { get; set; }

    // ── Away team ─────────────────────────────────────────────────────────────
    public Guid? AwayTeamId { get; set; }
    public Team? AwayTeam { get; set; }

    // ── Competition shape ─────────────────────────────────────────────────────

    /// <summary>Which part of the competition this fixture belongs to.</summary>
    public MatchStage Stage { get; set; } = MatchStage.League;

    /// <summary>Group this fixture was played in, for a group stage. "A", "B", and so on.</summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// How many teams were left at this point in the bracket, which is also its name: 2 is the
    /// final, 4 the semi-finals, 8 the quarter-finals. Storing the size rather than a label
    /// means the round names itself whatever the bracket is, and orders correctly by number.
    /// </summary>
    public int? KnockoutRoundSize { get; set; }

    /// <summary>The fixture the winner of this one goes on to. Null for a final.</summary>
    public Guid? NextMatchId { get; set; }
    public MatchResult? NextMatch { get; set; }

    /// <summary>Which side of the next fixture the winner takes.</summary>
    public MatchSlot? NextMatchSlot { get; set; }

    // ── Shootout ──────────────────────────────────────────────────────────────
    //
    // A knockout tie has to produce a winner, and amateur football settles a level score on
    // penalties rather than replaying. Null in a league, where a draw is a perfectly good result.
    public int? HomePenalties { get; set; }
    public int? AwayPenalties { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Division? Division { get; set; }
    public ICollection<MatchEvent> MatchEvents { get; set; } = new List<MatchEvent>();

    // ── Computed helpers (not persisted) ──────────────────────────────────────
    public string ScoreDisplay => Status == MatchStatus.Scheduled
        ? "vs"
        : $"{HomeScore} – {AwayScore}";
}
