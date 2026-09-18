using iDiski.Domain.Entities;
using iDiski.Domain.Services;

namespace iDiski.Application.Competitions;

/// <param name="Format">
/// How this competition is played. Required rather than defaulted on purpose: an optional
/// argument cannot be omitted inside an expression tree, so a projection that forgets it fails
/// to compile instead of quietly telling every screen that a cup is a league.
/// </param>
/// <param name="EntrantCount">
/// How many clubs are in it — which is not how many are in the division. A division of twenty
/// can run a cup for eight, and that is the number that matters here.
/// </param>
/// <param name="MaxTeams">
/// How many clubs the organiser said would play, or null when they did not say. Shown beside
/// the entrant count so a half-filled cup reads as half-filled rather than as finished.
/// </param>
/// <param name="ExternalEntrantCount">
/// How many of those come from outside the division running it. Worth saying plainly on a
/// sponsor's cup, where a reader would otherwise wonder why an unfamiliar club is in the draw.
/// </param>
public record CompetitionDto(
    Guid Id,
    Guid DivisionId,
    string DivisionName,
    string Name,
    string ShortCode,
    int Season,
    CompetitionFormat Format,
    int? MaxTeams,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Description,
    bool IsActive,
    int EntrantCount,
    int ExternalEntrantCount,
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

/// <summary>One club's place in a competition, and where they came from.</summary>
/// <param name="IsExternal">
/// True when this club plays in a different division from the one running the competition —
/// an invited side. Shown so an organiser can see at a glance who is a guest.
/// </param>
public record CompetitionEntrantDto(
    Guid TeamId,
    string TeamName,
    string ShortCode,
    string? LogoUrl,
    Guid? DivisionId,
    string? DivisionName,
    bool IsExternal
);
