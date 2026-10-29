namespace MonProfAI.Domain.Entities;

public class LearnerProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public SchoolLevel SchoolLevel { get; set; }
    public List<string> StrugglingSubjects { get; set; } = new();
    public LearningGoal? Goal { get; set; }
    public LearningStyle? PreferredLearningStyle { get; set; }
    public string? LearningObjectives { get; set; }

    public bool OnboardingCompleted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum SchoolLevel
{
    Sixieme = 0,      // 6ème
    Cinquieme = 1,    // 5ème
    Quatrieme = 2,    // 4ème
    Troisieme = 3,    // 3ème
    Seconde = 4,      // 2nde
    Premiere = 5,     // 1ère
    Terminale = 6     // Terminale
}

public enum LearningGoal
{
    Brevet = 0,
    Bac = 1,
    ImproveGrades = 2,
    Other = 3
}

public enum LearningStyle
{
    ConcreteExamples = 0,
    Diagrams = 1,
    ShortExplanations = 2,
    DetailedExplanations = 3
}
