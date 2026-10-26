using MediatR;

namespace MonProfAI.Application.UseCases.Auth;

public class LoginUserCommand : IRequest<LoginUserResponse>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginUserResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
}
