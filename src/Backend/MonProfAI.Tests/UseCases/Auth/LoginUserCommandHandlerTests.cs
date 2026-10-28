using Xunit;
using Moq;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Auth;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Tests.UseCases.Auth;

public class LoginUserCommandHandlerTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndUser()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockAuthService = new Mock<IAuthService>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = "hashed_password",
            IsActive = true
        };

        mockUserRepo.Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        mockAuthService.Setup(x => x.VerifyPassword("Password123", "hashed_password"))
            .Returns(true);

        mockAuthService.Setup(x => x.GenerateTokensAsync(user.Id))
            .ReturnsAsync(("access_token", "refresh_token"));

        var handler = new LoginUserCommandHandler(mockUserRepo.Object, mockAuthService.Object);
        var command = new LoginUserCommand
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal(user.Email, result.User.Email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsException()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockAuthService = new Mock<IAuthService>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = true
        };

        mockUserRepo.Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        mockAuthService.Setup(x => x.VerifyPassword("WrongPassword", "hashed_password"))
            .Returns(false);

        var handler = new LoginUserCommandHandler(mockUserRepo.Object, mockAuthService.Object);
        var command = new LoginUserCommand
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ThrowsException()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockAuthService = new Mock<IAuthService>();

        mockUserRepo.Setup(x => x.GetByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User?)null);

        var handler = new LoginUserCommandHandler(mockUserRepo.Object, mockAuthService.Object);
        var command = new LoginUserCommand
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }
}
