namespace MonProfAI.Domain.Mastery;

/// <summary>How an exercise ended, as far as mastery is concerned.</summary>
/// <param name="HintsUsed">Hints unlocked before the end (0 to 4).</param>
/// <param name="Attempts">Answers proposed before the end, the right one included.</param>
public sealed record ExerciseOutcome(bool Succeeded, int HintsUsed, int Attempts);

/// <summary>
/// The rule that moves a student's mastery of a notion (a score from 0 to 1) after an exercise.
/// Pure and isolated on purpose: it can be tuned or replaced without touching the rest, and its tests pin its behaviour.
///
/// The score moves towards a target by a fraction of the remaining distance (exponential smoothing):
/// a success pulls it towards 1, a failure towards 0. The fraction (the "rate") says how much one exercise counts:
/// <list type="bullet">
///   <item>success with no hint: strong rise (half of the way to 1);</item>
///   <item>each hint used makes the rise smaller, down to a slight rise with three hints or more;</item>
///   <item>wrong answers before the right one shrink the rise a little more;</item>
///   <item>failure (solution read before succeeding): drop by 30 % of the current score.</item>
/// </list>
/// Moving by a fraction of the distance keeps the score within [0, 1] and makes each exercise count less as the
/// score nears its bound, so one lucky or unlucky exercise never swings a well-established score completely.
/// </summary>
public static class MasteryRule
{
    /// <summary>Score assumed for a notion the student has never worked on.</summary>
    public const double InitialScore = 0.3;

    /// <summary>Share of the way to 1 covered by a success, by number of hints used (3 or more: last value).</summary>
    public static readonly IReadOnlyList<double> SuccessRateByHints = new[] { 0.5, 0.35, 0.2, 0.1 };

    /// <summary>Each wrong answer before the right one removes this share of the success rate…</summary>
    public const double PenaltyPerWrongAttempt = 0.1;

    /// <summary>…but never more than this in total.</summary>
    public const double MaxAttemptPenalty = 0.5;

    /// <summary>Share of the score lost on a failure.</summary>
    public const double FailureRate = 0.3;

    /// <param name="current">Current score, or null if the notion was never worked on.</param>
    /// <returns>The new score, within [0, 1].</returns>
    public static double Next(double? current, ExerciseOutcome outcome)
    {
        var score = Math.Clamp(current ?? InitialScore, 0, 1);

        if (!outcome.Succeeded)
            return Math.Clamp(score - FailureRate * score, 0, 1);

        var hints = Math.Max(0, outcome.HintsUsed);
        var rate = SuccessRateByHints[Math.Min(hints, SuccessRateByHints.Count - 1)];

        var wrongAttempts = Math.Max(0, outcome.Attempts - 1);
        rate *= 1 - Math.Min(MaxAttemptPenalty, PenaltyPerWrongAttempt * wrongAttempts);

        return Math.Clamp(score + rate * (1 - score), 0, 1);
    }
}
