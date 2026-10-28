using MediatR;

namespace MonProfAI.Application.UseCases.Auth;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    public Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Logout is handled by the frontend clearing the httpOnly cookie
        // This handler can be used for server-side cleanup (e.g., blacklist tokens)
        return Task.FromResult(Unit.Value);
    }
}
