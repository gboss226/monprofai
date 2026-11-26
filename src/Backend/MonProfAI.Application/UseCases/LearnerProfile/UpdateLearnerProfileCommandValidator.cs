using FluentValidation;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public class UpdateLearnerProfileCommandValidator : AbstractValidator<UpdateLearnerProfileCommand>
{
    public UpdateLearnerProfileCommandValidator()
    {
        RuleFor(x => x.SchoolLevel)
            .Must(BeDefined<SchoolLevel>).WithMessage("La classe n'est pas valide");

        RuleFor(x => x.Goal)
            .Must(BeDefined<LearningGoal>).WithMessage("L'objectif n'est pas valide");

        RuleFor(x => x.PreferredLearningStyle)
            .Must(BeDefined<LearningStyle>).WithMessage("La façon d'apprendre n'est pas valide");

        RuleFor(x => x.StrugglingSubjects)
            .NotNull().WithMessage("La liste des matières est requise")
            .Must(subjects => subjects == null || subjects.All(SchoolSubjects.All.Contains))
                .WithMessage("Une des matières n'est pas valide")
            .Must(subjects => subjects == null || subjects.Distinct().Count() == subjects.Count)
                .WithMessage("Une matière est en double");
    }

    // Accepts only the exact enum member name ("Bac"), not numeric strings ("1") or arbitrary casing.
    private static bool BeDefined<TEnum>(string? value) where TEnum : struct, Enum
        => !string.IsNullOrEmpty(value) && Enum.GetNames<TEnum>().Contains(value);
}
