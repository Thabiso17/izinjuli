using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using iDiski.Application.AuditLogs;
using iDiski.Application.AuditLogs.Queries;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using iDiski.Tests.Unit.Common;

namespace iDiski.Tests.Unit.AuditLogs;

public class GetAuditLogsQueryTests : BaseTest
{
    [Fact]
    public async Task GetAuditLogsQuery_WithNoFilters_ReturnsAllLogs()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_WithNoFilters_ReturnsAllLogs";
        LogTestStart(testName);

        var auditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = Guid.NewGuid(),
                Action = "Created",
                UserId = Guid.NewGuid(),
                UserEmail = "admin@test.com",
                Description = "Created team",
                ChangedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Player",
                EntityId = Guid.NewGuid(),
                Action = "Updated",
                UserId = Guid.NewGuid(),
                UserEmail = "admin@test.com",
                Description = "Updated player",
                ChangedAt = DateTime.UtcNow
            }
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs.Count);

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(l => l.EntityType == "Team");
        result.Should().Contain(l => l.EntityType == "Player");

        LogTestPass(testName);
    }

    [Fact]
    public async Task GetAuditLogsQuery_FilterByEntityType_ReturnsOnlyMatchingLogs()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_FilterByEntityType_ReturnsOnlyMatchingLogs";
        LogTestStart(testName);

        var teamLogs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = Guid.NewGuid(),
                Action = "Created",
                UserId = Guid.NewGuid(),
                UserEmail = "admin@test.com",
                ChangedAt = DateTime.UtcNow
            }
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamLogs.Count);

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamLogs);

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery(entityType: "Team");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().EntityType.Should().Be("Team");

        LogTestPass(testName);
    }

    [Fact]
    public async Task GetAuditLogsQuery_FilterByUserId_ReturnsOnlyUserLogs()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_FilterByUserId_ReturnsOnlyUserLogs";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var userLogs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = Guid.NewGuid(),
                Action = "Created",
                UserId = userId,
                UserEmail = "user@test.com",
                ChangedAt = DateTime.UtcNow
            }
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userLogs.Count);

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userLogs);

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery(userId: userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(userId);

        LogTestPass(testName);
    }

    [Fact]
    public async Task GetAuditLogsQuery_FilterByEntityId_ReturnsEntityHistory()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_FilterByEntityId_ReturnsEntityHistory";
        LogTestStart(testName);

        var entityId = Guid.NewGuid();
        var history = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = entityId,
                Action = "Created",
                UserId = Guid.NewGuid(),
                UserEmail = "admin@test.com",
                Description = "Created team",
                ChangedAt = DateTime.UtcNow.AddHours(-2)
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = entityId,
                Action = "Updated",
                UserId = Guid.NewGuid(),
                UserEmail = "admin@test.com",
                Description = "Updated team name",
                ChangedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(history.Count);

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery(entityId: entityId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(l => l.EntityId.Should().Be(entityId));
        result.Should().Contain(l => l.Action == "Created");
        result.Should().Contain(l => l.Action == "Updated");

        LogTestPass(testName);
    }

    [Fact]
    public async Task GetAuditLogsQuery_WithDateRangeFilter_ReturnsLogsInRange()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_WithDateRangeFilter_ReturnsLogsInRange";
        LogTestStart(testName);

        var now = DateTime.UtcNow;
        var fromDate = now.AddHours(-2);
        var toDate = now.AddHours(-1);

        var logsInRange = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = Guid.NewGuid(),
                Action = "Updated",
                ChangedAt = fromDate.AddMinutes(30)
            }
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(logsInRange.Count);

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(logsInRange);

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery(fromDate: fromDate, toDate: toDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().ChangedAt.Should().BeGreaterThanOrEqualTo(fromDate);
        result.First().ChangedAt.Should().BeLessThanOrEqualTo(toDate);

        LogTestPass(testName);
    }

    [Fact]
    public async Task GetAuditLogsQuery_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_WithPagination_ReturnsCorrectPage";
        LogTestStart(testName);

        var page2Logs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Team",
                EntityId = Guid.NewGuid(),
                Action = "Updated",
                ChangedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Player",
                EntityId = Guid.NewGuid(),
                Action = "Created",
                ChangedAt = DateTime.UtcNow
            }
        };

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100); // Total 100 logs

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Logs);

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery(pageNumber: 2, pageSize: 50);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        LogTestPass(testName);
    }

    [Fact]
    public async Task GetAuditLogsQuery_NoMatchingLogs_ReturnsEmptyList()
    {
        // Arrange
        var testName = "GetAuditLogsQuery_NoMatchingLogs_ReturnsEmptyList";
        LogTestStart(testName);

        var dbContext = new Mock<ILeagueDbContext>();
        var mockAuditLogs = new Mock<DbSet<AuditLog>>();

        mockAuditLogs
            .Setup(x => x.AsNoTracking())
            .Returns(mockAuditLogs.Object);

        mockAuditLogs
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mockAuditLogs
            .Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog>());

        dbContext.Setup(x => x.AuditLogs).Returns(mockAuditLogs.Object);

        var handler = new GetAuditLogsQueryHandler(dbContext.Object);
        var query = new GetAuditLogsQuery(entityType: "Nonexistent");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();

        LogTestPass(testName);
    }
}
