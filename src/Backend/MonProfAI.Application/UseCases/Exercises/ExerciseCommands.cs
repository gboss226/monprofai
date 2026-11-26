using FluentValidation;
using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Exercises;

/// <summary>Thrown when no notion was given and the model could not find one.</summary>
public sealed class NotionNotDetectedException : Exception
{
    public NotionNotDetectedException() : base("No notion detected for this exercise") { }
}

// ---------- Create ----------

/// <summary>Saves an exercise. Without <paramref name="NotionId"/>, the notion is detected by the model.</summary>
/// <exception cref="NotionNotDetectedException">No notion given and none detected: the student must choose one.</exception>
public sealed record CreateExerciseCommand(Guid UserId, string Statement, Guid? NotionId) : IRequest<CreatedExerciseDto>;

public sealed record CreatedExerciseDto(Guid Id, Guid NotionId, bool NotionDetected);

public class CreateExerciseCommandValidator : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseCommandValidator(INotionRepository notionRepository)
    {
        RuleFor(x => x.Statement)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("L'énoncé est vide")
            .MaximumLength(ExerciseLimits.MaxStatementLength)
            .WithMessage($"L'énoncé ne doit pas dépasser {ExerciseLimits.MaxStatementLength} caractères");

        RuleFor(x => x.NotionId)
            .MustAsync(async (id, ct) => id is null || await notionRepository.GetByIdAsync(id.Value, ct) is not null)
            .WithMessage("Cette notion n'existe pas");
    }
}

public class CreateExerciseCommandHandler : IRequestHandler<CreateExerciseCommand, CreatedExerciseDto>
{
    private readonly IExerciseRepository _exercises;
    private readonly ILearnerProfileRepository _profiles;
    private readonly INotionDetector _notionDetector;

    public CreateExerciseCommandHandler(
        IExerciseRepository exercises, ILearnerProfileRepository profiles, INotionDetector notionDetector)
    {
        _exercises = exercises;
        _profiles = profiles;
        _notionDetector = notionDetector;
    }

    public async Task<CreatedExerciseDto> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        var statement = request.Statement.Trim();
        var notionId = request.NotionId;
        var detected = false;

        if (notionId is null)
        {
            var profile = await _profiles.GetByUserIdAsync(request.UserId);
            var level = profile is { OnboardingCompleted: true } ? profile.SchoolLevel : (SchoolLevel?)null;
            notionId = await _notionDetector.DetectAsync(statement, level, cancellationToken)
                       ?? throw new NotionNotDetectedException();
            detected = true;
        }

        var exercise = await _exercises.CreateAsync(new Exercise
        {
            UserId = request.UserId,
            NotionId = notionId.Value,
            Statement = statement,
            NotionDetected = detected,
            Status = ExerciseStatus.ToSolve,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        return new CreatedExerciseDto(exercise.Id, exercise.NotionId, detected);
    }
}

// ---------- Get ----------

/// <summary>One of the student's exercises, or null if it is not his.</summary>
public sealed record GetExerciseQuery(Guid UserId, Guid ExerciseId) : IRequest<ExerciseDto?>;

/// <param name="Status">"ToSolve", "InProgress" or "Solved".</param>
public sealed record ExerciseDto(
    Guid Id,
    string Statement,
    string Status,
    bool NotionDetected,
    DateTime CreatedAt,
    ExerciseNotionDto Notion);

/// <param name="SchoolLevel">Enum name, e.g. "Quatrieme".</param>
public sealed record ExerciseNotionDto(Guid Id, string Title, string ChapterTitle, string SchoolLevel, string SubjectName);

public class GetExerciseQueryHandler : IRequestHandler<GetExerciseQuery, ExerciseDto?>
{
    private readonly IExerciseRepository _exercises;

    public GetExerciseQueryHandler(IExerciseRepository exercises)
    {
        _exercises = exercises;
    }

    public async Task<ExerciseDto?> Handle(GetExerciseQuery request, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetOwnedAsync(request.ExerciseId, request.UserId, cancellationToken);
        if (exercise is null)
            return null;

        var notion = exercise.Notion;
        return new ExerciseDto(
            exercise.Id,
            exercise.Statement,
            exercise.Status.ToString(),
            exercise.NotionDetected,
            exercise.CreatedAt,
            new ExerciseNotionDto(
                notion.Id, notion.Title, notion.Chapter.Title, notion.Chapter.SchoolLevel.ToString(), notion.Chapter.Subject.Name));
    }
}
