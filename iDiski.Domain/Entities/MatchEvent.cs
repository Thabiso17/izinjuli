namespace iDiski.Domain.Entities;

public class MatchEvent : BaseEntity
{
    public Guid MatchId { get; set; }
    public MatchResult Match { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public EventType EventType { get; set; }
    public int Minute { get; set; }
    public string? AdditionalInfo { get; set; }
}

public enum EventType
{
    Goal = 0,
    Assist = 1,
    YellowCard = 2,
    RedCard = 3,
    OwnGoal = 4,
    Substitution = 5
}
