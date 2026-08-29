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
