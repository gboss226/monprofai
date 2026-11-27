using System.Text;
using FluentValidation;
using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Exercises;

public sealed class ExerciseNotFoundException : Exception
{
    public ExerciseNotFoundException() : base("Exercise not found") { }
}

/// <summary>The hint cannot be unlocked yet; <see cref="Exception.Message"/> says why, for the student.</summary>
public sealed class HintLockedException : Exception
{
    public HintLockedException(string reason) : base(reason) { }
}

public sealed record HintDto(int Level, string Title, string Content);

public sealed record AttemptDto(Guid Id, string Answer, bool IsCorrect, string Feedback, DateTime CreatedAt);

/// <summary>Shared by the two commands: loading the exercise and calling the model.</summary>
public abstract class ExerciseAiHandlerBase
{
    protected readonly IExerciseRepository Exercises;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IAiChatService _ai;

    protected ExerciseAiHandlerBase(IExerciseRepository exercises, ILearnerProfileRepository profiles, IAiChatService ai)
    {
        Exercises = exercises;
        _profiles = profiles;
        _ai = ai;
    }

    protected async Task<Exercise> LoadAsync(Guid exerciseId, Guid userId, CancellationToken cancellationToken) =>
        await Exercises.GetOwnedAsync(exerciseId, userId, cancellationToken) ?? throw new ExerciseNotFoundException();

    protected async Task<ExercisePromptContext> ContextAsync(Exercise exercise) =>
        new(exercise, await _profiles.GetByUserIdAsync(exercise.UserId));

    protected async Task<string> AskAsync(string systemPrompt, string message, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in _ai.StreamReplyAsync(
                           new AiChatRequest(systemPrompt, new[] { new AiChatMessage(AiChatRole.User, message) }),
                           cancellationToken))
            sb.Append(chunk);
        var text = sb.ToString().Trim();
        return text.Length > 0 ? text : throw new AiServiceException(AiErrorKind.InvalidResponse);
    }
}

// ---------- Unlock a hint ----------

/// <summary>Unlocks (generates and saves) the hint of the given level, or returns it if it was already unlocked.</summary>
/// <exception cref="ExerciseNotFoundException">Not one of the student's exercises.</exception>
/// <exception cref="HintLockedException">The rules of <see cref="HintRules"/> forbid this level for now.</exception>
public sealed record UnlockHintCommand(Guid UserId, Guid ExerciseId, int Level) : IRequest<HintDto>;

public class UnlockHintCommandHandler : ExerciseAiHandlerBase, IRequestHandler<UnlockHintCommand, HintDto>
{
    public UnlockHintCommandHandler(IExerciseRepository exercises, ILearnerProfileRepository profiles, IAiChatService ai)
        : base(exercises, profiles, ai) { }

    public async Task<HintDto> Handle(UnlockHintCommand request, CancellationToken cancellationToken)
    {
        var exercise = await LoadAsync(request.ExerciseId, request.UserId, cancellationToken);

        var existing = exercise.Hints.FirstOrDefault(h => h.Level == request.Level);
        if (existing is not null)
            return ToDto(existing);

        var decision = HintRules.CanUnlock(exercise, request.Level);
        if (!decision.Allowed)
            throw new HintLockedException(decision.Reason!);

        var prompt = ExercisePrompts.BuildHintPrompt(await ContextAsync(exercise), request.Level);
        var content = await AskAsync(prompt, $"Donne-moi l'indice de niveau {request.Level}.", cancellationToken);

        var hint = new ExerciseHint
        {
            ExerciseId = exercise.Id,
            Level = request.Level,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };
        var status = exercise.Status == ExerciseStatus.Solved ? ExerciseStatus.Solved : ExerciseStatus.InProgress;
        if (await Exercises.AddHintAsync(hint, status, cancellationToken))
        {
            // Reading the solution before succeeding ends the exercise as a failure.
            if (request.Level == HintLevels.Solution && exercise.Status != ExerciseStatus.Solved)
            {
                await ExerciseCompletion.FinishAsync(Exercises, exercise, succeeded: false,
                    hintsUsed: exercise.Hints.Count + 1, attempts: exercise.Attempts.Count, cancellationToken);
            }
            return ToDto(hint);
        }

        // Unlocked by a concurrent request: show the saved one so both tabs agree.
        var reloaded = await LoadAsync(request.ExerciseId, request.UserId, cancellationToken);
        return ToDto(reloaded.Hints.Single(h => h.Level == request.Level));
    }

