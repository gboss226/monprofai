using FluentValidation;

namespace MonProfAI.Application.UseCases.Auth;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis")
            .EmailAddress().WithMessage("L'email n'est pas valide");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis");
    }
}
