using iDiski.Application.Common.Exceptions;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Matches.Commands;

// ═════════════════════════════════════════════════════════════════════════════
// GENERATE FIXTURES (Round-Robin)
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Generates fixtures for a division using round-robin algorithm.
/// </summary>
/// <param name="DivisionId">The division to generate fixtures for</param>
/// <param name="Season">Season year (e.g., 2025)</param>
/// <param name="IsHomeAndAway">True for home-and-away (2 rounds), false for single round-robin</param>
/// <param name="StartDate">Date of first matchweek</param>
/// <param name="DaysBetweenMatchweeks">Days between each matchweek (default 7)</param>
public sealed record GenerateFixturesCommand(
    Guid     DivisionId,
    int      Season,
    bool     IsHomeAndAway,
    DateTime StartDate,
    int      DaysBetweenMatchweeks = 7
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
        RuleFor(x => x.DivisionId).NotEmpty();
        RuleFor(x => x.Season).GreaterThan(2000).LessThan(2100);
        RuleFor(x => x.StartDate).GreaterThan(DateTime.MinValue);
        RuleFor(x => x.DaysBetweenMatchweeks)
            .InclusiveBetween(1, 30)
            .WithMessage("Days between matchweeks must be between 1 and 30");
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
        // 1. Validate division exists
        var division = await _db.Divisions
            .Include(d => d.Teams)
            .FirstOrDefaultAsync(d => d.Id == request.DivisionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Division), request.DivisionId);

        // 2. Ensure division has at least 2 teams
        var teams = division.Teams.ToList();
        if (teams.Count < 2)
        {
            throw new Common.Exceptions.ValidationException(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("DivisionId", "Division must have at least 2 teams to generate fixtures")
                });
        }

        // 3. Generate round-robin fixtures
        var fixtures = GenerateRoundRobinFixtures(
            teams,
            division.Id,
            request.Season,
            request.IsHomeAndAway,
            request.StartDate,
            request.DaysBetweenMatchweeks);

        // 4. Save to database
        _db.MatchResults.AddRange(fixtures);
        await _db.SaveChangesAsync(cancellationToken);

        // 5. Return summary
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

        // Generate first round (single round-robin)
        for (int round = 0; round < rounds; round++)
        {
            var matchweek = round + 1;
            var matchDate = startDate.AddDays(round * daysBetweenMatchweeks);

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
                var returnMatchDate = startDate.AddDays(returnMatchweek * daysBetweenMatchweeks - daysBetweenMatchweeks);

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
