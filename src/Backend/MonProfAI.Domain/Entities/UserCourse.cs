namespace MonProfAI.Domain.Entities;

public class UserCourse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public float Progress { get; set; }
    public DateTime EnrolledAt { get; set; }
}
