using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Authentication.Commands;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Services;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace iDiski.Tests.Integration.Users;

/// <summary>
/// Who may create whom. The handler enforces this itself rather than leaning on the endpoint
/// policy, so it is worth pinning down: a division admin may only make team admins, and only
/// inside their own division.
/// </summary>
public class CreateUserPermissionTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CreateUserPermissionTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Theory]
    // A super admin may create any role.
    [InlineData(Role.SuperAdmin, Role.SuperAdmin)]
    [InlineData(Role.SuperAdmin, Role.DivisionAdmin)]
    [InlineData(Role.SuperAdmin, Role.TeamAdmin)]
    // A division admin may create team admins, for a team inside their division.
    [InlineData(Role.DivisionAdmin, Role.TeamAdmin)]
    public async Task CreatingAnAllowedRole_PersistsTheUserWithThatRole(Role actor, Role roleToCreate)
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var email = $"created-{Guid.NewGuid():N}@test.com";

        var newUserId = await HandlerFor(scenario, actor).Handle(
            new CreateUserCommand(
                email, "SecurePassword123!", "New", "Admin",
                new[] { roleToCreate },
                AssignedTeamIds: new[] { scenario.TeamAId }),
            CancellationToken.None);

        var created = await _fixture.DbContext.Users.FindAsync(newUserId);
        created.Should().NotBeNull();
        created!.Email.Should().Be(email);

        var roles = await _fixture.DbContext.UserRoles
            .Where(r => r.UserId == newUserId)
            .Select(r => r.Role)
            .ToListAsync();
        roles.Should().ContainSingle().Which.Should().Be(roleToCreate);
    }

    [Theory]
    // A division admin may not mint peers or super admins.
    [InlineData(Role.DivisionAdmin, Role.SuperAdmin)]
    [InlineData(Role.DivisionAdmin, Role.DivisionAdmin)]
    // A team admin may not create users at all.
    [InlineData(Role.TeamAdmin, Role.TeamAdmin)]
    [InlineData(Role.TeamAdmin, Role.DivisionAdmin)]
    [InlineData(Role.TeamAdmin, Role.SuperAdmin)]
    public async Task CreatingARoleBeyondTheActorsReach_IsForbidden(Role actor, Role roleToCreate)
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var create = async () => await HandlerFor(scenario, actor).Handle(
            new CreateUserCommand(
                $"denied-{Guid.NewGuid():N}@test.com", "SecurePassword123!", "New", "Admin",
                new[] { roleToCreate },
                AssignedTeamIds: new[] { scenario.TeamAId }),
            CancellationToken.None);

        await create.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DivisionAdmin_CannotPlaceATeamAdminOnATeamOutsideTheirDivision()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var create = async () => await HandlerFor(scenario, Role.DivisionAdmin).Handle(
            new CreateUserCommand(
                $"outside-{Guid.NewGuid():N}@test.com", "SecurePassword123!", "New", "Admin",
                new[] { Role.TeamAdmin },
                // TeamC sits in the division this admin has nothing to do with.
                AssignedTeamIds: new[] { scenario.TeamCId }),
            CancellationToken.None);

        await create.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreatedUsers_GetAHashedPasswordRatherThanTheirPlaintextOne()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        const string password = "SecurePassword123!";

        var newUserId = await HandlerFor(scenario, Role.SuperAdmin).Handle(
            new CreateUserCommand(
                $"hashed-{Guid.NewGuid():N}@test.com", password, "New", "Admin",
                new[] { Role.TeamAdmin },
                AssignedTeamIds: new[] { scenario.TeamAId }),
            CancellationToken.None);

        var created = await _fixture.DbContext.Users.FindAsync(newUserId);
        created!.PasswordHash.Should().NotBe(password);
        new Argon2PasswordHasher().VerifyPassword(password, created.PasswordHash).Should().BeTrue();
    }

    private CreateUserCommandHandler HandlerFor(LeagueScenario scenario, Role actor) =>
        new(_fixture.DbContext,
            new Argon2PasswordHasher(),
            scenario.SignedInAs(actor),
            NullLogger<CreateUserCommandHandler>.Instance);
}
