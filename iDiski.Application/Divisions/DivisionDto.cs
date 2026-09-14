using iDiski.Domain.Entities;
using iDiski.Domain.Services;

namespace iDiski.Application.Divisions;

/// <param name="Format">
/// How the competition is played — league, knockout, or groups then a knockout. Required
/// rather than defaulted on purpose: an optional argument cannot be omitted inside an
/// expression tree, so a projection that forgets it fails to compile instead of quietly
/// telling every screen that a cup is a league.
/// </param>
/// <param name="PlayedCount">Fixtures with a result. Also what "12 of 30 played" is read from.</param>
/// <param name="PendingCount">
/// Fixtures still expected — scheduled, in progress or postponed. Cancelled ones are excluded,
/// because one abandoned fixture must not hold a season open for ever.
/// </param>
/// <param name="FinalPlayed">
/// Whether the tie nothing follows has been played. The three of these together are the whole
/// input to <see cref="Status"/>: counts a database can work out, rather than a judgement it
/// cannot.
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
    int MatchCount,
    int PlayedCount,
    int PendingCount,
    bool FinalPlayed
)
{
    /// <summary>
    /// Whether this competition has finished, worked out from its own results.
    ///
    /// Computed rather than stored, and computed here rather than in the projection, because
    /// the rule differs by format and none of it translates to SQL. What the query supplies is
    /// three numbers; what turns them into an answer is one function in the domain, so every
    /// screen agrees and none of them has its own idea of what "finished" means.
    ///
    /// <c>IsActive</c> is a different thing and stays a different thing: that is somebody
    /// deciding a division should be shown at all, not the competition having run its course.
    /// </summary>
    public CompetitionStatus Status =>
        CompetitionProgress.Status(Format, PlayedCount, PendingCount, FinalPlayed);
}
