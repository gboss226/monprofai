using Microsoft.EntityFrameworkCore;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Infrastructure.Data;

public class MonProfAIDbContext : DbContext
{
    public MonProfAIDbContext(DbContextOptions<MonProfAIDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<NotionMastery> NotionMasteries => Set<NotionMastery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Course configuration
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Description).IsRequired();
            entity.HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Lesson configuration
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Content).IsRequired();
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserCourse configuration
        modelBuilder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LearnerProfile configuration
        modelBuilder.Entity<LearnerProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<LearnerProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.SchoolLevel).IsRequired();
            entity.Property(e => e.LearningObjectives).HasMaxLength(1000);
            entity.Property(e => e.ExplanationPreferences).HasMaxLength(1000);
        });

        // NotionMastery configuration
        modelBuilder.Entity<NotionMastery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.NotionKey).IsRequired().HasMaxLength(256);
            entity.Property(e => e.MasteryScore).IsRequired();
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.NotionKey }).IsUnique();
        });
    }
}
