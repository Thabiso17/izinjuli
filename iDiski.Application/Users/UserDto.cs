namespace iDiski.Application.Users;

/// <summary>
/// Basic user info for list views. Roles are included because a list of administrators that
/// does not say what anyone administers cannot be acted on — the alternative is a detail
/// request per row just to render the table.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<int> RoleIds
);

/// <summary>Detailed user info including roles and assignments (for edit views)</summary>
public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<int> RoleIds,
    IReadOnlyList<Guid> AssignedTeamIds,
    IReadOnlyList<Guid> AssignedCompetitionIds
);
