namespace iDiski.Application.Common.Authorization;

/// <summary>
/// Implemented by commands/queries scoped to a single division. Picked up by
/// AuthorizationBehaviour, which checks the requester against DivisionOwnershipRequirement
/// for DivisionId (SuperAdmin always passes; DivisionAdmin must be assigned to it).
/// </summary>
public interface IRequireDivisionAccess
{
    Guid DivisionId { get; }
}

/// <summary>
/// Implemented by commands/queries scoped to a single team. Picked up by
/// AuthorizationBehaviour, which checks the requester against TeamOwnershipRequirement
/// for TeamId (SuperAdmin always passes; DivisionAdmin passes via the team's division;
/// TeamAdmin must be directly assigned to the team).
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
/// Implemented by commands scoped to a single fixture, whose scope is not in the request
/// payload — a score update carries only the match id.
///
/// A fixture has no division of its own: it belongs to a competition, and a competition is
/// contested by clubs from wherever it invited them. So the scope comes from the two clubs
/// playing, and AuthorizationBehaviour lets through anybody who administers the division of
/// either of them. That is what keeps a division admin able to record their own club's
/// results, including in a cup tie against a club from another division — both sides'
/// administrators can enter that score, which is the honest reading of a match that belongs
/// to neither division.
///
/// A fixture with no clubs yet — an unplayed semi-final — resolves to nobody, leaving it to a
/// SuperAdmin. Nothing to record there anyway until a winner arrives.
/// </summary>
public interface IRequireMatchAccess
{
    Guid MatchId { get; }
}

// Competitions have no ownership marker. They belong to no division, so there is nothing to
// resolve them to: creating and running one is SuperAdmin work, enforced by policy on the
// controller rather than by scope here.
