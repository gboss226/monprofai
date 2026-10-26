using Xunit;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Tests.Domain;

public class UserEntityTests
{
    [Fact]
    public void CreateUser_WithValidData_Succeeds()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = "hashed_password",
            Role = UserRole.Student,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.FullName);
        Assert.Equal(UserRole.Student, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_CanChangeRole()
    {
        // Arrange
        var user = new User { Role = UserRole.Student };

        // Act
        user.Role = UserRole.Teacher;

        // Assert
        Assert.Equal(UserRole.Teacher, user.Role);
    }

    [Theory]
    [InlineData(UserRole.Student)]
    [InlineData(UserRole.Teacher)]
    [InlineData(UserRole.Admin)]
    public void User_SupportsAllRoles(UserRole role)
    {
        // Arrange & Act
        var user = new User { Role = role };

        // Assert
        Assert.Equal(role, user.Role);
    }
}
