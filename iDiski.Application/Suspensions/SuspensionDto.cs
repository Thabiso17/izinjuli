namespace iDiski.Application.Suspensions;

public record SuspensionDto(
    Guid Id,
    Guid PlayerId,
    string PlayerName,
    string TeamName,
    string Reason,
    DateTime StartDate,
    DateTime EndDate,
    int MatchesSuspended,
    bool IsActive
);
