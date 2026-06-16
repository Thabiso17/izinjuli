namespace iDiski.Application.Users;

/// <summary>Basic user info (for list views)</summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
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
    IReadOnlyList<Guid> AssignedDivisionIds
);
