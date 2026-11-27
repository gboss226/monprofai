using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Exercises;

/// <summary>
/// When a hint can be unlocked. Hints come one at a time, in order; the full solution only comes after the three
/// other hints, or after <see cref="HintLevels.AttemptsBeforeSolution"/> wrong answers, or once the exercise is solved.
/// Enforced on the server: the page only mirrors these rules.
/// </summary>
public static class HintRules
{
    public sealed record Decision(bool Allowed, string? Reason = null);

    public static int WrongAttempts(Exercise exercise) => exercise.Attempts.Count(a => !a.IsCorrect);

    public static Decision CanUnlock(Exercise exercise, int level)
    {
        if (level < 1 || level > HintLevels.Solution)
            return new Decision(false, "Ce niveau d'indice n'existe pas.");

        var unlocked = exercise.Hints.Select(h => h.Level).ToHashSet();
        if (unlocked.Contains(level))
            return new Decision(true);

        var previousUnlocked = Enumerable.Range(1, level - 1).All(unlocked.Contains);

        if (level < HintLevels.Solution)
        {
            return previousUnlocked
                ? new Decision(true)
                : new Decision(false, $"Débloque d'abord l'indice {level - 1}.");
        }

        if (previousUnlocked || exercise.Status == ExerciseStatus.Solved
                             || WrongAttempts(exercise) >= HintLevels.AttemptsBeforeSolution)
            return new Decision(true);

        var attemptsLeft = HintLevels.AttemptsBeforeSolution - WrongAttempts(exercise);
        return new Decision(false,
            $"La solution complète se débloque après les 3 premiers indices ou après {attemptsLeft} essai(s) de plus.");
    }

    /// <summary>The level the page offers next, or null when every hint is unlocked.</summary>
    public static int? NextLevel(Exercise exercise)
    {
        var unlocked = exercise.Hints.Select(h => h.Level).ToHashSet();
        for (var level = 1; level <= HintLevels.Solution; level++)
        {
            if (!unlocked.Contains(level))
                return level;
        }
        return null;
    }
}
