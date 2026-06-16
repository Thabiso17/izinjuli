using Microsoft.AspNetCore.Authorization;

namespace iDiski.Application.Common.Authorization;

public class DivisionOwnershipRequirement : IAuthorizationRequirement
{
    public Guid DivisionId { get; set; }

    public DivisionOwnershipRequirement(Guid divisionId)
    {
        DivisionId = divisionId;
    }
}
