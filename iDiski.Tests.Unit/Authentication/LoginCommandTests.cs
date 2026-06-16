using Xunit;
using Moq;
using FluentAssertions;
using iDiski.Application.Authentication.Commands;
using iDiski.Application.Common.Interfaces;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using iDiski.Tests.Unit.Common;

namespace iDiski.Tests.Unit.Authentication;

public class LoginCommandTests : BaseTest
{
    [Fact]
    public async Task LoginCommand_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var testName = "LoginCommand_WithValidCredentials_ReturnsToken";
        LogTestStart(testName);

        var userId = Guid.NewGuid();
        var email = "admin@test.com";
        var password = "SecurePassword123!";

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var dbContext = new Mock<ILeagueDbContext>();

        var user = new User
        {
            Id = userId,
            Email = email,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true,
            PasswordHash = "hashed_password"
        };

        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        dbContext.Setup(x => x.Users).Returns(mockUserSet.Object);
        passwordHasher.Setup(x => x.VerifyPassword(password, user.PasswordHash)).Returns(true);
        jwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<IReadOnlyList<int>>()))
            .Returns("valid_jwt_token");

        var handler = new LoginCommandHandler(dbContext.Object, passwordHasher.Object, jwtTokenGenerator.Object);
        var command = new LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("valid_jwt_token");
        result.User.Email.Should().Be(email);

        LogTestPass(testName);
    }

    [Fact]
    public async Task LoginCommand_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var testName = "LoginCommand_WithInvalidPassword_ThrowsUnauthorizedException";
        LogTestStart(testName);

        var email = "admin@test.com";
        var password = "WrongPassword123!";

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var dbContext = new Mock<ILeagueDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true,
            PasswordHash = "hashed_password"
        };

        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        dbContext.Setup(x => x.Users).Returns(mockUserSet.Object);
        passwordHasher.Setup(x => x.VerifyPassword(password, user.PasswordHash)).Returns(false);

        var handler = new LoginCommandHandler(dbContext.Object, passwordHasher.Object, jwtTokenGenerator.Object);
        var command = new LoginCommand(email, password);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task LoginCommand_WithNonexistentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var testName = "LoginCommand_WithNonexistentUser_ThrowsUnauthorizedException";
        LogTestStart(testName);

        var email = "nonexistent@test.com";
        var password = "AnyPassword123!";

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var dbContext = new Mock<ILeagueDbContext>();

        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null!);

        dbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

        var handler = new LoginCommandHandler(dbContext.Object, passwordHasher.Object, jwtTokenGenerator.Object);
        var command = new LoginCommand(email, password);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }

    [Fact]
    public async Task LoginCommand_WithInactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var testName = "LoginCommand_WithInactiveUser_ThrowsUnauthorizedException";
        LogTestStart(testName);

        var email = "inactive@test.com";
        var password = "SecurePassword123!";

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        var dbContext = new Mock<ILeagueDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Inactive",
            LastName = "User",
            IsActive = false,
            PasswordHash = "hashed_password"
        };

        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        dbContext.Setup(x => x.Users).Returns(mockUserSet.Object);
        passwordHasher.Setup(x => x.VerifyPassword(password, user.PasswordHash)).Returns(true);

        var handler = new LoginCommandHandler(dbContext.Object, passwordHasher.Object, jwtTokenGenerator.Object);
        var command = new LoginCommand(email, password);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await handler.Handle(command, CancellationToken.None));

        LogTestPass(testName);
    }
}
