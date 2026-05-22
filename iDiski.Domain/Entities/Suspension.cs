namespace iDiski.Domain.Entities;

public class Suspension : BaseEntity
{
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MatchesSuspended { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AppliedByUser { get; set; }
}
