using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using iDiski.Application.Common.Authorization;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iDiski.Tests.Unit.Common;

namespace iDiski.Tests.Unit.Authorization;

public class TeamOwnershipHandlerTests : BaseTest
{
    [Fact]
    public async Task TeamOwnershipHandler_SuperAdmin_SucceedsWithoutTeamAssignment()
    {
        // Arrange
        var testName = "TeamOwnershipHandler_SuperAdmin_SucceedsWithoutTeamAssignment";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(userId);

        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // SuperAdmin role exists

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);

        var handler = new TeamOwnershipHandler(dbContext.Object, currentUserService.Object);
        var requirement = new TeamOwnershipRequirement(teamId);
        var context = new AuthorizationHandlerContext(new[] { requirement }, null, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
        LogTestPass(testName);
    }

    [Fact]
    public async Task TeamOwnershipHandler_TeamAdmin_SucceedsWithTeamAssignment()
    {
        // Arrange
        var testName = "TeamOwnershipHandler_TeamAdmin_SucceedsWithTeamAssignment";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(userId);

        var team = new Team { Id = teamId, DivisionId = Guid.NewGuid(), ShortCode = "TEST" };

        // Mock UserRoles (SuperAdmin check - returns false)
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Mock Teams
        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Mock UserTeams (Team assignment - returns true)
        var mockUserTeams = new Mock<DbSet<UserTeam>>();
        mockUserTeams
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserTeam, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);
        dbContext.Setup(x => x.UserTeams).Returns(mockUserTeams.Object);

        var handler = new TeamOwnershipHandler(dbContext.Object, currentUserService.Object);
        var requirement = new TeamOwnershipRequirement(teamId);
        var context = new AuthorizationHandlerContext(new[] { requirement }, null, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
        LogTestPass(testName);
    }

    [Fact]
    public async Task TeamOwnershipHandler_UnassignedUser_FailsAuthorization()
    {
        // Arrange
        var testName = "TeamOwnershipHandler_UnassignedUser_FailsAuthorization";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var dbContext = new Mock<ILeagueDbContext>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(userId);

        var team = new Team { Id = teamId, DivisionId = Guid.NewGuid(), ShortCode = "TEST" };

        // Mock UserRoles (SuperAdmin check - returns false)
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Mock Teams
        var mockTeams = new Mock<DbSet<Team>>();
        mockTeams.Setup(x => x.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Mock UserDivisions and UserTeams (no assignments)
        var mockUserDivisions = new Mock<DbSet<UserDivision>>();
        mockUserDivisions
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserDivision, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockUserTeams = new Mock<DbSet<UserTeam>>();
        mockUserTeams
            .Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<UserTeam, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);
        dbContext.Setup(x => x.Teams).Returns(mockTeams.Object);
        dbContext.Setup(x => x.UserDivisions).Returns(mockUserDivisions.Object);
        dbContext.Setup(x => x.UserTeams).Returns(mockUserTeams.Object);

        var handler = new TeamOwnershipHandler(dbContext.Object, currentUserService.Object);
        var requirement = new TeamOwnershipRequirement(teamId);
        var context = new AuthorizationHandlerContext(new[] { requirement }, null, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
        LogTestPass(testName);
    }
}
