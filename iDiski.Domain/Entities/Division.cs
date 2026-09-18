namespace iDiski.Domain.Entities;

/// <summary>
/// A pool of teams — "U17 Boys, 2026" — rather than a competition in its own right.
///
/// What those teams play is a <see cref="Competition"/>, and there can be several at once: a
/// league running all season alongside a cup for eight of them and a sponsor's tournament that
/// invites clubs from elsewhere.
/// </summary>
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

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();

    // No competitions here. A division is the clubs; what they play is a competition, and a
    // competition is contested by clubs from wherever it invited them.
}

public enum Gender
{
    Male = 0,
    Female = 1,
    Mixed = 2
}
