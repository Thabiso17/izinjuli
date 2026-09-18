using iDiski.Domain.Entities;
using iDiski.Domain.Services;

namespace iDiski.Application.Competitions;

/// <param name="Format">
/// How this competition is played. Required rather than defaulted on purpose: an optional
/// argument cannot be omitted inside an expression tree, so a projection that forgets it fails
/// to compile instead of quietly telling every screen that a cup is a league.
/// </param>
/// <param name="Gender">
/// Who it is for. Carried by the competition itself rather than read off a division, because
/// nothing runs a competition: it is what the entry list is checked against.
/// </param>
/// <param name="EntrantCount">
/// How many clubs are in it. Nothing else says: a competition is its entry list, and the clubs
/// in it may come from one division, from several, or from all of them.
/// </param>
/// <param name="MaxTeams">
/// How many clubs the organiser said would play, or null when they did not say. Shown beside
/// the entrant count so a half-filled cup reads as half-filled rather than as finished.
/// </param>
/// <param name="DivisionsRepresented">
/// How many different divisions the entrants are drawn from. One means a competition played
/// inside a single division — a league, usually; more than one means a cup contested across
/// them, which is worth saying plainly to a reader wondering why these clubs are meeting.
/// </param>
public record CompetitionDto(
    Guid Id,
    string Name,
    string ShortCode,
    int Season,
    CompetitionFormat Format,
    Gender Gender,
    string? AgeGroup,
    int? MaxTeams,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description,
    bool IsActive,
    int EntrantCount,
    int DivisionsRepresented,
    int MatchCount,
    int PlayedCount,
    int PendingCount,
    bool FinalPlayed
)
{
    /// <summary>
    /// Whether this competition has finished, worked out from its own results rather than
    /// stored. The rule lives in the domain so every screen agrees; see CompetitionProgress.
    ///
    /// It sits on the competition rather than the division because a division now runs several
    /// at once: its league can be halfway through while its cup has already been won.
    /// </summary>
    public CompetitionStatus Status =>
        CompetitionProgress.Status(Format, PlayedCount, PendingCount, FinalPlayed);
}

/// <summary>One club's place in a competition, and which division it plays in.</summary>
/// <param name="DivisionName">
/// The club's own division. There is no "invited" any more, because there is no host: a
/// competition is contested by whoever is entered, and saying which division each of them
/// comes from is the honest way to show a draw that spans several.
/// </param>
public record CompetitionEntrantDto(
    Guid TeamId,
    string TeamName,
    string ShortCode,
    string? LogoUrl,
    Guid? DivisionId,
    string? DivisionName
);
