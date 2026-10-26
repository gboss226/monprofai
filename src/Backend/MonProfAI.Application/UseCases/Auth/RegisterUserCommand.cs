using MediatR;

namespace MonProfAI.Application.UseCases.Auth;

public class RegisterUserCommand : IRequest<RegisterUserResponse>
{
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RegisterUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
}
