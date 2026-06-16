using Xunit;
using Moq;
using FluentAssertions;
using iDiski.Application.Users.Commands;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using iDiski.Tests.Unit.Common;

namespace iDiski.Tests.Unit.Users;

public class CreateUserCommandTests : BaseTest
{
    [Fact]
    public async Task CreateUserCommand_WithValidData_CreatesUser()
    {
        // Arrange
        var testName = "CreateUserCommand_WithValidData_CreatesUser";
        LogTestStart(testName);

        var email = "newuser@test.com";
        var firstName = "Test";
        var lastName = "User";
        var password = "SecurePassword123!";

        var dbContext = new Mock<ILeagueDbContext>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var currentUserService = new Mock<ICurrentUserService>();

        // Mock that email doesn't already exist
        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        dbContext.Setup(x => x.Users).Returns(mockUserSet.Object);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        passwordHasher.Setup(x => x.HashPassword(password)).Returns("hashed_password");

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new CreateUserCommandHandler(dbContext.Object, passwordHasher.Object, currentUserService.Object);
        var command = new CreateUserCommand(email, password, firstName, lastName, new[] { (int)Role.TeamAdmin }, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        dbContext.Verify(x => x.Users.Add(It.IsAny<User>()), Times.Once);
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        LogTestPass(testName);
    }

    [Fact]
    public async Task CreateUserCommand_WithDuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        var testName = "CreateUserCommand_WithDuplicateEmail_ThrowsValidationException";
        LogTestStart(testName);

        var email = "existing@test.com";
        var password = "SecurePassword123!";

        var dbContext = new Mock<ILeagueDbContext>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var currentUserService = new Mock<ICurrentUserService>();

        var existingUser = new User { Id = Guid.NewGuid(), Email = email };

        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        dbContext.Setup(x => x.Users).Returns(mockUserSet.Object);
        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new CreateUserCommandHandler(dbContext.Object, passwordHasher.Object, currentUserService.Object);
        var command = new CreateUserCommand(email, password, "Test", "User", new[] { (int)Role.TeamAdmin }, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task CreateUserCommand_WithWeakPassword_ThrowsValidationException()
    {
        // Arrange
        var testName = "CreateUserCommand_WithWeakPassword_ThrowsValidationException";
        LogTestStart(testName);

        var email = "newuser@test.com";
        var weakPassword = "weak"; // Too short, no special chars

        var dbContext = new Mock<ILeagueDbContext>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);

        var handler = new CreateUserCommandHandler(dbContext.Object, passwordHasher.Object, currentUserService.Object);
        var command = new CreateUserCommand(email, weakPassword, "Test", "User", new[] { (int)Role.TeamAdmin }, null, null);

        // Act & Assert
        // The validator should catch this
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task CreateUserCommand_NotSuperAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var testName = "CreateUserCommand_NotSuperAdmin_ThrowsForbiddenException";
        LogTestStart(testName);

        var dbContext = new Mock<ILeagueDbContext>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Mock that user is NOT Super Admin
        var mockUserRoles = new Mock<DbSet<UserRole>>();
        mockUserRoles
            .Setup(x => x.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<UserRole, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        dbContext.Setup(x => x.UserRoles).Returns(mockUserRoles.Object);

        var handler = new CreateUserCommandHandler(dbContext.Object, passwordHasher.Object, currentUserService.Object);
        var command = new CreateUserCommand("new@test.com", "SecurePassword123!", "Test", "User", new[] { (int)Role.TeamAdmin }, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task CreateUserCommand_WithInvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var testName = "CreateUserCommand_WithInvalidEmail_ThrowsValidationException";
        LogTestStart(testName);

        var invalidEmail = "notanemail";
        var password = "SecurePassword123!";

        var dbContext = new Mock<ILeagueDbContext>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var currentUserService = new Mock<ICurrentUserService>();

        var handler = new CreateUserCommandHandler(dbContext.Object, passwordHasher.Object, currentUserService.Object);
        var command = new CreateUserCommand(invalidEmail, password, "Test", "User", new[] { (int)Role.TeamAdmin }, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }
}
