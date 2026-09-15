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
/// Implemented by commands scoped to a single fixture, whose division is not in the request
/// payload — a score update carries only the match id. AuthorizationBehaviour resolves the
/// fixture's division and applies DivisionOwnershipRequirement to it.
///
/// A fixture with no division resolves to Guid.Empty, which nobody but a SuperAdmin owns.
/// That is the safe direction: an orphaned fixture predating the division guard can still be
/// repaired, but only by somebody who can see the whole league.
/// </summary>
public interface IRequireMatchAccess
{
    Guid MatchId { get; }
}

/// <summary>
/// Implemented by commands scoped to a single competition. AuthorizationBehaviour resolves the
/// competition's owning division and applies DivisionOwnershipRequirement to it.
///
/// The owning division decides who administers a competition, not the entrants: a cup may
/// field clubs invited from three other divisions, and their administrators do not thereby get
/// a say in running it.
/// </summary>
public interface IRequireCompetitionAccess
{
    Guid CompetitionId { get; }
}
