using MediatR;

namespace iDiski.Application.Divisions.Queries;

public record GetDivisionByIdQuery(Guid Id) : IRequest<DivisionDto?>;
