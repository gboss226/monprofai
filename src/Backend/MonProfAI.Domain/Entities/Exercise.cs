namespace MonProfAI.Domain.Entities;

/// <summary>An exercise statement entered by a student, attached to the notion it practises.</summary>
public class Exercise
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid NotionId { get; set; }
    public Notion Notion { get; set; } = null!;

    /// <summary>The statement as typed or pasted by the student (plain text, formulas in LaTeX).</summary>
    public string Statement { get; set; } = null!;

    /// <summary>True when the model picked the notion; false when the student chose it.</summary>
    public bool NotionDetected { get; set; }

    public ExerciseStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}

public static class ExerciseLimits
{
    /// <summary>Long enough for a multi-question exercise, short enough to bound the cost of a model call.</summary>
    public const int MaxStatementLength = 4000;
}

public enum ExerciseStatus
{
    /// <summary>Entered, resolution not started yet.</summary>
    ToSolve = 0,
    InProgress = 1,
    Solved = 2,
}
