namespace MonProfAI.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int Order { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
