using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Matches.Commands;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Matches;

/// <summary>
/// Fixture generation, which had no tests at all despite being the one command that writes
/// dozens of rows from a single click. A round-robin that is subtly wrong does not fail — it
/// produces a season that looks plausible and is not, and by the time anyone notices, results
/// have been entered against it.
///
/// So these check the properties a schedule has to have rather than just the row count: every
/// pair meets, nobody plays twice in the same week, and an odd number of teams produces a bye
/// rather than a fixture against nobody.
/// </summary>
public class GenerateFixturesTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public GenerateFixturesTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Theory]
    // n teams, single round: every pair meets once, over n-1 matchweeks.
    [InlineData(4, false, 6, 3)]
    [InlineData(6, false, 15, 5)]
    // Home and away doubles both.
    [InlineData(4, true, 12, 6)]
    // An odd count sits one team out each week: 5 teams still play 10 fixtures, over 5 weeks
    // rather than 4, because somebody rests each time.
    [InlineData(5, false, 10, 5)]
    [InlineData(3, false, 3, 3)]
    public async Task TheRightNumberOfFixturesOverTheRightNumberOfMatchweeks(
        int teamCount, bool homeAndAway, int expectedFixtures, int expectedMatchweeks)
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(teamCount);

        var result = await Generate(competitionId, homeAndAway);

        result.FixturesGenerated.Should().Be(expectedFixtures);
        result.MatchweeksCreated.Should().Be(expectedMatchweeks);

        var stored = await FixturesFor(competitionId);
        stored.Should().HaveCount(expectedFixtures, "the summary has to match what was written");
    }

    [Fact]
    public async Task EveryPairMeetsExactlyOnce_InASingleRound()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(6);

        await Generate(competitionId, homeAndAway: false);

        var fixtures = await FixturesFor(competitionId);
        var pairings = fixtures
            // A league fixture always names both sides; only a bracket slot can be empty.
            .Select(f => Unordered(f.HomeTeamId!.Value, f.AwayTeamId!.Value))
            .ToList();

        pairings.Should().OnlyHaveUniqueItems("a single round is every pair meeting once");
        pairings.Should().HaveCount(15, "six teams make fifteen pairs");
    }

    [Fact]
    public async Task TheReturnFixtureSwapsTheVenue()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(4);

        await Generate(competitionId, homeAndAway: true);

        var fixtures = await FixturesFor(competitionId);

        // Ordered this time: A v B and B v A are different fixtures, and both must exist.
        var ordered = fixtures
            .Select(f => (Home: f.HomeTeamId!.Value, Away: f.AwayTeamId!.Value))
            .ToList();
        ordered.Should().OnlyHaveUniqueItems("nobody should host the same opponent twice");

        foreach (var (home, away) in ordered)
        {
            ordered.Should().Contain((away, home),
                "a home-and-away season owes every club the return fixture");
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task NobodyPlaysTwiceInTheSameMatchweek(int teamCount)
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(teamCount);

        await Generate(competitionId, homeAndAway: true);

        var fixtures = await FixturesFor(competitionId);

        foreach (var week in fixtures.GroupBy(f => f.MatchweekNumber))
        {
            var playing = week
                .SelectMany(f => new[] { f.HomeTeamId!.Value, f.AwayTeamId!.Value })
                .ToList();

            playing.Should().OnlyHaveUniqueItems(
                $"matchweek {week.Key} has a club playing two fixtures at once");
        }
    }

    [Fact]
    public async Task AnOddNumberOfTeams_RestsOneRatherThanInventingAnOpponent()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(5);

        await Generate(competitionId, homeAndAway: false);

        var fixtures = await FixturesFor(competitionId);

        // The algorithm pads an odd squad list with Guid.Empty to stand for the bye. If that
        // placeholder ever reached the database it would be a fixture against a team that does
        // not exist, which is a foreign key violation at best and a phantom result at worst.
        fixtures.Should().NotContain(f => f.HomeTeamId == Guid.Empty || f.AwayTeamId == Guid.Empty);

        // Five clubs over five weeks, two fixtures a week: each sits out exactly once.
        foreach (var week in fixtures.GroupBy(f => f.MatchweekNumber))
        {
            week.Should().HaveCount(2, $"one of the five clubs rests in matchweek {week.Key}");
        }
    }

    [Fact]
    public async Task MatchweeksAreSpacedByTheRequestedInterval()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(4);
        var start = new DateTime(2026, 2, 7, 0, 0, 0, DateTimeKind.Utc);

        await Generate(competitionId, homeAndAway: false, start: start, daysBetween: 14);

        var fixtures = await FixturesFor(competitionId);
        var weeks = fixtures
            .GroupBy(f => f.MatchweekNumber)
            .OrderBy(g => g.Key)
            .ToList();

        for (var i = 0; i < weeks.Count; i++)
        {
            var expected = start.AddDays(i * 14);
            weeks[i].Should().OnlyContain(f => f.MatchDate == expected,
                $"matchweek {weeks[i].Key} should fall a fortnight after the one before it");
        }
    }

    [Fact]
    public async Task EveryFixtureCarriesTheCompetitionAndTheSeasonItBelongsTo()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(4);

        await Generate(competitionId, homeAndAway: true);

        var fixtures = await FixturesFor(competitionId);

        fixtures.Should().OnlyContain(f => f.CompetitionId == competitionId);

        // The season is the competition's own rather than an argument, so a fixture can no
        // longer be written into a year nothing reads.
        fixtures.Should().OnlyContain(f => f.Season == 2026);
        fixtures.Should().OnlyContain(f => f.Status == MatchStatus.Scheduled);
        fixtures.Should().OnlyContain(f => f.HomeScore == 0 && f.AwayScore == 0,
            "a generated fixture has not been played yet");
    }

    [Fact]
    public async Task ACompetitionWithOneEntrant_IsRefused()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(1);

        var generate = async () => await Generate(competitionId, homeAndAway: false);

        await generate.Should().ThrowAsync<ValidationException>(
            "a competition needs two entrants before it has a fixture to play");
    }

    [Fact]
    public async Task ADivisionThatDoesNotExist_IsRefused()
    {
        var generate = async () => await Generate(Guid.NewGuid(), homeAndAway: false);

        await generate.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GeneratingTwice_IsRefused()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(4);
        await Generate(competitionId, homeAndAway: false);

        var again = async () => await Generate(competitionId, homeAndAway: false);

        // Generating appends, so without this guard an impatient double-click left the
        // division holding two complete copies of its season, with nothing on screen saying so.
        await again.Should().ThrowAsync<InvalidOperationException>();

        var fixtures = await FixturesFor(competitionId);
        fixtures.Should().HaveCount(6, "the second attempt must not have added anything");
    }

    [Fact]
    public async Task Replacing_ClearsTheOldSeasonRatherThanAppendingToIt()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(4);
        await Generate(competitionId, homeAndAway: false);

        var result = await Generate(competitionId, homeAndAway: true, replaceExisting: true);

        result.FixturesGenerated.Should().Be(12);

        var fixtures = await FixturesFor(competitionId);
        fixtures.Should().HaveCount(12,
            "replacing means the division is left with one season, the new one");
    }

    [Fact]
    public async Task Replacing_IsRefusedOnceAnyResultIsIn()
    {
        var competitionId = await CreateCompetitionWithTeamsAsync(4);
        await Generate(competitionId, homeAndAway: false);

        // One fixture gets played.
        var played = await _fixture.DbContext.MatchResults
            .FirstAsync(m => m.CompetitionId == competitionId);
        played.Status = MatchStatus.Completed;
        played.HomeScore = 2;
        played.AwayScore = 1;
        await _fixture.DbContext.SaveChangesAsync();

        var regenerate = async () =>
            await Generate(competitionId, homeAndAway: false, replaceExisting: true);

        // Those fixtures are now a record of matches that happened. Regenerating would throw
        // away the scores, and the events and standings built on them.
        await regenerate.Should().ThrowAsync<InvalidOperationException>();

        var fixtures = await FixturesFor(competitionId);
        fixtures.Should().HaveCount(6);
        fixtures.Should().Contain(m => m.Id == played.Id && m.HomeScore == 2);
    }

    [Fact]
    public async Task AnotherCompetitionForTheSameClubs_IsNotBlockedByTheGuard()
    {
        var league = await CreateCompetitionWithTeamsAsync(4);
        await Generate(league, homeAndAway: false);

        // The guard is about drawing the same competition twice, not about a set of clubs only
        // ever playing one thing. They have a league and a cup, which is the point.
        var cup = await ASecondCompetitionForTheSameClubs(league, CompetitionFormat.League);

        var next = await Generate(cup, homeAndAway: false);

        next.FixturesGenerated.Should().Be(6);
        (await FixturesFor(league)).Should().HaveCount(6);
        (await FixturesFor(cup)).Should().HaveCount(6);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<GenerateFixturesResult> Generate(
        Guid competitionId,
        bool homeAndAway,
        DateTime? start = null,
        int daysBetween = 7,
        bool replaceExisting = false) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                homeAndAway,
                start ?? new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                daysBetween,
                replaceExisting),
            CancellationToken.None);

    /// <summary>
    /// A second competition in the same division, with the same clubs entered. What a season
    /// actually looks like: a league and a cup running side by side.
    /// </summary>
    private async Task<Guid> ASecondCompetitionForTheSameClubs(
        Guid existingCompetitionId, CompetitionFormat format)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var source = await _fixture.DbContext.Competitions
            .AsNoTracking()
            .FirstAsync(c => c.Id == existingCompetitionId);

        var entrants = await _fixture.DbContext.CompetitionEntries
            .AsNoTracking()
            .Where(e => e.CompetitionId == existingCompetitionId)
            .Select(e => e.TeamId)
            .ToListAsync();

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Competitions.Add(new Competition
        {
            Id = id,
            Name = $"Second {TestIds.Code("N")}",
            ShortCode = TestIds.Code("S"),
            Season = source.Season,
            Format = format,
            IsActive = true,
            CreatedAt = now,
        });

        foreach (var teamId in entrants)
        {
            _fixture.DbContext.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = id,
                TeamId = teamId,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return id;
    }

    private async Task<List<MatchResult>> FixturesFor(Guid competitionId) =>
        await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId)
            .ToListAsync();

    private async Task<Guid> CreateCompetitionWithTeamsAsync(int teamCount)
    {
        // Tests in this class share a database, and a previous failure can leave entities
        // tracked as Added that the next save would resubmit.
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"Generated {teamCount}",
            ShortCode = TestIds.Code("GF"),
            Season = 2026,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = now,
        });

        // The division is the pool; the competition is what gets played. Everybody in the
        // division is entered, which is what this file assumes throughout.
        var competitionId = Guid.NewGuid();

        _fixture.DbContext.Competitions.Add(new Competition
        {
            Id = competitionId,
            Name = $"Competition {TestIds.Code("N")}",
            ShortCode = TestIds.Code("GF"),
            Season = 2026,
            Format = CompetitionFormat.League,
            IsActive = true,
            CreatedAt = now,
        });

        for (var i = 0; i < teamCount; i++)
        {
            var teamId = Guid.NewGuid();

            _fixture.DbContext.Teams.Add(new Team
            {
                Id = teamId,
                Name = $"Club {i + 1}",
                ShortCode = TestIds.Code("G"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = now,
            });

            _fixture.DbContext.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = competitionId,
                TeamId = teamId,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return competitionId;
    }

    private static (Guid, Guid) Unordered(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}
