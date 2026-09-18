using System;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Common.Authorization;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Authorization;
using iDiski.Tests.Integration.Common;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace iDiski.Tests.Integration.Authorization;

/// <summary>
/// The ownership handlers are what actually confine an admin to their own teams and
/// divisions — a role policy only answers "is this user a DivisionAdmin at all". These run
/// against a real context because the handlers resolve access by querying UserTeams,
/// UserCompetitions and UserRoles.
/// </summary>
public class PermissionHierarchyTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public PermissionHierarchyTests(IntegrationTestFixture fixture) => _fixture = fixture;

    /// <summary>Which team the case is about, since a Guid cannot be an InlineData argument.</summary>
    public enum TeamUnderTest { OwnTeam, SameDivisionOtherTeam, OtherDivisionTeam }

    [Theory]
    // A super admin reaches every team, in any division.
    [InlineData(Role.SuperAdmin, TeamUnderTest.OwnTeam, true)]
    [InlineData(Role.SuperAdmin, TeamUnderTest.SameDivisionOtherTeam, true)]
    [InlineData(Role.SuperAdmin, TeamUnderTest.OtherDivisionTeam, true)]
    // A competition admin reaches no club at all — not even one entered in the competition
    // they run. Clubs answer to their own administrators.
    [InlineData(Role.CompetitionAdmin, TeamUnderTest.OwnTeam, false)]
    [InlineData(Role.CompetitionAdmin, TeamUnderTest.SameDivisionOtherTeam, false)]
    [InlineData(Role.CompetitionAdmin, TeamUnderTest.OtherDivisionTeam, false)]
    // A team admin reaches only the team they are assigned to, even within the same division.
    [InlineData(Role.TeamAdmin, TeamUnderTest.OwnTeam, true)]
    [InlineData(Role.TeamAdmin, TeamUnderTest.SameDivisionOtherTeam, false)]
    [InlineData(Role.TeamAdmin, TeamUnderTest.OtherDivisionTeam, false)]
    public async Task TeamOwnership_FollowsTheAssignedHierarchy(
        Role role, TeamUnderTest target, bool expectedAllowed)
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var teamId = target switch
        {
            TeamUnderTest.OwnTeam => scenario.TeamAId,
            TeamUnderTest.SameDivisionOtherTeam => scenario.TeamBId,
            TeamUnderTest.OtherDivisionTeam => scenario.TeamCId,
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };

        var allowed = await EvaluateAsync(
            new TeamOwnershipHandler(_fixture.DbContext, scenario.SignedInAs(role)),
            new TeamOwnershipRequirement(teamId));

        allowed.Should().Be(expectedAllowed);
    }

    [Theory]
    [InlineData(Role.SuperAdmin, true, true)]
    [InlineData(Role.SuperAdmin, false, true)]
    // A competition admin is confined to the competitions they were actually given.
    [InlineData(Role.CompetitionAdmin, true, true)]
    [InlineData(Role.CompetitionAdmin, false, false)]
    // Being a team admin never grants authority over a competition.
    [InlineData(Role.TeamAdmin, true, false)]
    [InlineData(Role.TeamAdmin, false, false)]
    public async Task CompetitionOwnership_FollowsTheAssignment(
        Role role, bool theirOwn, bool expectedAllowed)
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var competitionId = theirOwn
            ? scenario.CompetitionOneId
            : await CompetitionScenario.ACompetitionAsync(_fixture.DbContext);

        var allowed = await EvaluateAsync(
            new CompetitionOwnershipHandler(_fixture.DbContext, scenario.SignedInAs(role)),
            new CompetitionOwnershipRequirement(competitionId));

        allowed.Should().Be(expectedAllowed);
    }

    [Fact]
    public async Task AnonymousRequests_AreRefusedEvenForAnExistingTeam()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var allowed = await EvaluateAsync(
            new TeamOwnershipHandler(_fixture.DbContext, FakeCurrentUserService.Anonymous()),
            new TeamOwnershipRequirement(scenario.TeamAId));

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task UnknownTeam_IsRefusedForAnAdminWhoIsNotSuperAdmin()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var allowed = await EvaluateAsync(
            new TeamOwnershipHandler(_fixture.DbContext, scenario.SignedInAs(Role.TeamAdmin)),
            new TeamOwnershipRequirement(Guid.NewGuid()));

        allowed.Should().BeFalse();
    }

    private static async Task<bool> EvaluateAsync(
        IAuthorizationHandler handler, IAuthorizationRequirement requirement)
    {
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, new System.Security.Claims.ClaimsPrincipal(), resource: null);

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }
}