    public static HintDto ToDto(ExerciseHint hint) => new(hint.Level, ExercisePrompts.LevelTitles[hint.Level], hint.Content);
}

// ---------- Submit an answer ----------

/// <summary>Checks the student's answer with the model, saves it and returns the verdict.</summary>
/// <exception cref="ExerciseNotFoundException">Not one of the student's exercises.</exception>
public sealed record SubmitAnswerCommand(Guid UserId, Guid ExerciseId, string Answer) : IRequest<AttemptDto>;

public class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.Answer)
            .Must(a => !string.IsNullOrWhiteSpace(a)).WithMessage("Ta réponse est vide")
            .MaximumLength(HintLevels.MaxAnswerLength)
            .WithMessage($"Ta réponse ne doit pas dépasser {HintLevels.MaxAnswerLength} caractères");
    }
}

public class SubmitAnswerCommandHandler : ExerciseAiHandlerBase, IRequestHandler<SubmitAnswerCommand, AttemptDto>
{
    public SubmitAnswerCommandHandler(IExerciseRepository exercises, ILearnerProfileRepository profiles, IAiChatService ai)
        : base(exercises, profiles, ai) { }

    public async Task<AttemptDto> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var exercise = await LoadAsync(request.ExerciseId, request.UserId, cancellationToken);
        var answer = request.Answer.Trim();

        var prompt = ExercisePrompts.BuildAnswerCheckPrompt(await ContextAsync(exercise));
        var reply = await AskAsync(prompt, $"Voici ma réponse :\n<reponse>\n{answer}\n</reponse>", cancellationToken);
        var (isCorrect, feedback) = ParseVerdict(reply);

        var attempt = new ExerciseAttempt
        {
            ExerciseId = exercise.Id,
            Answer = answer,
            IsCorrect = isCorrect,
            Feedback = feedback,
            CreatedAt = DateTime.UtcNow,
        };
        var status = isCorrect || exercise.Status == ExerciseStatus.Solved ? ExerciseStatus.Solved : ExerciseStatus.InProgress;
        await Exercises.AddAttemptAsync(attempt, status, cancellationToken);

        // The first right answer ends the exercise as a success (unless the solution was read before).
        if (isCorrect)
        {
            await ExerciseCompletion.FinishAsync(Exercises, exercise, succeeded: true,
                hintsUsed: exercise.Hints.Count, attempts: exercise.Attempts.Count + 1, cancellationToken);
        }

        return new AttemptDto(attempt.Id, attempt.Answer, attempt.IsCorrect, attempt.Feedback, attempt.CreatedAt);
    }

    /// <summary>Reads the verdict on the first line (JUSTE or FAUX) and the message for the student below it.</summary>
    /// <exception cref="AiServiceException">The model did not follow the format: no verdict can be trusted.</exception>
    public static (bool IsCorrect, string Feedback) ParseVerdict(string reply)
    {
        var lines = reply.Replace("\r\n", "\n").Split('\n');
        var first = lines[0].Trim().Trim('*', '`', ' ', '.', ':', '!').ToUpperInvariant();
        bool isCorrect;
        if (first == "JUSTE") isCorrect = true;
        else if (first == "FAUX") isCorrect = false;
        else throw new AiServiceException(AiErrorKind.InvalidResponse);

        var feedback = string.Join('\n', lines.Skip(1)).Trim();
        if (feedback.Length == 0)
            feedback = isCorrect ? "Bravo, c'est juste !" : "Ce n'est pas encore ça. Relis ta démarche étape par étape.";
        return (isCorrect, feedback);
    }
}
