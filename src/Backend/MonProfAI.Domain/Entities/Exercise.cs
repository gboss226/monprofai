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

    public ICollection<ExerciseHint> Hints { get; set; } = new List<ExerciseHint>();
    public ICollection<ExerciseAttempt> Attempts { get; set; } = new List<ExerciseAttempt>();
}

/// <summary>
/// A hint unlocked by the student. Levels go from the least to the most revealing; see <see cref="HintLevels"/>.
/// </summary>
public class ExerciseHint
{
    public Guid Id { get; set; }

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    /// <summary>1 to <see cref="HintLevels.Solution"/>.</summary>
    public int Level { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>An answer proposed by the student, with the model's verdict.</summary>
public class ExerciseAttempt
{
    public Guid Id { get; set; }

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public string Answer { get; set; } = null!;
    public bool IsCorrect { get; set; }

    /// <summary>What is right, or where the mistake is, without giving the solution away.</summary>
    public string Feedback { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public static class HintLevels
{
    public const int NotionReminder = 1;
    public const int Method = 2;
    public const int FirstStep = 3;
    /// <summary>The full explained solution: the last level.</summary>
    public const int Solution = 4;

    /// <summary>Wrong answers after which the solution can be unlocked without the three hints.</summary>
    public const int AttemptsBeforeSolution = 3;

    public const int MaxAnswerLength = 2000;
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
