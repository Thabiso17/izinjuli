namespace iDiski.Application.Divisions;

/// <summary>
/// A pool of teams rather than a competition.
///
/// It used to carry a format and a status of its own, because a division was the competition.
/// Those moved to <c>CompetitionDto</c> when a division became able to run several at once: its
/// league can be halfway through while its cup has already been won, and no single status can
/// honestly describe both.
/// </summary>
/// <param name="CompetitionCount">
/// How many competitions are being run from this division. The number a reader needs to know
/// there is more here than a table.
/// </param>
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
    int MatchCount,
    int CompetitionCount
);
