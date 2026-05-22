namespace iDiski.Application.Divisions;

public record DivisionDto(
    Guid Id,
    string Name,
    string ShortCode,
    int Season,
    string? AgeGroup,
    string? Gender,
    bool IsActive,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description,
    int TeamCount,
    int MatchCount
);
