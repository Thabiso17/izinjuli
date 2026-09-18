using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Competitions;
using iDiski.Application.Competitions.Commands;
using iDiski.Application.Matches.Commands;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Competitions;

/// <summary>
/// Starting, changing and abandoning a competition.
///
/// This is the surface an organiser actually drives — the one that did not exist before, and
/// so had no tests at all. The interesting cases are the guards, because every one of them
/// exists to stop a competition ending up in a state that reads as a bug on the public page:
/// an entrant with no fixtures, a bracket sitting in a league, a table counting somebody who
/// withdrew.
/// </summary>
public class CompetitionCrudTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CompetitionCrudTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ── Starting one ──────────────────────────────────────────────────────────

    [Fact]
    public async Task StartingALeagueEntersTheWholeDivision()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 6);

        var id = await Create(divisionId, enterAll: true);

        (await EntrantsOf(id)).Should().HaveCount(6, "a league is played by the whole division");
    }

    [Fact]
    public async Task StartingACupEntersNobodyUntilTheOrganiserChooses()
    {
        // The case the old model could not express: twenty clubs in the division, and the
        // organiser picks who is in the cup.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 6);

        var id = await Create(divisionId, enterAll: false, format: CompetitionFormat.Knockout);

        (await EntrantsOf(id)).Should().BeEmpty();

        await Enter(id, clubs[0]);
        await Enter(id, clubs[1]);

        (await EntrantsOf(id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task TwoCompetitionsInOneSeasonCannotShareAShortCode()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);

        var code = TestIds.Code("DUP");
        await Create(divisionId, enterAll: false, shortCode: code);

        var again = async () => await Create(divisionId, enterAll: false, shortCode: code);

        // Said as a sentence rather than left to the unique index.
        (await again.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain(code);
    }

    [Fact]
    public async Task TheSameShortCodeIsRefusedAcrossDivisionsToo()
    {
        // It used to be unique within a division and season, so two divisions could each call
        // theirs "LGE". A competition belongs to no division now, so the season is the only
        // scope left for a code to be unique within.
        var first = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var second = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);

        var code = TestIds.Code("SH");

        await Create(first, enterAll: false, shortCode: code);
        var shared = async () => await Create(second, enterAll: false, shortCode: code);

        await shared.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TheSameShortCodeIsFineInAnotherSeason()
    {
        // Next year's cup is the same cup, and reusing its code is the point of a code.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var code = TestIds.Code("YR");

        await Create(divisionId, enterAll: false, shortCode: code);
        var nextYear = async () =>
            await Create(divisionId, enterAll: false, shortCode: code, season: 2041);

        await nextYear.Should().NotThrowAsync();
    }

    // ── How many clubs it holds ───────────────────────────────────────────────

    [Fact]
    public async Task ACupThatHoldsFourRefusesAFifth()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 5);

        var id = await Create(
            divisionId, enterAll: false, format: CompetitionFormat.Knockout, maxTeams: 4);

        foreach (var club in clubs.Take(4)) await Enter(id, club);

        var fifth = async () => await Enter(id, clubs[4]);

        // Otherwise the bracket is quietly built around a number nobody meant.
        (await fifth.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("holds 4");

        (await EntrantsOf(id)).Should().HaveCount(4);
    }

    [Fact]
    public async Task ADivisionTooBigForTheCupIsNotSilentlyTrimmed()
    {
        // Which four of the six get dropped is the organiser's decision, so "enter everybody"
        // into a competition that cannot hold everybody is refused rather than guessed at.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 6);

        var overfull = async () => await Create(
            divisionId, enterAll: true, format: CompetitionFormat.Knockout, maxTeams: 4);

        (await overfull.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("choose who plays");
    }

    [Fact]
    public async Task ALeagueWithNoNumberTakesEverybody()
    {
        // The ordinary case, and the reason the number is optional: a league is played by
        // whoever is in the division.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 7);

        var id = await Create(divisionId, enterAll: true);

        (await EntrantsOf(id)).Should().HaveCount(7);
    }

    [Fact]
    public async Task ACompetitionCannotBeCutBelowTheClubsAlreadyInIt()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 4);

        var id = await Create(divisionId, enterAll: true, maxTeams: 4);

        var shrink = async () =>
            await Update(id, name: "Now Smaller", format: CompetitionFormat.League, maxTeams: 2);

        // Two of the four would be left holding a place in something that no longer has room
        // for them, and picking which two is not ours to do.
        (await shrink.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("Withdraw somebody first");
    }

    // ── Changing one ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RenamingIsFineOnceItHasBeenDrawn()
    {
        var (_, id, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await Generate(id);

        await Update(id, name: "Renamed Mid-Season", format: CompetitionFormat.League);

        var after = await _fixture.DbContext.Competitions.AsNoTracking()
            .FirstAsync(c => c.Id == id);

        after.Name.Should().Be("Renamed Mid-Season");
    }

    [Fact]
    public async Task HowItIsPlayedCannotChangeOnceItHasBeenDrawn()
    {
        // A league's fixtures make no sense read as a bracket, so the shape is fixed the
        // moment there are fixtures to misread.
        var (_, id, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await Generate(id);

        var reshape = async () =>
            await Update(id, name: "Now A Cup", format: CompetitionFormat.Knockout);

        (await reshape.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("already been drawn up");
    }

    [Fact]
    public async Task HowItIsPlayedCanChangeBeforeItIsDrawn()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var id = await Create(divisionId, enterAll: false);

        await Update(id, name: "Second Thoughts", format: CompetitionFormat.GroupAndKnockout);

        var after = await _fixture.DbContext.Competitions.AsNoTracking()
            .FirstAsync(c => c.Id == id);

        after.Format.Should().Be(CompetitionFormat.GroupAndKnockout);
    }

    // ── Entering and withdrawing ──────────────────────────────────────────────

    [Fact]
    public async Task AClubCannotBeEnteredTwice()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 2);
        var id = await Create(divisionId, enterAll: false);

        await Enter(id, clubs[0]);
        var again = async () => await Enter(id, clubs[0]);

        // Two places in the draw and two rows in the table, otherwise.
        (await again.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("already entered");
    }

    [Fact]
    public async Task NobodyJoinsACompetitionThatHasAlreadyBeenDrawn()
    {
        var (divisionId, id, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await Generate(id);

        var latecomer = (await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 1))[0];
        var late = async () => await Enter(id, latecomer);

        // A place and no fixtures reads as a bug to everyone who sees it.
        (await late.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("no fixtures");
    }

    [Fact]
    public async Task AClubCanWithdrawBeforeTheDraw()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 3);
        var id = await Create(divisionId, enterAll: true);

        await Withdraw(id, clubs[0]);

        var left = await EntrantsOf(id);
        left.Should().HaveCount(2);
        left.Should().NotContain(clubs[0]);
    }

    [Fact]
    public async Task AClubWithFixturesCannotSimplyWithdraw()
    {
        var (_, id, clubs) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await Generate(id);

        var leave = async () => await Withdraw(id, clubs[0]);

        // Otherwise the table counts results from somebody with no place in the competition.
        (await leave.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("already has fixtures");
    }

    // ── Abandoning one ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletingTakesTheEntryListWithIt()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 4);
        var id = await Create(divisionId, enterAll: true);

        await new DeleteCompetitionCommandHandler(_fixture.DbContext)
            .Handle(new DeleteCompetitionCommand(id), CancellationToken.None);

        (await _fixture.DbContext.Competitions.AsNoTracking().AnyAsync(c => c.Id == id))
            .Should().BeFalse();

        // An entry describes a place in this competition and nothing else, so it goes too
        // rather than being left orphaned.
        (await EntrantsOf(id)).Should().BeEmpty();
    }

    [Fact]
    public async Task ACompetitionWithFixturesIsNotDeletedByAccident()
    {
        var (_, id, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await Generate(id);

        var abandon = async () => await new DeleteCompetitionCommandHandler(_fixture.DbContext)
            .Handle(new DeleteCompetitionCommand(id), CancellationToken.None);

        (await abandon.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("fixtures");

        (await _fixture.DbContext.Competitions.AsNoTracking().AnyAsync(c => c.Id == id))
            .Should().BeTrue("a refusal must not half-delete the thing it refused");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> Create(
        Guid divisionId,
        bool enterAll,
        CompetitionFormat format = CompetitionFormat.League,
        string? shortCode = null,
        int? maxTeams = null,
        int season = 2040)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await new CreateCompetitionCommandHandler(_fixture.DbContext).Handle(
            new CreateCompetitionCommand
            {
                Name = $"Competition {TestIds.Code("N")}",
                ShortCode = shortCode ?? TestIds.Code("CC"),
                Season = season,
                Format = format,
                MaxTeams = maxTeams,
                Gender = Gender.Male,
                EnterTeamsFromDivisionId = enterAll ? divisionId : null,
            },
            CancellationToken.None);
    }

    private Task Update(Guid id, string name, CompetitionFormat format, int? maxTeams = null) =>
        new UpdateCompetitionCommandHandler(_fixture.DbContext).Handle(
            new UpdateCompetitionCommand
            {
                CompetitionId = id,
                Name = name,
                ShortCode = TestIds.Code("UC"),
                Format = format,
                Gender = Gender.Male,
                MaxTeams = maxTeams,
                IsActive = true,
            },
            CancellationToken.None);

    private Task Enter(Guid competitionId, Guid teamId) =>
        new EnterTeamCommandHandler(_fixture.DbContext).Handle(
            new EnterTeamCommand(competitionId, teamId), CancellationToken.None);

    private Task Withdraw(Guid competitionId, Guid teamId) =>
        new WithdrawTeamCommandHandler(_fixture.DbContext).Handle(
            new WithdrawTeamCommand(competitionId, teamId), CancellationToken.None);

    private Task<GenerateFixturesResult> Generate(Guid competitionId) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                IsHomeAndAway: false,
                StartDate: new DateTime(2040, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7),
            CancellationToken.None);

    private Task<List<Guid>> EntrantsOf(Guid competitionId) =>
        _fixture.DbContext.CompetitionEntries
            .AsNoTracking()
            .Where(e => e.CompetitionId == competitionId)
            .Select(e => e.TeamId)
            .ToListAsync();
}
