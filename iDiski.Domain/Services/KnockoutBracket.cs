using iDiski.Domain.Entities;

namespace iDiski.Domain.Services;

/// <summary>
/// Builds a knockout bracket: every round of it, at once, before anyone has played.
///
/// That is the point of a bracket — an organiser wants the whole thing on a wall on day one,
/// with kick-off times and venues for the final already set. So the later rounds are created
/// as empty slots, each earlier fixture pointing at the one its winner goes on to, and the
/// slots fill in as results arrive.
///
/// Byes are handled the way a real draw does it. A bracket holds a power of two, so when the
/// entry list falls short the strongest seeds skip the first round rather than play an
/// opponent who does not exist. A team with a bye is placed straight into the second round.
///
/// Pure: it is handed teams and hands back fixtures, with no database and no clock of its own.
/// </summary>
public static class KnockoutBracket
{
    /// <summary>
    /// A bracket for <paramref name="teamIds"/>, in seeding order — first entry is the top
    /// seed. Returns every fixture in every round, linked.
    /// </summary>
    /// <param name="teamIds">Entrants, best first. Two or more.</param>
    /// <param name="season">Season stamped on every fixture.</param>
    /// <param name="firstRoundDate">When the opening round is played.</param>
    /// <param name="daysBetweenRounds">
    /// Gap between rounds. Zero is allowed and is what a weekend tournament needs, where every
    /// round is played on the same day.
    /// </param>
    /// <param name="firstMatchweek">
    /// Matchweek number the opening round takes, so a bracket that follows a group stage
    /// carries on counting rather than starting again at one.
    /// </param>
    public static List<MatchResult> Build(
        IReadOnlyList<Guid> teamIds,
        int season,
        DateTime firstRoundDate,
        int daysBetweenRounds,
        int firstMatchweek = 1)
    {
        if (teamIds.Count < 2)
            throw new ArgumentException("A knockout needs at least two teams.", nameof(teamIds));

        var bracketSize = NextPowerOfTwo(teamIds.Count);
        var startDate = DateTime.SpecifyKind(firstRoundDate, DateTimeKind.Utc);

        // Round sizes from the opening round down to the final: 8, 4, 2 and so on.
        var roundSizes = new List<int>();
        for (var size = bracketSize; size >= 2; size /= 2)
            roundSizes.Add(size);

        // Every fixture up front, still empty, so each round can be linked to the next.
        var rounds = new List<List<MatchResult>>();

        for (var round = 0; round < roundSizes.Count; round++)
        {
            var size = roundSizes[round];
            var matches = new List<MatchResult>();

            for (var i = 0; i < size / 2; i++)
            {
                matches.Add(new MatchResult
                {
                    Id = Guid.NewGuid(),
                    Season = season,
                    Stage = MatchStage.Knockout,
                    KnockoutRoundSize = size,
                    MatchweekNumber = firstMatchweek + round,
                    MatchDate = startDate.AddDays(round * daysBetweenRounds),
                    Status = MatchStatus.Scheduled,
                    HomeScore = 0,
                    AwayScore = 0,
                });
            }

            rounds.Add(matches);
        }

        // Point each fixture at the one its winner goes on to. Two fixtures feed each fixture
        // in the round after, the first taking the home slot and the second the away slot.
        for (var round = 0; round < rounds.Count - 1; round++)
        {
            for (var i = 0; i < rounds[round].Count; i++)
            {
                var next = rounds[round + 1][i / 2];
                rounds[round][i].NextMatchId = next.Id;
                rounds[round][i].NextMatchSlot = i % 2 == 0 ? MatchSlot.Home : MatchSlot.Away;
            }
        }

        // Seed the opening round. SeedOrder puts the top two seeds at opposite ends, so they
        // can only meet in the final.
        var order = SeedOrder(bracketSize);
        var openingRound = rounds[0];
        var byes = new List<MatchResult>();

        for (var position = 0; position < bracketSize; position += 2)
        {
            var match = openingRound[position / 2];

            var homeSeed = order[position] - 1;
            var awaySeed = order[position + 1] - 1;

            var home = homeSeed < teamIds.Count ? teamIds[homeSeed] : (Guid?)null;
            var away = awaySeed < teamIds.Count ? teamIds[awaySeed] : (Guid?)null;

            if (home is not null && away is not null)
            {
                match.HomeTeamId = home;
                match.AwayTeamId = away;
                continue;
            }

            // A bye: one entrant and no opponent. There is no fixture to play, so the team is
            // put straight into the next round and this slot never becomes a fixture at all —
            // leaving it in would show the organiser a match nobody turns up to.
            var walkover = home ?? away;

            if (rounds.Count > 1 && match.NextMatchId is not null)
            {
                var next = rounds[1].First(m => m.Id == match.NextMatchId);
                if (match.NextMatchSlot == MatchSlot.Home) next.HomeTeamId = walkover;
                else next.AwayTeamId = walkover;
            }

            byes.Add(match);
        }

        foreach (var bye in byes) openingRound.Remove(bye);

        return rounds.SelectMany(r => r).ToList();
    }

    /// <summary>
    /// An empty bracket for <paramref name="entrantCount"/> qualifiers, every slot waiting.
    ///
    /// This is what a group stage feeds. Nobody knows who is coming until the groups finish,
    /// but the organiser still wants the shape of the knockout — how many rounds, on what
    /// dates — settled when the competition is drawn up.
    /// </summary>
    public static List<MatchResult> BuildEmpty(
        int entrantCount,
        int season,
        DateTime firstRoundDate,
        int daysBetweenRounds,
        int firstMatchweek = 1)
    {
        if (entrantCount < 2)
            throw new ArgumentException(
                "A knockout needs at least two qualifiers.", nameof(entrantCount));

        // Placeholder ids stand in for entrants so the rounds and links come out the same as a
        // seeded bracket; the slots are then cleared, leaving the shape without the names.
        var placeholders = Enumerable.Range(0, entrantCount).Select(_ => Guid.NewGuid()).ToList();

        var bracket = Build(
            placeholders, season, firstRoundDate, daysBetweenRounds, firstMatchweek);

        foreach (var match in bracket)
        {
            match.HomeTeamId = null;
            match.AwayTeamId = null;
        }

        return bracket;
    }

    /// <summary>The name a round of this size goes by.</summary>
    public static string RoundName(int roundSize) => roundSize switch
    {
        2 => "Final",
        4 => "Semi-final",
        8 => "Quarter-final",
        _ => $"Round of {roundSize}",
    };

    internal static int NextPowerOfTwo(int value)
    {
        var size = 2;
        while (size < value) size *= 2;
        return size;
    }

    /// <summary>
    /// Standard bracket seeding: 1 v 16, 8 v 9, 5 v 12 and so on, arranged so the two best
    /// seeds can only meet in the final and the strongest entrants get the byes.
    /// </summary>
    internal static int[] SeedOrder(int bracketSize)
    {
        var order = new List<int> { 1, 2 };

        while (order.Count < bracketSize)
        {
            var opponentOf = order.Count * 2 + 1;
            var next = new List<int>();

            foreach (var seed in order)
            {
                next.Add(seed);
                next.Add(opponentOf - seed);
            }

            order = next;
        }

        return order.ToArray();
    }
}
