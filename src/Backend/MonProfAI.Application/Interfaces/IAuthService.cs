namespace MonProfAI.Application.Interfaces;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(Guid userId);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
