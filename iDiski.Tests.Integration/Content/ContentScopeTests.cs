using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Articles.Commands;
using iDiski.Application.Videos.Commands;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Content;

/// <summary>
/// An article or video can say what it is about, narrowing division → team → player. The
/// levels have to agree, or content would claim a subject it does not have and show up on
/// the wrong pages.
/// </summary>
public class ContentScopeTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ContentScopeTests(IntegrationTestFixture fixture) => _fixture = fixture;

    /// <summary>Which scope shape the case describes, since ids cannot be InlineData arguments.</summary>
    public enum Scope
    {
        LeagueWide,
        DivisionOnly,
        DivisionAndTeam,
        FullPlayerScope,
        TeamWithoutDivision,
        PlayerWithoutTeam,
        TeamFromAnotherDivision,
        PlayerWhoHasSinceMoved,
        UnknownDivision,
    }

    [Theory]
    // The four shapes an author can legitimately choose.
    [InlineData(Scope.LeagueWide, true)]
    [InlineData(Scope.DivisionOnly, true)]
    [InlineData(Scope.DivisionAndTeam, true)]
    [InlineData(Scope.FullPlayerScope, true)]
    // Each level needs the one above it.
    [InlineData(Scope.TeamWithoutDivision, false)]
    [InlineData(Scope.PlayerWithoutTeam, false)]
    // And the levels must actually belong together.
    [InlineData(Scope.TeamFromAnotherDivision, false)]
    [InlineData(Scope.UnknownDivision, false)]
    // Allowed on purpose: the player has moved on since, and the piece stays with the club
    // it was written about. Nothing records past squads, so a stricter rule could not tell a
    // historical tag from a wrong one.
    [InlineData(Scope.PlayerWhoHasSinceMoved, true)]
    public async Task ArticleScope_IsAcceptedOnlyWhenTheLevelsAgree(Scope scope, bool expectedValid)
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var (divisionId, teamId, playerId) = Resolve(scope, scenario);

        var command = new CreateArticleCommand(
            "A headline that is long enough", "Body content comfortably past the minimum length.",
            null, null, null, null, "Editorial", Array.Empty<string>(),
            PublishImmediately: false,
            DivisionId: divisionId, TeamId: teamId, PlayerId: playerId);

        var result = await new CreateArticleCommandValidator(_fixture.DbContext)
            .ValidateAsync(command);

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(Scope.LeagueWide, true)]
    [InlineData(Scope.DivisionOnly, true)]
    [InlineData(Scope.DivisionAndTeam, true)]
    [InlineData(Scope.FullPlayerScope, true)]
    [InlineData(Scope.TeamWithoutDivision, false)]
    [InlineData(Scope.PlayerWithoutTeam, false)]
    [InlineData(Scope.TeamFromAnotherDivision, false)]
    [InlineData(Scope.PlayerWhoHasSinceMoved, true)]
    public async Task VideoScope_FollowsTheSameRules(Scope scope, bool expectedValid)
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var (divisionId, teamId, playerId) = Resolve(scope, scenario);

        var command = new CreateVideoCommand(
            "Highlights", "https://youtu.be/abcdefghijk", null, null, "Media",
            PublishImmediately: false,
            DivisionId: divisionId, TeamId: teamId, PlayerId: playerId);

        var result = await new CreateVideoCommandValidator(_fixture.DbContext).ValidateAsync(command);

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public async Task ScopeSurvivesATransfer_SoTheOldClubKeepsThePiece()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var article = new Article
        {
            Id = Guid.NewGuid(), Title = "Signing", Slug = $"signing-{Guid.NewGuid():N}",
            Content = "Body", Author = "Editorial", Tags = Array.Empty<string>(),
            DivisionId = scenario.DivisionOneId, TeamId = scenario.TeamAId,
            PlayerId = scenario.PlayerAId, CreatedAt = DateTime.UtcNow
        };
        _fixture.DbContext.Articles.Add(article);
        await _fixture.DbContext.SaveChangesAsync();

        // The player moves to another club.
        var player = await _fixture.DbContext.Players.FindAsync(scenario.PlayerAId);
        player!.TeamId = scenario.TeamBId;
        await _fixture.DbContext.SaveChangesAsync();

        var stored = await _fixture.DbContext.Articles.FindAsync(article.Id);
        stored!.TeamId.Should().Be(scenario.TeamAId, "the piece records who it was about at the time");
        stored.PlayerId.Should().Be(scenario.PlayerAId, "so it still surfaces on the player's own page");
    }

    private static (Guid? Division, Guid? Team, Guid? Player) Resolve(Scope scope, LeagueScenario s) =>
        scope switch
        {
            Scope.LeagueWide => (null, null, null),
            Scope.DivisionOnly => (s.DivisionOneId, null, null),
            Scope.DivisionAndTeam => (s.DivisionOneId, s.TeamAId, null),
            Scope.FullPlayerScope => (s.DivisionOneId, s.TeamAId, s.PlayerAId),
            Scope.TeamWithoutDivision => (null, s.TeamAId, null),
            Scope.PlayerWithoutTeam => (s.DivisionOneId, null, s.PlayerAId),
            // TeamC belongs to DivisionTwo, so pairing it with DivisionOne is a mismatch.
            Scope.TeamFromAnotherDivision => (s.DivisionOneId, s.TeamCId, null),
            // PlayerA plays for TeamA, so pairing them with TeamB is what a piece about their
            // time at a previous club looks like.
            Scope.PlayerWhoHasSinceMoved => (s.DivisionOneId, s.TeamBId, s.PlayerAId),
            Scope.UnknownDivision => (Guid.NewGuid(), null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(scope)),
        };
}
