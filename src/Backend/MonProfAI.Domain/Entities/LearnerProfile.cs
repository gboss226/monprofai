namespace MonProfAI.Domain.Entities;

public class LearnerProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public SchoolLevel SchoolLevel { get; set; }
    public string? LearningObjectives { get; set; }
    public string? ExplanationPreferences { get; set; }

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
