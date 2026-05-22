namespace iDiski.Domain.Entities;

public enum MatchStatus
{
    Scheduled,
    InProgress,
    Completed,
    Postponed,
    Cancelled
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
    public Guid HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;

    // ── Away team ─────────────────────────────────────────────────────────────
    public Guid AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;

    // ── Navigation ────────────────────────────────────────────────────────────
    public Division? Division { get; set; }
    public ICollection<MatchEvent> MatchEvents { get; set; } = new List<MatchEvent>();

    // ── Computed helpers (not persisted) ──────────────────────────────────────
    public string ScoreDisplay => Status == MatchStatus.Scheduled
        ? "vs"
        : $"{HomeScore} – {AwayScore}";
}
