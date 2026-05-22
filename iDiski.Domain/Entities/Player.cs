namespace iDiski.Domain.Entities;

public enum PlayerPosition
{
    // Goalkeeper
    GK,  // Goalkeeper

    // Defenders
    CB,  // Center Back
    SW,  // Sweeper
    RB,  // Right Back
    LB,  // Left Back
    RWB, // Right Wing-Back
    LWB, // Left Wing-Back

    // Midfielders
    CDM, // Defensive Midfielder
    CM,  // Central Midfielder
    CAM, // Attacking Midfielder
    RM,  // Right Midfielder
    LM,  // Left Midfielder

    // Forwards
    ST,  // Striker
    CF,  // Center Forward
    RW,  // Right Winger
    LW   // Left Winger
}

public enum PreferredFoot
{
    Right,
    Left,
    Both
}

public class Player : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }

    /// <summary>Detailed player biography for "Player of the Month" features (max 2000 chars).</summary>
    public string? Bio { get; set; }

    public DateTime DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public int JerseyNumber { get; set; }
    public PlayerPosition Position { get; set; }
    public PreferredFoot PreferredFoot { get; set; } = PreferredFoot.Right;
    public bool IsActive { get; set; } = true;

    /// <summary>Optional: minutes played, goals, assists etc. can be a separate Stat entity later.</summary>
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }

    // ── FK ───────────────────────────────────────────────────────────────────
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<MatchEvent> MatchEvents { get; set; } = new List<MatchEvent>();
    public ICollection<Suspension> Suspensions { get; set; } = new List<Suspension>();
}
