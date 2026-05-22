using iDiski.Domain.Entities;

namespace iDiski.Application.Players;

public sealed record PlayerDto(
    Guid           Id,
    string         FirstName,
    string         LastName,
    string         FullName,
    string?        ProfileImageUrl,
    string?        Bio,
    DateTime       DateOfBirth,
    int            AgeYears,
    string?        Nationality,
    int            JerseyNumber,
    PlayerPosition Position,
    PreferredFoot  PreferredFoot,
    bool           IsActive,
    int            Goals,
    int            Assists,
    int            YellowCards,
    int            RedCards,
    Guid           TeamId,
    string         TeamName
);
