using FluentValidation;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Application.UseCases.Tutor;

/// <summary>Bounds what a student can send, which also bounds the cost of each call.</summary>
public class AskTutorQueryValidator : AbstractValidator<AskTutorQuery>
{
    public const int MaxMessageLength = 2000;
    public const int MaxHistoryTurns = 20;

    public AskTutorQueryValidator(INotionRepository notionRepository)
    {
        RuleFor(x => x.NotionId)
            .MustAsync(async (id, ct) => id is null || await notionRepository.GetByIdAsync(id.Value, ct) is not null)
            .WithMessage("Cette notion n'existe pas");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("La question est vide")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"La question ne doit pas dépasser {MaxMessageLength} caractères");

        RuleFor(x => x.History)
            .Must(h => h is null || h.Count <= MaxHistoryTurns)
            .WithMessage($"La conversation ne peut pas dépasser {MaxHistoryTurns} messages");

        RuleForEach(x => x.History).ChildRules(turn =>
        {
            turn.RuleFor(t => t.Role)
                .Must(r => r is "user" or "assistant")
                .WithMessage("Rôle de message invalide");
            turn.RuleFor(t => t.Content)
                .NotEmpty().WithMessage("Un message de la conversation est vide")
                .MaximumLength(MaxMessageLength * 4)
                .WithMessage("Un message de la conversation est trop long");
        });
    }
}
