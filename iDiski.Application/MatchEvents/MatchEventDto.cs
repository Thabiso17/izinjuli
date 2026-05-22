namespace iDiski.Application.MatchEvents;

public record MatchEventDto(
    Guid Id,
    Guid MatchId,
    Guid PlayerId,
    string PlayerName,
    string TeamName,
    string EventType,
    int Minute,
    string? AdditionalInfo
);
