namespace iDiski.Application.Teams;

public sealed record TeamDto(
    Guid   Id,
    string Name,
    string ShortCode,
    string? LogoUrl,
    int    Founded,
    string? HomeGround,
    string? City,
    string? PrimaryColour,
    string? SecondaryColour,
    int    PlayerCount,
    Guid?  DivisionId,
    string? DivisionName
);
