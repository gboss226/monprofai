using FluentValidation;

namespace MonProfAI.Application.UseCases.Tutor;

/// <summary>Bounds what a student can send, which also bounds the cost of each call.</summary>
public class AskTutorQueryValidator : AbstractValidator<AskTutorQuery>
{
    public const int MaxMessageLength = 2000;

    public AskTutorQueryValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("La question est vide")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"La question ne doit pas dépasser {MaxMessageLength} caractères");
    }
}
