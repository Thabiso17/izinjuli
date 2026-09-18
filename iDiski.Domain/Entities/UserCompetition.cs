namespace iDiski.Domain.Entities;

/// <summary>
/// One person's assignment to one competition they help run.
///
/// The replacement for UserDivision. A competition admin may hold several of these — the
/// league and the cup, or three sponsors' tournaments — and the set of them is exactly what
/// they can touch. An organiser of the Nedbank Cup runs that and nothing else, however many
/// divisions its entrants are drawn from.
/// </summary>
public class UserCompetition : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; set; }
}
