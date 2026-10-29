using Xunit;
using Moq;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Auth;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Tests.UseCases.Auth;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Register_WithValidData_CreatesUser()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockLearnerProfileRepo = new Mock<ILearnerProfileRepository>();
        var mockAuthService = new Mock<IAuthService>();

        mockAuthService.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password");

        mockUserRepo.Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync((User?)null);

        mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = Guid.NewGuid(); return u; });

        var handler = new RegisterUserCommandHandler(mockUserRepo.Object, mockLearnerProfileRepo.Object, mockAuthService.Object);
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Password123"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
        mockLearnerProfileRepo.Verify(x => x.CreateAsync(It.IsAny<MonProfAI.Domain.Entities.LearnerProfile>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsException()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockLearnerProfileRepo = new Mock<ILearnerProfileRepository>();
        var mockAuthService = new Mock<IAuthService>();

        var existingUser = new User { Email = "test@example.com" };
        mockUserRepo.Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        var handler = new RegisterUserCommandHandler(mockUserRepo.Object, mockLearnerProfileRepo.Object, mockAuthService.Object);
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            FullName = "Test User",
            Password = "Password123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
