using iDiski.Domain.Entities;

namespace iDiski.Application.Divisions;

/// <param name="Format">
/// How the competition is played — league, knockout, or groups then a knockout. Required
/// rather than defaulted on purpose: an optional argument cannot be omitted inside an
/// expression tree, so a projection that forgets it fails to compile instead of quietly
/// telling every screen that a cup is a league.
/// </param>
public record DivisionDto(
    Guid Id,
    string Name,
    string ShortCode,
    int Season,
    CompetitionFormat Format,
    string? AgeGroup,
    string? Gender,
    bool IsActive,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description,
    int TeamCount,
    int MatchCount
);
