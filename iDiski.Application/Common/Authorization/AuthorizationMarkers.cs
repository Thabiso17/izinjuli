namespace iDiski.Application.Common.Authorization;

/// <summary>
/// Implemented by commands scoped to a single competition. Picked up by AuthorizationBehaviour,
/// which checks the requester against CompetitionOwnershipRequirement: a SuperAdmin always
/// passes, and a competition admin passes for the competitions they were assigned.
///
/// This is what running a competition means — its entry list, its draw, its results. A
/// [Authorize(Policy = "CanManageCompetitions")] attribute on the endpoint only asks whether
/// somebody is a competition admin at all, never which competitions.
/// </summary>
public interface IRequireCompetitionAccess
{
    Guid CompetitionId { get; }
}

/// <summary>
/// Implemented by commands/queries scoped to a single team. Picked up by
/// AuthorizationBehaviour, which checks the requester against TeamOwnershipRequirement for
/// TeamId: a SuperAdmin always passes, and a team admin must be directly assigned to the club.
///
/// A club's own administrators, and nobody else below SuperAdmin. Running a competition a club
/// is entered in does not come with the right to edit the club.
/// </summary>
public interface IRequireTeamAccess
{
    Guid TeamId { get; }
}

/// <summary>
/// Implemented by commands scoped to a single player whose owning team isn't already
/// part of the request payload. AuthorizationBehaviour resolves the player's TeamId and
/// applies the same TeamOwnershipRequirement check as IRequireTeamAccess.
/// </summary>
public interface IRequirePlayerAccess
{
    Guid PlayerId { get; }
}

/// <summary>
/// Implemented by commands scoped to a single fixture, whose competition is not in the request
/// payload — a score update carries only the match id. AuthorizationBehaviour resolves the
/// fixture's competition and applies CompetitionOwnershipRequirement to it.
///
/// Recording what happened in a match is part of running the competition it belongs to, so it
/// reaches the same people the draw does. A fixture that belongs to no competition resolves to
/// Guid.Empty, which nobody is assigned to, leaving it to a SuperAdmin — the safe direction for
/// a row nobody's scope covers.
/// </summary>
public interface IRequireMatchAccess
{
    Guid MatchId { get; }
}

// There is no division marker. A division is a collection of clubs: it runs nothing, so being
// assigned to one granted no coherent authority. Divisions themselves are SuperAdmin work, and
// the clubs inside them answer to their own administrators.
