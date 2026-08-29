using System.Security.Claims;
using iDiski.Domain.Enums;

namespace iDiski.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool HasRole(Role role);
    bool IsSuperAdmin { get; }

    /// <summary>The raw claims principal, for handing off to IAuthorizationService.AuthorizeAsync.</summary>
    ClaimsPrincipal? User { get; }
}
