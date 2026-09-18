using Microsoft.AspNetCore.Authorization;

namespace iDiski.Application.Common.Authorization;

/// <summary>
/// "May this person run this competition?" — satisfied by a SuperAdmin, or by a competition
/// admin who was assigned to this one.
///
/// It replaces DivisionOwnershipRequirement. A division is a collection of clubs and runs
/// nothing, so being assigned to one granted no coherent authority over what its clubs played.
/// </summary>
public class CompetitionOwnershipRequirement : IAuthorizationRequirement
{
    public Guid CompetitionId { get; }

    public CompetitionOwnershipRequirement(Guid competitionId)
    {
        CompetitionId = competitionId;
    }
}
