using Xunit;
using Microsoft.AspNetCore.Mvc;
using MonProfAI.Api.Controllers;

namespace MonProfAI.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsOkResult()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OkObjectResult>(result);

        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void Get_ReturnsHealthResponse()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.Get() as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result.Value;
        Assert.NotNull(value);

        var type = value.GetType();
        var statusProperty = type.GetProperty("status");
        Assert.NotNull(statusProperty);
        Assert.Equal("healthy", statusProperty?.GetValue(value)?.ToString());
    }
}
