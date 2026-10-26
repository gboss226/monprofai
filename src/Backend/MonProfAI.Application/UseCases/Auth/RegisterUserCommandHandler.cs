using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Auth;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(IUserRepository userRepository, IAuthService authService)
    {
        _userRepository = userRepository;
        _authService = authService;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User already exists");

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = _authService.HashPassword(request.Password),
            Role = UserRole.Student,
            IsActive = true
        };

        var createdUser = await _userRepository.CreateAsync(user);

        return new RegisterUserResponse
        {
            UserId = createdUser.Id,
            Email = createdUser.Email
        };
    }
}
