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
/// Who a club can be drawn against, and the smallest competition of each kind.
///
/// A club belongs to exactly one division and has no gender of its own — its gender is the
/// division's. So the one way a boys side could be fixtured against a women's side was a
/// match made by hand, where nothing checked that the two clubs played in the same
/// competition. That also left the fixture with no division at all, so it never appeared in a
/// division's list and never counted towards a table, while looking perfectly saved.
/// </summary>
public class CrossDivisionSafetyTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CrossDivisionSafetyTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ── Who may be drawn against whom ─────────────────────────────────────────

    [Fact]
    public async Task AFixtureMadeByHand_BelongsToTheDivisionBothClubsPlayIn()
    {
        var divisionId = await ADivision(CompetitionFormat.League, Gender.Male);
        var (home, away) = await TwoClubsIn(divisionId);

        var id = await CreateFixture(home, away);

        var match = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .FirstAsync(m => m.Id == id);

        // Nothing set this before, so every hand-made fixture was an orphan.
        match.DivisionId.Should().Be(divisionId);
    }

    [Fact]
    public async Task ABoysClubCannotBeDrawnAgainstAWomensClub()
    {
        var boys = await ADivision(CompetitionFormat.League, Gender.Male, "U15 Boys");
        var women = await ADivision(CompetitionFormat.League, Gender.Female, "Women");

        var (boysClub, _) = await TwoClubsIn(boys);
        var (womensClub, _) = await TwoClubsIn(women);

        var create = async () => await CreateFixture(boysClub, womensClub);

        var thrown = await create.Should().ThrowAsync<InvalidOperationException>();

        // Named rather than a generic refusal: this is the one mistake here nobody would want
        // to explain afterwards, so the message says which club plays where. Checked by the
        // branch it took rather than by both words, since "female" contains "male".
        thrown.Which.Message.Should().Contain("female");
        thrown.Which.Message.Should().NotContain("different divisions",
            "a gender mismatch deserves more than the generic refusal");
    }

    [Fact]
    public async Task TwoClubsFromDifferentDivisionsOfTheSameGender_AreAlsoRefused()
    {
        // Two under-fifteen boys divisions. Nothing improper about the pairing, but there is
        // no competition it belongs to, and a fixture has to sit in one.
        var first = await ADivision(CompetitionFormat.League, Gender.Male, "U15 Boys A");
        var second = await ADivision(CompetitionFormat.League, Gender.Male, "U15 Boys B");

        var (fromFirst, _) = await TwoClubsIn(first);
        var (fromSecond, _) = await TwoClubsIn(second);

        var create = async () => await CreateFixture(fromFirst, fromSecond);

        var thrown = await create.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain("different divisions");
    }

    [Fact]
    public async Task AClubWithNoDivision_CannotBeFixturedAtAll()
    {
        var divisionId = await ADivision(CompetitionFormat.League, Gender.Male);
        var (inDivision, _) = await TwoClubsIn(divisionId);
        var unattached = await AClubWithNoDivision();

        var create = async () => await CreateFixture(inDivision, unattached);

        await create.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GeneratingForOneDivision_NeverReachesIntoAnother()
    {
        var boys = await ADivision(CompetitionFormat.Knockout, Gender.Male, "U15 Boys");
        var women = await ADivision(CompetitionFormat.League, Gender.Female, "Women");

        await FourClubsIn(boys);
        await FourClubsIn(women);

        await Generate(boys);

        var womensClubs = await ClubIdsIn(women);
        var fixtures = await FixturesFor(boys);

        var drawn = fixtures
            .SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();

        // Entrants come from the division being generated, so a women's club can never be
        // drawn into a boys competition. Asserted rather than assumed, because it is the
        // property everything else here rests on.
        drawn.Should().NotIntersectWith(womensClubs);
        drawn.Should().NotBeEmpty();
    }

    // ── The smallest competition of each kind ─────────────────────────────────

    [Fact]
    public async Task ALeagueOfTwo_IsASingleFixture()
    {
        var divisionId = await ADivision(CompetitionFormat.League, Gender.Male);
        await ClubsIn(divisionId, 2);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);

        fixtures.Should().HaveCount(1);
        fixtures.Should().OnlyContain(m => m.Stage == MatchStage.League);
    }

    [Fact]
    public async Task AKnockoutOfTwo_IsJustAFinal()
    {
        var divisionId = await ADivision(CompetitionFormat.Knockout, Gender.Male);
        await ClubsIn(divisionId, 2);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);

        // The smallest cup there is. It should be one tie that leads nowhere, not a bracket
        // with a spurious round in front of it.
        fixtures.Should().HaveCount(1);
        (fixtures[0].KnockoutRoundSize ?? 0).Should().Be(2);
        fixtures[0].NextMatchId.Should().BeNull();
        fixtures[0].HomeTeamId.Should().NotBeNull();
        fixtures[0].AwayTeamId.Should().NotBeNull();
    }

    [Fact]
    public async Task AKnockoutOfFive_GivesThreeByesAndOneOpeningTie()
    {
        var divisionId = await ADivision(CompetitionFormat.Knockout, Gender.Male);
        await ClubsIn(divisionId, 5);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);

        // Five entrants fill a bracket of eight, so three sit out the opening round and only
        // one tie is actually played in it.
        fixtures.Where(f => f.KnockoutRoundSize == 8).Should().HaveCount(1);
        fixtures.Should().HaveCount(4, "one opening tie, two semi-finals and a final");

        var secondRound = fixtures.Where(f => f.KnockoutRoundSize == 4).ToList();
        secondRound.SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId })
            .Count(id => id is not null)
            .Should().Be(3, "the three byes are already through");
    }

    [Fact]
    public async Task AGroupCompetitionOfFour_IsTwoGroupsOfTwoFeedingASemiFinal()
    {
        var divisionId = await ADivision(CompetitionFormat.GroupAndKnockout, Gender.Male);
        await ClubsIn(divisionId, 4);

        await Generate(divisionId, groups: 2);

        var fixtures = await FixturesFor(divisionId);

        // The smallest group competition that still has both halves.
        fixtures.Count(m => m.Stage == MatchStage.Group).Should().Be(2, "one tie in each group");

        // Two from each of two groups is four qualifiers: two semi-finals and a final.
        fixtures.Count(m => m.Stage == MatchStage.Knockout).Should().Be(3);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<Guid> CreateFixture(Guid home, Guid away) =>
        new CreateMatchResultCommandHandler(_fixture.DbContext).Handle(
            new CreateMatchResultCommand(
                MatchDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                MatchweekNumber: 1,
                Season: 2026,
                HomeTeamId: home,
                AwayTeamId: away,
                Venue: null,
                Referee: null),
            CancellationToken.None);

    private Task<GenerateFixturesResult> Generate(Guid divisionId, int? groups = null) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                divisionId,
                Season: 2026,
                IsHomeAndAway: false,
                StartDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7,
                GroupCount: groups),
            CancellationToken.None);

    private async Task<List<MatchResult>> FixturesFor(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.DivisionId == divisionId)
            .ToListAsync();
    }

    private async Task<List<Guid>> ClubIdsIn(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await _fixture.DbContext.Teams
            .AsNoTracking()
            .Where(t => t.DivisionId == divisionId)
            .Select(t => t.Id)
            .ToListAsync();
    }

    private async Task<Guid> ADivision(
        CompetitionFormat format,
        Gender gender,
        string name = "Division")
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"{name} {TestIds.Code("N")}",
            ShortCode = TestIds.Code("XD"),
            Season = 2026,
            Gender = gender,
            IsActive = true,
            Format = format,
            CreatedAt = DateTime.UtcNow,
        });

        await _fixture.DbContext.SaveChangesAsync();
        return divisionId;
    }

    private async Task<List<Guid>> ClubsIn(Guid divisionId, int count)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);

            _fixture.DbContext.Teams.Add(new Team
            {
                Id = id,
                Name = $"Club {TestIds.Code("N")}",
                ShortCode = TestIds.Code("X"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return ids;
    }

    private async Task<(Guid Home, Guid Away)> TwoClubsIn(Guid divisionId)
    {
        var ids = await ClubsIn(divisionId, 2);
        return (ids[0], ids[1]);
    }

    private Task<List<Guid>> FourClubsIn(Guid divisionId) => ClubsIn(divisionId, 4);

    private async Task<Guid> AClubWithNoDivision()
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var id = Guid.NewGuid();

        _fixture.DbContext.Teams.Add(new Team
        {
            Id = id,
            Name = $"Unattached {TestIds.Code("N")}",
            ShortCode = TestIds.Code("U"),
            DivisionId = null,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
        });

        await _fixture.DbContext.SaveChangesAsync();
        return id;
    }
}
