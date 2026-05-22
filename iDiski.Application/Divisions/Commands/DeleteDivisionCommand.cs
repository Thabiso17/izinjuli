using MediatR;

namespace iDiski.Application.Divisions.Commands;

public record DeleteDivisionCommand(Guid Id) : IRequest<Unit>;
