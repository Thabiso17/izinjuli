using MediatR;

namespace iDiski.Application.Divisions.Queries;

public record GetDivisionsQuery(int? Season = null, bool? IsActive = null) : IRequest<IReadOnlyList<DivisionDto>>;
