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
        var divisionId = await CreateDivisionWithTeamsAsync(teamCount);

        var result = await Generate(divisionId, homeAndAway);

        result.FixturesGenerated.Should().Be(expectedFixtures);
        result.MatchweeksCreated.Should().Be(expectedMatchweeks);

        var stored = await FixturesFor(divisionId);
        stored.Should().HaveCount(expectedFixtures, "the summary has to match what was written");
    }

    [Fact]
    public async Task EveryPairMeetsExactlyOnce_InASingleRound()
    {
        var divisionId = await CreateDivisionWithTeamsAsync(6);

        await Generate(divisionId, homeAndAway: false);

        var fixtures = await FixturesFor(divisionId);
        var pairings = fixtures
            .Select(f => Unordered(f.HomeTeamId, f.AwayTeamId))
            .ToList();

        pairings.Should().OnlyHaveUniqueItems("a single round is every pair meeting once");
        pairings.Should().HaveCount(15, "six teams make fifteen pairs");
    }

    [Fact]
    public async Task TheReturnFixtureSwapsTheVenue()
    {
        var divisionId = await CreateDivisionWithTeamsAsync(4);

        await Generate(divisionId, homeAndAway: true);

        var fixtures = await FixturesFor(divisionId);

        // Ordered this time: A v B and B v A are different fixtures, and both must exist.
        var ordered = fixtures.Select(f => (f.HomeTeamId, f.AwayTeamId)).ToList();
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
        var divisionId = await CreateDivisionWithTeamsAsync(teamCount);

        await Generate(divisionId, homeAndAway: true);

        var fixtures = await FixturesFor(divisionId);

        foreach (var week in fixtures.GroupBy(f => f.MatchweekNumber))
        {
            var playing = week
                .SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId })
                .ToList();

            playing.Should().OnlyHaveUniqueItems(
                $"matchweek {week.Key} has a club playing two fixtures at once");
        }
    }

    [Fact]
    public async Task AnOddNumberOfTeams_RestsOneRatherThanInventingAnOpponent()
    {
        var divisionId = await CreateDivisionWithTeamsAsync(5);

        await Generate(divisionId, homeAndAway: false);

        var fixtures = await FixturesFor(divisionId);

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
        var divisionId = await CreateDivisionWithTeamsAsync(4);
        var start = new DateTime(2026, 2, 7, 0, 0, 0, DateTimeKind.Utc);

        await Generate(divisionId, homeAndAway: false, start: start, daysBetween: 14);

        var fixtures = await FixturesFor(divisionId);
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
    public async Task EveryFixtureCarriesTheDivisionAndSeasonItWasAskedFor()
    {
        var divisionId = await CreateDivisionWithTeamsAsync(4);

        await Generate(divisionId, homeAndAway: true, season: 2029);

        var fixtures = await FixturesFor(divisionId);

        fixtures.Should().OnlyContain(f => f.DivisionId == divisionId);
        fixtures.Should().OnlyContain(f => f.Season == 2029);
        fixtures.Should().OnlyContain(f => f.Status == MatchStatus.Scheduled);
        fixtures.Should().OnlyContain(f => f.HomeScore == 0 && f.AwayScore == 0,
            "a generated fixture has not been played yet");
    }

    [Fact]
    public async Task ADivisionWithOneTeam_IsRefused()
    {
        var divisionId = await CreateDivisionWithTeamsAsync(1);

        var generate = async () => await Generate(divisionId, homeAndAway: false);

        await generate.Should().ThrowAsync<ValidationException>(
            "a division needs two clubs before it has a fixture to play");
    }

    [Fact]
    public async Task ADivisionThatDoesNotExist_IsRefused()
    {
        var generate = async () => await Generate(Guid.NewGuid(), homeAndAway: false);

        await generate.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GeneratingTwice_DoublesTheFixtures_WhichIsWorthKnowing()
    {
        var divisionId = await CreateDivisionWithTeamsAsync(4);

        await Generate(divisionId, homeAndAway: false);
        await Generate(divisionId, homeAndAway: false);

        var fixtures = await FixturesFor(divisionId);

        // Documenting rather than endorsing: the command appends, it does not replace, so a
        // second click leaves the division with two copies of its season. Nothing in the API or
        // the admin screen warns about that. Worth a guard, but changing it is a decision about
        // behaviour rather than a bug fix, so this pins what it does today.
        fixtures.Should().HaveCount(12);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<GenerateFixturesResult> Generate(
        Guid divisionId,
        bool homeAndAway,
        DateTime? start = null,
        int daysBetween = 7,
        int season = 2026) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                divisionId,
                season,
                homeAndAway,
                start ?? new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                daysBetween),
            CancellationToken.None);

    private async Task<List<MatchResult>> FixturesFor(Guid divisionId) =>
        await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.DivisionId == divisionId)
            .ToListAsync();

    private async Task<Guid> CreateDivisionWithTeamsAsync(int teamCount)
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

        for (var i = 0; i < teamCount; i++)
        {
            _fixture.DbContext.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                Name = $"Club {i + 1}",
                ShortCode = TestIds.Code("G"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return divisionId;
    }

    private static (Guid, Guid) Unordered(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}
