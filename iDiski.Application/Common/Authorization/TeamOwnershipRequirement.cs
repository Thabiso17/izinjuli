using Microsoft.AspNetCore.Authorization;

namespace iDiski.Application.Common.Authorization;

public class TeamOwnershipRequirement : IAuthorizationRequirement
{
    public Guid TeamId { get; set; }

    public TeamOwnershipRequirement(Guid teamId)
    {
        TeamId = teamId;
    }
}
