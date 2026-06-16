using Xunit;
using Moq;
using FluentAssertions;
using iDiski.Application.Teams.Commands;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using iDiski.Tests.Unit.Common;

namespace iDiski.Tests.Unit.Teams;

public class UpdateTeamCommandTests : BaseTest
{
    [Fact]
    public async Task UpdateTeamCommand_SuperAdmin_SucceedsWithoutTeamAssignment()
    {
        // Arrange
        var testName = "UpdateTeamCommand_SuperAdmin_SucceedsWithoutTeamAssignment";
        LogTestStart(testName);

        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Old Name",
            ShortCode = "OLD",
            DivisionId = Guid.NewGuid(),
            City = "Old City"
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();
        var auditService = new Mock<IAuditService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Mock that user IS Super Admin
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateTeamCommandHandler(dbContext.Object, currentUserService.Object, auditService.Object);
        var command = new UpdateTeamCommand(teamId, "New Name", null, 2020, "New City", null, null, null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        team.Name.Should().Be("New Name");
        team.City.Should().Be("New City");
        team.Founded.Should().Be(2020);
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        auditService.Verify(x => x.LogAsync(
            "Team",
            teamId,
            "Updated",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        LogTestPass(testName);
    }

    [Fact]
    public async Task UpdateTeamCommand_TeamAdmin_SucceedsWithAssignment()
    {
        // Arrange
        var testName = "UpdateTeamCommand_TeamAdmin_SucceedsWithAssignment";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Old Name",
            ShortCode = "OLD",
            DivisionId = Guid.NewGuid(),
            City = "Old City"
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();
        var auditService = new Mock<IAuditService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(userId);

        // Mock that user is NOT Super Admin
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Mock that user IS assigned to team
        var mockUserTeams = new Mock<DbSet<UserTeam>>();
        mockUserTeams
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserTeam, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);
        dbContext.Setup(x => x.UserTeams).Returns(mockUserTeams.Object);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateTeamCommandHandler(dbContext.Object, currentUserService.Object, auditService.Object);
        var command = new UpdateTeamCommand(teamId, "New Name", null, 2020, null, null, null, null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        team.Name.Should().Be("New Name");
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        LogTestPass(testName);
    }

    [Fact]
    public async Task UpdateTeamCommand_UnauthorizedUser_ThrowsForbiddenException()
    {
        // Arrange
        var testName = "UpdateTeamCommand_UnauthorizedUser_ThrowsForbiddenException";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Team Name",
            ShortCode = "TST",
            DivisionId = Guid.NewGuid()
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();
        var auditService = new Mock<IAuditService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(userId);

        // Mock that user is NOT Super Admin
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Mock that user is NOT assigned to team
        var mockUserTeams = new Mock<DbSet<UserTeam>>();
        mockUserTeams
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserTeam, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Mock UserDivisions too (Division Admin check)
        var mockUserDivisions = new Mock<DbSet<UserDivision>>();
        mockUserDivisions
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserDivision, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);
        dbContext.Setup(x => x.UserTeams).Returns(mockUserTeams.Object);
        dbContext.Setup(x => x.UserDivisions).Returns(mockUserDivisions.Object);

        var handler = new UpdateTeamCommandHandler(dbContext.Object, currentUserService.Object, auditService.Object);
        var command = new UpdateTeamCommand(teamId, "New Name", null, 2020, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task UpdateTeamCommand_NonexistentTeam_ThrowsNotFoundException()
    {
        // Arrange
        var testName = "UpdateTeamCommand_NonexistentTeam_ThrowsNotFoundException";
        LogTestStart(testName);

        var teamId = Guid.NewGuid();

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();
        var auditService = new Mock<IAuditService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team)null!);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);

        var handler = new UpdateTeamCommandHandler(dbContext.Object, currentUserService.Object, auditService.Object);
        var command = new UpdateTeamCommand(teamId, "New Name", null, 2020, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task UpdateTeamCommand_DivisionAdmin_CanUpdateTeamInTheirDivision()
    {
        // Arrange
        var testName = "UpdateTeamCommand_DivisionAdmin_CanUpdateTeamInTheirDivision";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Old Name",
            ShortCode = "OLD",
            DivisionId = divisionId,
            City = "Old City"
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();
        var auditService = new Mock<IAuditService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(userId);

        // Mock that user is NOT Super Admin
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Mock that user is NOT assigned as TeamAdmin
        var mockUserTeams = new Mock<DbSet<UserTeam>>();
        mockUserTeams
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserTeam, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Mock that user IS assigned as DivisionAdmin to this division
        var mockUserDivisions = new Mock<DbSet<UserDivision>>();
        mockUserDivisions
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserDivision, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);
        dbContext.Setup(x => x.UserTeams).Returns(mockUserTeams.Object);
        dbContext.Setup(x => x.UserDivisions).Returns(mockUserDivisions.Object);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateTeamCommandHandler(dbContext.Object, currentUserService.Object, auditService.Object);
        var command = new UpdateTeamCommand(teamId, "New Name", null, 2020, null, null, null, null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        team.Name.Should().Be("New Name");
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        LogTestPass(testName);
    }
}
