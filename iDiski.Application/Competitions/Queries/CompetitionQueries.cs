using MediatR;

namespace iDiski.Application.Competitions.Queries;

/// <summary>
/// The competitions being run, optionally narrowed to one division or one season.
/// </summary>
public sealed record GetCompetitionsQuery(
    Guid? DivisionId = null,
    int? Season = null,
    bool? IsActive = null
) : IRequest<IReadOnlyList<CompetitionDto>>;

public sealed record GetCompetitionByIdQuery(Guid Id) : IRequest<CompetitionDto?>;

/// <summary>Who is in a competition, and which of them were invited from elsewhere.</summary>
public sealed record GetCompetitionEntrantsQuery(Guid CompetitionId)
    : IRequest<IReadOnlyList<CompetitionEntrantDto>>;
