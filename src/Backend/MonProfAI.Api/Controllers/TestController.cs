using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly MonProfAIDbContext _dbContext;

    public TestController(MonProfAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = _dbContext.Users.Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.Role,
                u.IsActive,
                u.CreatedAt
            }).ToList();

            return Ok(new
            {
                success = true,
                message = "✅ Database connection successful!",
                userCount = users.Count,
                users = users,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "❌ Database connection failed!",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("seed-user")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedTestUser()
    {
        try
        {
            // Vérifier si l'utilisateur existe déjà
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.Email == "test@example.com");
            if (existingUser != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Test user already exists",
                    user = new { existingUser.Id, existingUser.Email, existingUser.FullName }
                });
            }

            // Créer un utilisateur de test
            var testUser = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FullName = "Test User",
                PasswordHash = "$2a$11$test", // Dummy hash
                Role = Domain.Entities.UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "✅ Test user created!",
                user = new { testUser.Id, testUser.Email, testUser.FullName },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "❌ Failed to create test user!",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
