using MediatR;

namespace iDiski.Application.Suspensions.Queries;

public record GetActiveSuspensionsQuery(Guid? DivisionId = null) : IRequest<IReadOnlyList<SuspensionDto>>;
