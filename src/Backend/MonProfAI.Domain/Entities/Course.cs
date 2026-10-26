namespace MonProfAI.Domain.Entities;

public class Course
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<Lesson> Lessons { get; set; } = [];
    public List<UserCourse> Enrollments { get; set; } = [];
}
