using iDiski.Application.Common.Authorization;
using iDiski.Domain.Entities;
using MediatR;

namespace iDiski.Application.Competitions.Commands;

/// <summary>
/// Starts a competition: a league, a cup, a sponsor's tournament. Only a SuperAdmin creates
/// one — there is nothing to scope it to before it exists, and its organisers are assigned
/// afterwards. Everything else about running it goes through IRequireCompetitionAccess.
/// </summary>
public sealed record CreateCompetitionCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public int Season { get; init; }
    public CompetitionFormat Format { get; init; } = CompetitionFormat.League;

    /// <summary>
    /// Who it is for. Checked against every club entered, so a women's side cannot end up in
    /// a boys competition.
    /// </summary>
    public Gender Gender { get; init; } = Gender.Male;

    /// <summary>Informational: "U17", "Open". Age has never been enforced at entry.</summary>
    public string? AgeGroup { get; init; }

    /// <summary>
    /// How many clubs are meant to play. Null for "however many are entered", which is the
    /// ordinary case for a league; eight for a top-eight cup, whatever the division holds.
    /// </summary>
    public int? MaxTeams { get; init; }

    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Optionally fill the entry list straight away with every club in this division — what a
    /// league wants, and a reasonable starting point for a cup the organiser then trims down.
    /// Null enters nobody, which is where a cup drawn from several divisions starts.
    ///
    /// A convenience, not a relationship: nothing about the competition remembers afterwards
    /// that the clubs arrived together.
    /// </summary>
    public Guid? EnterTeamsFromDivisionId { get; init; }
}

public sealed record UpdateCompetitionCommand : IRequest<Unit>, IRequireCompetitionAccess
{
    public Guid CompetitionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public CompetitionFormat Format { get; init; } = CompetitionFormat.League;
    public Gender Gender { get; init; } = Gender.Male;
    public string? AgeGroup { get; init; }
    public int? MaxTeams { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record DeleteCompetitionCommand(Guid CompetitionId)
    : IRequest<Unit>, IRequireCompetitionAccess;

/// <summary>
/// Puts a club in a competition. It may come from any division — that is the point — but not
/// from one whose gender differs from the competition's.
/// </summary>
public sealed record EnterTeamCommand(Guid CompetitionId, Guid TeamId)
    : IRequest<Unit>, IRequireCompetitionAccess;

public sealed record WithdrawTeamCommand(Guid CompetitionId, Guid TeamId)
    : IRequest<Unit>, IRequireCompetitionAccess;
