namespace iDiski.Domain.Entities;

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
    public ICollection<MatchResult> Matches { get; set; } = new List<MatchResult>();
}

public enum Gender
{
    Male = 0,
    Female = 1,
    Mixed = 2
}
