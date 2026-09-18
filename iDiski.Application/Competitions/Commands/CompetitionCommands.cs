using iDiski.Application.Common.Authorization;
using iDiski.Domain.Entities;
using MediatR;

namespace iDiski.Application.Competitions.Commands;

/// <summary>
/// Starts a competition in a division: its league, a cup for some of its clubs, a sponsor's
/// tournament. Scoped to the division it is created in.
/// </summary>
public sealed record CreateCompetitionCommand : IRequest<Guid>, IRequireDivisionAccess
{
    public Guid DivisionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public int Season { get; init; }
    public CompetitionFormat Format { get; init; } = CompetitionFormat.League;

    /// <summary>
    /// How many clubs are meant to play. Null for "however many are entered", which is the
    /// ordinary case for a league; eight for a top-eight cup, whatever the division holds.
    /// </summary>
    public int? MaxTeams { get; init; }

    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Enter every club in the division straight away. What a league wants, and a reasonable
    /// starting point for a cup the organiser then trims down.
    /// </summary>
    public bool EnterAllDivisionTeams { get; init; } = true;
}

public sealed record UpdateCompetitionCommand : IRequest<Unit>, IRequireCompetitionAccess
{
    public Guid CompetitionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public CompetitionFormat Format { get; init; } = CompetitionFormat.League;
    public int? MaxTeams { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record DeleteCompetitionCommand(Guid CompetitionId)
    : IRequest<Unit>, IRequireCompetitionAccess;

/// <summary>
/// Puts a club in a competition. The club may come from another division — that is the point —
/// but not from one of a different gender.
/// </summary>
public sealed record EnterTeamCommand(Guid CompetitionId, Guid TeamId)
    : IRequest<Unit>, IRequireCompetitionAccess;

public sealed record WithdrawTeamCommand(Guid CompetitionId, Guid TeamId)
    : IRequest<Unit>, IRequireCompetitionAccess;
