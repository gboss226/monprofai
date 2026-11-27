using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Domain.Mastery;

namespace MonProfAI.Application.UseCases.Exercises;

/// <summary>
/// Ends an exercise: records how it went and moves the student's mastery of its notion with <see cref="MasteryRule"/>.
/// An exercise ends once, at the first of: a right answer (success), or the full solution unlocked before any
/// right answer (failure). Later answers or hints change nothing.
/// </summary>
public static class ExerciseCompletion
{
    /// <param name="hintsUsed">Hints unlocked so far, the one just unlocked included.</param>
    /// <param name="attempts">Answers proposed so far, the one just checked included.</param>
    /// <returns>The saved result, or null if the exercise was already finished.</returns>
    public static async Task<ExerciseResult?> FinishAsync(
        IExerciseRepository exercises, Exercise exercise, bool succeeded, int hintsUsed, int attempts,
        CancellationToken cancellationToken)
    {
        if (exercise.Result is not null)
            return null;

        var before = await exercises.GetMasteryScoreAsync(exercise.UserId, exercise.NotionId, cancellationToken);
        var after = MasteryRule.Next(before, new ExerciseOutcome(succeeded, hintsUsed, attempts));

        var result = new ExerciseResult
        {
            ExerciseId = exercise.Id,
            HintsUsed = hintsUsed,
            Attempts = attempts,
            Succeeded = succeeded,
            MasteryBefore = before ?? MasteryRule.InitialScore,
            MasteryAfter = after,
            CompletedAt = DateTime.UtcNow,
        };

        return await exercises.SaveResultAsync(result, exercise.UserId, exercise.NotionId, cancellationToken)
            ? result
            : null;
    }
}
