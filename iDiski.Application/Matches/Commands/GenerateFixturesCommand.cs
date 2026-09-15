using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using iDiski.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Matches.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// GENERATE FIXTURES (Round-Robin)
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Draws up a competition: a round-robin, a bracket, or groups feeding a bracket.
///
/// Entrants come from the competition's entry list rather than from a division's membership,
/// which is what lets twelve of a division's twenty play a cup while all twenty play the
/// league, and lets clubs invited from elsewhere play alongside them.
/// </summary>
/// <param name="CompetitionId">The competition to draw up.</param>
/// <param name="IsHomeAndAway">True for home-and-away (2 rounds), false for single round-robin</param>
/// <param name="StartDate">Date of first matchweek</param>
/// <param name="DaysBetweenMatchweeks">
/// Days between each matchweek. Zero is allowed, and is what a weekend tournament needs: every
/// round played on the same day.
/// </param>
/// <param name="GroupCount">How many groups to split the teams into. Group stages only.</param>
/// <param name="TeamsAdvancingPerGroup">
/// How many of each group go through to the bracket. The organiser chooses; two is the usual.
/// </param>
/// <param name="ReplaceExisting">
/// Whether to clear the division's existing fixtures for this season first.
///
/// Generating appends, so without this a second click — an impatient double-click included —
/// leaves the division holding two complete copies of its season, with nothing on screen
/// saying so. The command now refuses outright when fixtures already exist, and this is the
/// deliberate way to say "yes, start the season again".
/// </param>
public sealed record GenerateFixturesCommand(
    Guid     CompetitionId,
    bool     IsHomeAndAway,
    DateTime StartDate,
    int      DaysBetweenMatchweeks = 7,
    bool     ReplaceExisting = false,
    int?     GroupCount = null,
    int      TeamsAdvancingPerGroup = 2
) : IRequest<GenerateFixturesResult>;

public sealed record GenerateFixturesResult(
    int FixturesGenerated,
    int MatchweeksCreated,
    DateTime FirstMatchDate,
    DateTime LastMatchDate
);

public sealed class GenerateFixturesCommandValidator : AbstractValidator<GenerateFixturesCommand>
{
    public GenerateFixturesCommandValidator()
    {
        RuleFor(x => x.CompetitionId).NotEmpty();
        RuleFor(x => x.StartDate).GreaterThan(DateTime.MinValue);
        // Zero is deliberate: a tournament played out over a single weekend puts every round
        // on the same day, which the old minimum of one day made impossible to express.
        RuleFor(x => x.DaysBetweenMatchweeks)
            .InclusiveBetween(0, 30)
            .WithMessage("Days between matchweeks must be between 0 and 30");

        RuleFor(x => x.GroupCount)
            .GreaterThanOrEqualTo(2)
            .When(x => x.GroupCount.HasValue)
            .WithMessage("A group stage needs at least two groups");

        RuleFor(x => x.TeamsAdvancingPerGroup)
            .GreaterThanOrEqualTo(1)
            .WithMessage("At least one team has to come out of each group");
    }
}

