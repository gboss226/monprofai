using MediatR;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Application.UseCases.Auth;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }
}
