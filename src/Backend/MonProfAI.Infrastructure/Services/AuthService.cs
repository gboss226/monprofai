using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(string jwtSecret, string jwtIssuer = "MonProfAI", string jwtAudience = "MonProfAI")
    {
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(Guid userId)
    {
        var accessToken = GenerateJwt(userId, TimeSpan.FromHours(1));
        var refreshToken = GenerateJwt(userId, TimeSpan.FromDays(7), "refresh");
        return await Task.FromResult((accessToken, refreshToken));
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private string GenerateJwt(Guid userId, TimeSpan expiresIn, string type = "access")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("token_type", type)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