public sealed class GenerateFixturesCommandHandler
    : IRequestHandler<GenerateFixturesCommand, GenerateFixturesResult>
{
    private readonly ILeagueDbContext _db;

    public GenerateFixturesCommandHandler(ILeagueDbContext db) => _db = db;

    public async Task<GenerateFixturesResult> Handle(
        GenerateFixturesCommand request,
        CancellationToken cancellationToken)
    {
        // 1. The competition, and who is in it
        var competition = await _db.Competitions
            .Include(c => c.Entries)
                .ThenInclude(e => e.Team)
            .FirstOrDefaultAsync(c => c.Id == request.CompetitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Competition), request.CompetitionId);

        // 2. Entrants are the entry list, not the division's membership. That distinction is
        //    the whole point: a division of twenty can run a cup for eight of them.
        var teams = competition.Entries
            .Select(e => e.Team)
            .OrderBy(t => t.Name)
            .ToList();

        if (teams.Count < 2)
        {
            throw new Common.Exceptions.ValidationException(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("CompetitionId",
                        $"{competition.Name} has {teams.Count} "
                        + (teams.Count == 1 ? "entrant" : "entrants")
                        + ". Enter at least two teams before drawing it up.")
                });
        }

        // 3. Refuse to append a second draw on top of the first
        var existing = await _db.MatchResults
            .Where(m => m.CompetitionId == request.CompetitionId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            if (!request.ReplaceExisting)
            {
                throw new InvalidOperationException(
                    $"{competition.Name} already has {existing.Count} fixtures. "
                    + "Generating again would add a second copy of it. "
                    + "Choose to replace the existing fixtures if you meant to start again.");
            }

            // Replacing is for a season that has not started. Once results are in, those
            // fixtures are a record of matches that were actually played, and regenerating
            // would throw away the scores along with the events and standings built on them.
            var played = existing
                .Where(m => m.Status != MatchStatus.Scheduled)
                .ToList();

            if (played.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{played.Count} of {competition.Name}'s fixtures have already been played "
                    + "or are in progress, so it cannot be drawn again. "
                    + "Remove those results first if the schedule really has to change.");
            }

            _db.MatchResults.RemoveRange(existing);
        }

        // 4. Build the fixtures the way this competition is played. The season is the
        //    competition's own rather than an argument, so fixtures can no longer be written
        //    into a year nothing reads.
        var fixtures = competition.Format switch
        {
            CompetitionFormat.Knockout => KnockoutBracket.Build(
                teams.Select(t => t.Id).ToList(),
                competition.DivisionId,
                competition.Season,
                request.StartDate,
                request.DaysBetweenMatchweeks),

            CompetitionFormat.GroupAndKnockout => GenerateGroupsAndBracket(
                teams, competition, request),

            _ => GenerateRoundRobinFixtures(
                teams,
                competition.DivisionId,
                competition.Season,
                request.IsHomeAndAway,
                request.StartDate,
                request.DaysBetweenMatchweeks),
        };

        // The builders set the division, which every fixture still carries; the competition is
        // stamped here in one place so no builder can forget it.
        foreach (var fixture in fixtures)
            fixture.CompetitionId = competition.Id;

        // 5. Save to database
        _db.MatchResults.AddRange(fixtures);
        await _db.SaveChangesAsync(cancellationToken);

        // 6. Return summary
        var matchweeks = fixtures.Select(f => f.MatchweekNumber).Distinct().Count();
        var firstDate = fixtures.Min(f => f.MatchDate);
        var lastDate = fixtures.Max(f => f.MatchDate);

        return new GenerateFixturesResult(
            FixturesGenerated: fixtures.Count,
            MatchweeksCreated: matchweeks,
            FirstMatchDate: firstDate,
            LastMatchDate: lastDate
        );
    }

    /// <summary>
    /// Groups first, then the bracket the qualifiers will play.
    ///
    /// The bracket is created empty. Nobody knows who is coming out of the groups yet, but the
    /// organiser still wants the shape of the knockout settled when the competition is drawn
    /// up — how many rounds, on what dates, at what venue.
    /// </summary>
    private static List<MatchResult> GenerateGroupsAndBracket(
        List<Team> teams,
        Competition competition,
        GenerateFixturesCommand request)
    {
        var groupCount = request.GroupCount ?? 2;

        if (groupCount > teams.Count / 2)
        {
            throw new Common.Exceptions.ValidationException(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("GroupCount",
                        $"{teams.Count} teams cannot fill {groupCount} groups — each group "
                        + "needs at least two.")
                });
        }

        // Deal the teams out one at a time rather than cutting the list into blocks, so an
        // uneven entry list spreads the extra teams across groups instead of loading the last.
        var groups = Enumerable.Range(0, groupCount).Select(_ => new List<Team>()).ToList();
        for (var i = 0; i < teams.Count; i++)
            groups[i % groupCount].Add(teams[i]);

        var smallestGroup = groups.Min(g => g.Count);
        if (request.TeamsAdvancingPerGroup > smallestGroup)
        {
            throw new Common.Exceptions.ValidationException(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("TeamsAdvancingPerGroup",
                        $"Cannot advance {request.TeamsAdvancingPerGroup} from every group when "
                        + $"the smallest holds {smallestGroup}.")
                });
        }

        var fixtures = new List<MatchResult>();

        for (var i = 0; i < groups.Count; i++)
        {
            var name = ((char)('A' + i)).ToString();

            var groupFixtures = GenerateRoundRobinFixtures(
                groups[i],
                competition.DivisionId,
                competition.Season,
                request.IsHomeAndAway,
                request.StartDate,
                request.DaysBetweenMatchweeks);

            foreach (var fixture in groupFixtures)
            {
                fixture.Stage = MatchStage.Group;
                fixture.GroupName = name;
            }

            fixtures.AddRange(groupFixtures);
        }

        // The bracket picks up the matchweek after the longest group finishes, so a group with
        // fewer teams does not leave the knockout starting on top of another group's last round.
        var groupMatchweeks = fixtures.Count == 0 ? 0 : fixtures.Max(f => f.MatchweekNumber);
        var qualifiers = groups.Count * request.TeamsAdvancingPerGroup;

        var bracketStart = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc)
            .AddDays(groupMatchweeks * request.DaysBetweenMatchweeks);

        fixtures.AddRange(KnockoutBracket.BuildEmpty(
            qualifiers,
            competition.DivisionId,
            competition.Season,
            bracketStart,
            request.DaysBetweenMatchweeks,
            groupMatchweeks + 1));

        return fixtures;
    }

    /// <summary>
    /// Generates round-robin fixtures using the "circle method" algorithm.
    /// Reference: https://en.wikipedia.org/wiki/Round-robin_tournament#Scheduling_algorithm
    /// </summary>
    private static List<MatchResult> GenerateRoundRobinFixtures(
        List<Team> teams,
        Guid divisionId,
        int season,
        bool isHomeAndAway,
        DateTime startDate,
        int daysBetweenMatchweeks)
    {
        var fixtures = new List<MatchResult>();
        var teamList = teams.Select(t => t.Id).ToList();
        var teamCount = teamList.Count;

        // If odd number of teams, add a "BYE" (Guid.Empty represents bye)
        if (teamCount % 2 != 0)
        {
            teamList.Add(Guid.Empty);
            teamCount++;
        }

        var rounds = teamCount - 1; // Number of rounds in single round-robin
        var matchesPerRound = teamCount / 2;

        // Ensure startDate is UTC to satisfy PostgreSQL
        var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);

        // Generate first round (single round-robin)
        for (int round = 0; round < rounds; round++)
        {
            var matchweek = round + 1;
            var matchDate = utcStartDate.AddDays(round * daysBetweenMatchweeks);

            // Generate matches for this round using rotation algorithm
            for (int match = 0; match < matchesPerRound; match++)
            {
                int home, away;

                if (match == 0)
                {
                    // First match: team 0 is always at home
                    home = 0;
                    away = teamCount - 1;
                }
                else
                {
                    home = match;
                    away = teamCount - 1 - match;
                }

                var homeTeamId = teamList[home];
                var awayTeamId = teamList[away];

                // Skip if either team is a BYE
                if (homeTeamId == Guid.Empty || awayTeamId == Guid.Empty)
                    continue;

                // Create fixture
                fixtures.Add(new MatchResult
                {
                    HomeTeamId = homeTeamId,
                    AwayTeamId = awayTeamId,
                    DivisionId = divisionId,
                    Season = season,
                    MatchweekNumber = matchweek,
                    MatchDate = matchDate,
                    Status = MatchStatus.Scheduled,
                    HomeScore = 0,
                    AwayScore = 0
                });
            }

            // Rotate teams (except team 0 which stays fixed)
            if (round < rounds - 1)
            {
                var temp = teamList[teamCount - 1];
                for (int i = teamCount - 1; i > 1; i--)
                {
                    teamList[i] = teamList[i - 1];
                }
                teamList[1] = temp;
            }
        }

        // If home-and-away, create return fixtures (reverse home/away)
        if (isHomeAndAway)
        {
            var firstRoundFixtures = fixtures.ToList();
            var secondRoundStart = rounds + 1;

            foreach (var fixture in firstRoundFixtures)
            {
                var returnMatchweek = fixture.MatchweekNumber + rounds;
                var returnMatchDate = utcStartDate.AddDays(returnMatchweek * daysBetweenMatchweeks - daysBetweenMatchweeks);

                fixtures.Add(new MatchResult
                {
                    HomeTeamId = fixture.AwayTeamId, // Swap home/away
                    AwayTeamId = fixture.HomeTeamId,
                    DivisionId = divisionId,
                    Season = season,
                    MatchweekNumber = returnMatchweek,
                    MatchDate = returnMatchDate,
                    Status = MatchStatus.Scheduled,
                    HomeScore = 0,
                    AwayScore = 0
                });
            }
        }

        return fixtures;
    }
}
