namespace MonProfAI.Domain.Entities;

/// <summary>
/// How a finished exercise went: one row per exercise, written once when it ends (right answer, or solution
/// unlocked before succeeding). Not to be confused with <see cref="ExerciseAttempt"/>, one row per proposed answer.
/// </summary>
public class ExerciseResult
{
    public Guid Id { get; set; }

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    /// <summary>Hints unlocked before the end, the full solution included.</summary>
    public int HintsUsed { get; set; }

    /// <summary>Answers proposed before the end, the right one included.</summary>
    public int Attempts { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>Mastery score of the notion before and after this exercise, to explain how it moved.</summary>
    public double MasteryBefore { get; set; }
    public double MasteryAfter { get; set; }

    public DateTime CompletedAt { get; set; }
}
