using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Competitions;
using iDiski.Application.Competitions.Commands;
using iDiski.Application.Matches.Commands;
using iDiski.Application.MatchResults;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Competitions;

/// <summary>
/// Who may be in a competition, and who may be drawn against whom.
///
/// The rule used to be "both clubs in the same division", because a division was the
/// competition. That refused the very thing a sponsor's cup is for. The rule is now "both
/// clubs entered in this competition", which allows the invited side and still refuses the
/// pairing nobody would want to explain.
///
/// The gender rule did not move, it got earlier: a women's club cannot be entered into a boys
/// competition at all, rather than being refused at the draw once somebody has already been
/// told they are playing.
/// </summary>
public class CompetitionEntryRulesTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CompetitionEntryRulesTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ── Entering ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AClubFromAnyDivisionCanBeEntered()
    {
        var elsewhere = await CompetitionScenario.ADivisionAsync(
            _fixture.DbContext, name: "Division Three");

        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, CompetitionFormat.Knockout);

        var guest = (await CompetitionScenario.ClubsAsync(_fixture.DbContext, elsewhere, 1))[0];

        var enter = async () => await Enter(cup, guest);

        await enter.Should().NotThrowAsync(
            "inviting clubs from other divisions is what this feature is for");

        var entered = await _fixture.DbContext.CompetitionEntries
            .AsNoTracking()
            .AnyAsync(e => e.CompetitionId == cup && e.TeamId == guest);

        entered.Should().BeTrue();
    }

    [Fact]
    public async Task AWomensClubCannotBeEnteredIntoABoysCompetition()
    {
        var boys = await CompetitionScenario.ADivisionAsync(
            _fixture.DbContext, Gender.Male, "U15 Boys");
        var women = await CompetitionScenario.ADivisionAsync(
            _fixture.DbContext, Gender.Female, "Women");

        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, CompetitionFormat.Knockout, gender: Gender.Male);

        var womensClub = (await CompetitionScenario.ClubsAsync(_fixture.DbContext, women, 1))[0];

        var enter = async () => await Enter(cup, womensClub);

        var thrown = await enter.Should().ThrowAsync<InvalidOperationException>();

        // Checked by the branch rather than by both words, since "female" contains "male".
        thrown.Which.Message.Should().Contain("female");
        thrown.Which.Message.Should().Contain("cannot be entered");
    }

    [Fact]
    public async Task AClubCannotBeEnteredTwice()
    {
        var (_, cup, clubs) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.Knockout, 2);

        var again = async () => await Enter(cup, clubs[0]);

        var thrown = await again.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain("already entered");
    }

    [Fact]
    public async Task AClubCannotBeEnteredOnceTheCompetitionIsDrawn()
    {
        var (divisionId, cup, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await Generate(cup);

        var latecomer = (await CompetitionScenario.ClubsAsync(
            _fixture.DbContext, divisionId, 1))[0];

        var enter = async () => await Enter(cup, latecomer);

        // They would hold a place with no fixtures, which reads as a bug to everybody who
        // sees it.
        var thrown = await enter.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain("already been drawn");
    }

    // ── Being drawn against somebody ──────────────────────────────────────────

    [Fact]
    public async Task AFixtureMadeByHandBelongsToItsCompetition()
    {
        var (_, cup, clubs) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 2);

        var id = await CreateFixture(cup, clubs[0], clubs[1]);

        var match = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .FirstAsync(m => m.Id == id);

        // The competition and nothing else. Even here, where both clubs share a division,
        // the fixture belongs to what is being played rather than to where they play it.
        match.CompetitionId.Should().Be(cup);
    }

    [Fact]
    public async Task TwoClubsFromDifferentDivisionsCanBeDrawnWhenBothAreEntered()
    {
        // The pairing the old rule refused outright, and the reason for the whole change.
        var first = await CompetitionScenario.ADivisionAsync(
            _fixture.DbContext, name: "Division One");
        var elsewhere = await CompetitionScenario.ADivisionAsync(
            _fixture.DbContext, name: "Division Three");

        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, CompetitionFormat.Knockout);

        var ours = (await CompetitionScenario.ClubsAsync(_fixture.DbContext, first, 1))[0];
        var theirs = (await CompetitionScenario.ClubsAsync(_fixture.DbContext, elsewhere, 1))[0];

        await CompetitionScenario.EnterAsync(_fixture.DbContext, cup, new[] { ours, theirs });

        var id = await CreateFixture(cup, ours, theirs);

        var match = await _fixture.DbContext.MatchResults.AsNoTracking()
            .FirstAsync(m => m.Id == id);

        // It belongs to the competition, and to neither division. There is no third answer
        // to give for a tie between clubs from two of them.
        match.CompetitionId.Should().Be(cup);
    }

    [Fact]
    public async Task AClubThatIsNotEnteredCannotBeDrawn()
    {
        var (divisionId, cup, clubs) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 2);

        // In the same division, and still not in this competition — which is exactly the case
        // "both clubs share a division" used to wave through.
        var outsider = (await CompetitionScenario.ClubsAsync(
            _fixture.DbContext, divisionId, 1))[0];

        var create = async () => await CreateFixture(cup, clubs[0], outsider);

        var thrown = await create.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain("not entered");
    }

    // ── Generating never reaches past the entry list ──────────────────────────

    [Fact]
    public async Task GeneratingOneCompetitionNeverReachesIntoAnother()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 8);

        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, CompetitionFormat.Knockout, name: "Top Four");

        // Only half the division is in it.
        await CompetitionScenario.EnterAsync(_fixture.DbContext, cup, clubs.Take(4));

        await Generate(cup);

        // Read the ties first and flatten them here: an array built inside the query has no
        // SQL to be translated into.
        var ties = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == cup)
            .Select(m => new { m.HomeTeamId, m.AwayTeamId })
            .ToListAsync();

        var drawn = ties
            .SelectMany(t => new[] { t.HomeTeamId, t.AwayTeamId })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .ToList();

        drawn.Should().NotBeEmpty();
        drawn.Should().NotIntersectWith(clubs.Skip(4));
    }

    // ── The smallest competition of each kind ─────────────────────────────────

    [Fact]
    public async Task ALeagueOfTwoIsASingleFixture()
    {
        var (_, competitionId, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 2);

        await Generate(competitionId);

        var fixtures = await FixturesFor(competitionId);

        fixtures.Should().HaveCount(1);
        fixtures.Should().OnlyContain(m => m.Stage == MatchStage.League);
    }

    [Fact]
    public async Task AKnockoutOfTwoIsJustAFinal()
    {
        var (_, competitionId, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.Knockout, 2);

        await Generate(competitionId);

        var fixtures = await FixturesFor(competitionId);

        fixtures.Should().HaveCount(1);
        (fixtures[0].KnockoutRoundSize ?? 0).Should().Be(2);
        fixtures[0].NextMatchId.Should().BeNull();
    }

    [Fact]
    public async Task ACompetitionWithOneEntrantIsRefused()
    {
        var (_, competitionId, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 1);

        var generate = async () => await Generate(competitionId);

        await generate.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task Enter(Guid competitionId, Guid teamId) =>
        new EnterTeamCommandHandler(_fixture.DbContext).Handle(
            new EnterTeamCommand(competitionId, teamId), CancellationToken.None);

    private Task<Guid> CreateFixture(Guid competitionId, Guid home, Guid away) =>
        new CreateMatchResultCommandHandler(_fixture.DbContext).Handle(
            new CreateMatchResultCommand(
                CompetitionId: competitionId,
                MatchDate: new DateTime(2040, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                MatchweekNumber: 1,
                HomeTeamId: home,
                AwayTeamId: away,
                Venue: null,
                Referee: null),
            CancellationToken.None);

    private Task<GenerateFixturesResult> Generate(Guid competitionId) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                IsHomeAndAway: false,
                StartDate: new DateTime(2040, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7),
            CancellationToken.None);

    private async Task<List<MatchResult>> FixturesFor(Guid competitionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId)
            .ToListAsync();
    }
}
