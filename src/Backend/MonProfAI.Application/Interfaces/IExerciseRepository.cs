using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.Interfaces;

/// <summary>Students' exercises. Reads take the owner's id: nobody else's exercise is ever returned.</summary>
public interface IExerciseRepository
{
    Task<Exercise> CreateAsync(Exercise exercise, CancellationToken cancellationToken = default);

    /// <summary>
    /// The exercise with its notion (chapter, subject), hints and attempts if it belongs to <paramref name="userId"/>.
    /// </summary>
    Task<Exercise?> GetOwnedAsync(Guid exerciseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Saves a hint and the exercise's new status. False if this level was saved meanwhile.</summary>
    Task<bool> AddHintAsync(ExerciseHint hint, ExerciseStatus status, CancellationToken cancellationToken = default);

    /// <summary>Saves an attempt and the exercise's new status.</summary>
    Task AddAttemptAsync(ExerciseAttempt attempt, ExerciseStatus status, CancellationToken cancellationToken = default);

    /// <summary>The student's mastery score of a notion, or null if he never worked on it.</summary>
    Task<double?> GetMasteryScoreAsync(Guid userId, Guid notionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the result of a finished exercise and sets the notion's mastery to <c>result.MasteryAfter</c>, together.
    /// False (and nothing saved) if the exercise already had a result.
    /// </summary>
    Task<bool> SaveResultAsync(
        ExerciseResult result, Guid userId, Guid notionId, CancellationToken cancellationToken = default);

    /// <summary>The student's latest finished exercises, most recent first, with their notion.</summary>
    Task<IReadOnlyList<ExerciseResult>> GetRecentResultsAsync(
        Guid userId, int count, CancellationToken cancellationToken = default);
}
