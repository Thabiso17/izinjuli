using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Matches.Commands;
using iDiski.Application.MatchResults;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Matches;

/// <summary>
/// Winners moving through a bracket.
///
/// Without this a knockout is a list of fixtures rather than a tournament: the semi-final would
/// sit empty however many quarter-finals were played. Entering the result is the only moment
/// the next round can be filled in, so it happens there rather than waiting for somebody to
/// notice and do it by hand.
/// </summary>
public class BracketProgressionTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public BracketProgressionTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task WinningPutsYouInTheNextFixture_OnTheSideYouWereAllotted()
    {
        var (divisionId, _) = await ABracketOf(4);
        var semiFinal = await AFixtureIn(divisionId, roundSize: 4);

        await Record(semiFinal, home: 3, away: 1);

        var final = await Reload(semiFinal.NextMatchId!.Value);
        var expectedSlot = semiFinal.NextMatchSlot == MatchSlot.Away
            ? final.AwayTeamId
            : final.HomeTeamId;

        expectedSlot.Should().Be(semiFinal.HomeTeamId, "the home side won three one");
    }

    [Fact]
    public async Task TheLosingSideGoesNoFurther()
    {
        var (divisionId, _) = await ABracketOf(4);
        var semiFinal = await AFixtureIn(divisionId, roundSize: 4);

        await Record(semiFinal, home: 0, away: 2);

        var final = await Reload(semiFinal.NextMatchId!.Value);

        new[] { final.HomeTeamId, final.AwayTeamId }
            .Should().NotContain(semiFinal.HomeTeamId);
    }

    [Fact]
    public async Task ALevelTieWithNoShootout_IsRefused()
    {
        var (divisionId, _) = await ABracketOf(4);
        var semiFinal = await AFixtureIn(divisionId, roundSize: 4);

        var record = async () => await Record(semiFinal, home: 1, away: 1);

        // Somebody has to go through. Accepting a draw here would leave the next round
        // permanently half-empty with nothing saying why.
        await record.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AShootoutSettlesIt()
    {
        var (divisionId, _) = await ABracketOf(4);
        var semiFinal = await AFixtureIn(divisionId, roundSize: 4);

        // Level after ninety minutes, and the away side wins on penalties — so the side that
        // did not win on the day is the one that goes through.
        await Record(semiFinal, home: 1, away: 1, homePenalties: 4, awayPenalties: 5);

        var final = await Reload(semiFinal.NextMatchId!.Value);

        new[] { final.HomeTeamId, final.AwayTeamId }
            .Should().Contain(semiFinal.AwayTeamId);
        new[] { final.HomeTeamId, final.AwayTeamId }
            .Should().NotContain(semiFinal.HomeTeamId);
    }

    [Fact]
    public async Task AShootoutThatIsAlsoLevel_IsRefused()
    {
        var (divisionId, _) = await ABracketOf(4);
        var semiFinal = await AFixtureIn(divisionId, roundSize: 4);

        var record = async () =>
            await Record(semiFinal, home: 1, away: 1, homePenalties: 3, awayPenalties: 3);

        await record.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CorrectingAMistypedResult_ReplacesTheTeamItPutThrough()
    {
        var (divisionId, _) = await ABracketOf(4);
        var semiFinal = await AFixtureIn(divisionId, roundSize: 4);

        await Record(semiFinal, home: 2, away: 0);
        await Record(semiFinal, home: 0, away: 2);

        var final = await Reload(semiFinal.NextMatchId!.Value);

        // The wrong club must not be left standing in the next round alongside the right one.
        new[] { final.HomeTeamId, final.AwayTeamId }
            .Should().Contain(semiFinal.AwayTeamId);
        new[] { final.HomeTeamId, final.AwayTeamId }
            .Should().NotContain(semiFinal.HomeTeamId);
    }

    [Fact]
    public async Task BothSemiFinals_FillTheFinal()
    {
        var (divisionId, _) = await ABracketOf(4);

        var semiFinals = await FixturesIn(divisionId, roundSize: 4);
        semiFinals.Should().HaveCount(2);

        foreach (var semiFinal in semiFinals)
            await Record(semiFinal, home: 1, away: 0);

        var final = (await FixturesIn(divisionId, roundSize: 2)).Single();

        final.HomeTeamId.Should().NotBeNull();
        final.AwayTeamId.Should().NotBeNull();
        final.HomeTeamId.Should().NotBe(final.AwayTeamId,
            "two different winners cannot be the same club");
    }

    [Fact]
    public async Task TheFinalHasNowhereToSendAWinner_AndThatIsFine()
    {
        var (divisionId, _) = await ABracketOf(4);

        foreach (var semiFinal in await FixturesIn(divisionId, roundSize: 4))
            await Record(semiFinal, home: 1, away: 0);

        var final = (await FixturesIn(divisionId, roundSize: 2)).Single();

        var record = async () => await Record(final, home: 2, away: 1);

        await record.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AFixtureStillWaitingOnTheRoundBefore_CannotHaveAResult()
    {
        var (divisionId, _) = await ABracketOf(4);
        var final = (await FixturesIn(divisionId, roundSize: 2)).Single();

        var record = async () => await Record(final, home: 1, away: 0);

        // Nobody has reached it yet, so a score here would belong to nobody.
        await record.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ALeagueMatchIsUntouchedByAnyOfThis()
    {
        var divisionId = await ALeagueOf(4);
        var fixture = (await FixturesFor(divisionId)).First();

        // A draw is a perfectly good league result, and there is nowhere for anyone to advance.
        var record = async () => await Record(fixture, home: 1, away: 1);

        await record.Should().NotThrowAsync();

        var stored = await Reload(fixture.Id);
        stored.HomeScore.Should().Be(1);
        stored.AwayScore.Should().Be(1);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task Record(
        MatchResult match,
        int home,
        int away,
        int? homePenalties = null,
        int? awayPenalties = null) =>
        new UpdateMatchScoreCommandHandler(_fixture.DbContext).Handle(
            new UpdateMatchScoreCommand(
                match.Id, home, away, MatchStatus.Completed, null,
                homePenalties, awayPenalties),
            CancellationToken.None);

    private async Task<MatchResult> Reload(Guid id)
    {
        _fixture.DbContext.ChangeTracker.Clear();
        return await _fixture.DbContext.MatchResults.AsNoTracking().FirstAsync(m => m.Id == id);
    }

    private async Task<List<MatchResult>> FixturesFor(Guid divisionId) =>
        await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.DivisionId == divisionId)
            .ToListAsync();

    private async Task<List<MatchResult>> FixturesIn(Guid divisionId, int roundSize) =>
        (await FixturesFor(divisionId))
            .Where(m => m.KnockoutRoundSize == roundSize)
            .ToList();

    private async Task<MatchResult> AFixtureIn(Guid divisionId, int roundSize) =>
        (await FixturesIn(divisionId, roundSize)).First();

    private async Task<(Guid DivisionId, List<MatchResult> Fixtures)> ABracketOf(int teamCount)
    {
        var divisionId = await CreateDivisionAsync(CompetitionFormat.Knockout, teamCount);
        await Generate(divisionId);
        return (divisionId, await FixturesFor(divisionId));
    }

    private async Task<Guid> ALeagueOf(int teamCount)
    {
        var divisionId = await CreateDivisionAsync(CompetitionFormat.League, teamCount);
        await Generate(divisionId);
        return divisionId;
    }

    private Task<GenerateFixturesResult> Generate(Guid divisionId) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                divisionId,
                Season: 2026,
                IsHomeAndAway: false,
                StartDate: new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 1),
            CancellationToken.None);

    private async Task<Guid> CreateDivisionAsync(CompetitionFormat format, int teamCount)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"Progression {teamCount}",
            ShortCode = TestIds.Code("BP"),
            Season = 2026,
            Gender = Gender.Male,
            IsActive = true,
            Format = format,
            CreatedAt = now,
        });

        for (var i = 0; i < teamCount; i++)
        {
            _fixture.DbContext.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                Name = $"Side {i + 1}",
                ShortCode = TestIds.Code("S"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return divisionId;
    }
}
