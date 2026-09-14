using iDiski.Domain.Entities;

namespace iDiski.Domain.Services;

/// <summary>How far through its own life a competition is.</summary>
public enum CompetitionStatus
{
    /// <summary>Nothing has been played. The fixtures may not even be drawn yet.</summary>
    NotStarted = 0,

    /// <summary>Under way: some of it played, some of it still to come.</summary>
    InProgress = 1,

    /// <summary>Settled. A league has played everything; a cup has played its final.</summary>
    Completed = 2
}

/// <summary>
/// Whether a competition has finished, worked out from its fixtures rather than stored.
///
/// Derived on purpose. The alternative is a flag somebody ticks when a season ends, and a flag
/// like that is wrong the moment anybody forgets — which, for the one screen a visitor uses to
/// tell a live competition from one that ended two years ago, is worse than not having it.
/// This cannot drift: it is a function of the results themselves.
///
/// Pure, with no dependencies, so it is the same answer everywhere it is asked and can be
/// tested without a database.
/// </summary>
public static class CompetitionProgress
{
    /// <param name="played">Fixtures with a result.</param>
    /// <param name="pending">
    /// Fixtures still expected to be played — scheduled, in progress, or postponed. A cancelled
    /// fixture is deliberately neither: it will never be played, so it must not hold a
    /// competition open forever.
    /// </param>
    /// <param name="finalPlayed">
    /// Whether the fixture nothing follows — the final — has a result. Meaningless for a
    /// league, which has no final, and ignored there.
    /// </param>
    public static CompetitionStatus Status(
        CompetitionFormat format,
        int played,
        int pending,
        bool finalPlayed)
    {
        // Nothing played is nothing started, whether the draw has been made or not. A cup whose
        // bracket is drawn up in full on day one would otherwise read as under way before
        // anybody has kicked a ball, which is the whole point of drawing it in advance.
        if (played == 0) return CompetitionStatus.NotStarted;

        // A cup ends when the final is played, and only then. Counting fixtures instead would
        // call it finished the moment nothing was left scheduled — which is true of a bracket
        // for as long as the round before it is unplayed, since the next round has no teams in
        // it yet and no date anybody is waiting on.
        if (format is CompetitionFormat.Knockout or CompetitionFormat.GroupAndKnockout)
            return finalPlayed ? CompetitionStatus.Completed : CompetitionStatus.InProgress;

        // A league has no final: it is over when there is nothing left to play.
        return pending == 0 ? CompetitionStatus.Completed : CompetitionStatus.InProgress;
    }

    /// <summary>
    /// Whether this is a competition a reader would call current: on now, or still to come.
    /// The distinction the divisions page is actually filtering on.
    /// </summary>
    public static bool IsCurrent(CompetitionStatus status) =>
        status != CompetitionStatus.Completed;
}
