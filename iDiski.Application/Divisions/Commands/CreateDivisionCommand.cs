using iDiski.Domain.Entities;
using MediatR;

namespace iDiski.Application.Divisions.Commands;

public record CreateDivisionCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string ShortCode { get; init; } = string.Empty;
    public int Season { get; init; }

    /// <summary>
    /// How the competition is played. Defaults to a league, which is what every division
    /// created before this existed is.
    /// </summary>
    public CompetitionFormat Format { get; init; } = CompetitionFormat.League;
    public string? AgeGroup { get; init; }
    public string? Gender { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Description { get; init; }
}
