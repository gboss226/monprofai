using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Auth;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ILearnerProfileRepository _learnerProfileRepository;
    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(IUserRepository userRepository, ILearnerProfileRepository learnerProfileRepository, IAuthService authService)
    {
        _userRepository = userRepository;
        _learnerProfileRepository = learnerProfileRepository;
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

        var profile = new MonProfAI.Domain.Entities.LearnerProfile
        {
            UserId = createdUser.Id,
            SchoolLevel = SchoolLevel.Sixieme,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _learnerProfileRepository.CreateAsync(profile);

        return new RegisterUserResponse
        {
            UserId = createdUser.Id,
            Email = createdUser.Email
        };
    }
}
