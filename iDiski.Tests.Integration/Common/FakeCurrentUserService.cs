using System;
using System.Linq;
using System.Security.Claims;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Enums;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// Stands in for the signed-in user. Hand-written rather than mocked: the roles here have to
/// agree with the UserRoles rows the test seeds, because the ownership handlers read the
/// database rather than the claims, and a mock makes that easy to get silently wrong.
/// </summary>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    private readonly Role[] _roles;

    public FakeCurrentUserService(Guid? userId, params Role[] roles)
    {
        UserId = userId;
        _roles = roles;
    }

    /// <summary>Nobody signed in.</summary>
    public static FakeCurrentUserService Anonymous() => new(null);

    public Guid? UserId { get; }

    public string? Email => UserId is null ? null : $"{UserId}@test.com";

    public bool IsAuthenticated => UserId is not null;

    public bool HasRole(Role role) => _roles.Contains(role);

    public bool IsSuperAdmin => HasRole(Role.SuperAdmin);

    public ClaimsPrincipal? User
    {
        get
        {
            if (UserId is null) return null;

            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, UserId.Value.ToString()) }
                    .Concat(_roles.Select(r => new Claim(ClaimTypes.Role, r.ToString()))),
                authenticationType: "Test");

            return new ClaimsPrincipal(identity);
        }
    }
}

/// <summary>Audit writes are not the subject of these tests, so they go nowhere.</summary>
public sealed class NoOpAuditService : IAuditService
{
    public System.Threading.Tasks.Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string description,
        string? oldValues = null,
        string? newValues = null,
        System.Threading.CancellationToken cancellationToken = default)
        => System.Threading.Tasks.Task.CompletedTask;
}
