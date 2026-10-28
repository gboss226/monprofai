using MediatR;

namespace MonProfAI.Application.UseCases.Auth;

public class GetCurrentUserQuery : IRequest<CurrentUserDto>
{
    public Guid UserId { get; set; }
}

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
}
