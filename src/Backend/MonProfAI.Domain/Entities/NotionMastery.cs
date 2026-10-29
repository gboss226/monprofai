namespace MonProfAI.Domain.Entities;

public class NotionMastery
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string NotionKey { get; set; } = null!;
    public double MasteryScore { get; set; }
    public int AttemptCount { get; set; }

    public DateTime LastActivity { get; set; }
    public DateTime CreatedAt { get; set; }
}
