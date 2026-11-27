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
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Notion> Notions => Set<Notion>();
    public DbSet<NotionPrerequisite> NotionPrerequisites => Set<NotionPrerequisite>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> Messages => Set<ConversationMessage>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseHint> ExerciseHints => Set<ExerciseHint>();
    public DbSet<ExerciseAttempt> ExerciseAttempts => Set<ExerciseAttempt>();

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
            entity.Property(e => e.StrugglingSubjects).IsRequired();
            entity.Property(e => e.LearningObjectives).HasMaxLength(1000);
            entity.Property(e => e.OnboardingCompleted).IsRequired();
        });

        // NotionMastery configuration
        modelBuilder.Entity<NotionMastery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Notion)
                .WithMany()
                .HasForeignKey(e => e.NotionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.MasteryScore).IsRequired();
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.NotionId }).IsUnique();
        });

        // Notion referential: Subject -> Chapter -> Notion, plus prerequisite graph.
        // Deletes are restricted so that removing referential data never silently wipes
        // chapters, notions or learners' mastery history.
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Subject)
                .WithMany(s => s.Chapters)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SchoolLevel).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Order).IsRequired();
            entity.HasIndex(e => new { e.SubjectId, e.SchoolLevel, e.Order }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Chapters_Order_Positive", "\"Order\" > 0"));
        });

        modelBuilder.Entity<Notion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Chapter)
                .WithMany(c => c.Notions)
                .HasForeignKey(e => e.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Order).IsRequired();
            entity.HasIndex(e => new { e.ChapterId, e.Order }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Notions_Order_Positive", "\"Order\" > 0"));
        });

        modelBuilder.Entity<NotionPrerequisite>(entity =>
        {
            entity.HasKey(e => new { e.NotionId, e.PrerequisiteNotionId });
            // Deleting a notion removes its own prerequisite edges...
            entity.HasOne(e => e.Notion)
                .WithMany(n => n.Prerequisites)
                .HasForeignKey(e => e.NotionId)
                .OnDelete(DeleteBehavior.Cascade);
            // ...but a notion still required by others cannot be deleted.
            entity.HasOne(e => e.PrerequisiteNotion)
                .WithMany(n => n.RequiredBy)
                .HasForeignKey(e => e.PrerequisiteNotionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.PrerequisiteNotionId);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_NotionPrerequisites_NotSelf", "\"NotionId\" <> \"PrerequisiteNotionId\""));
        });

        // Tutor conversations. Deleting a user deletes their conversations and messages;
        // a notion used by a conversation cannot be deleted (same rule as for mastery history).
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Notion)
                .WithMany()
                .HasForeignKey(e => e.NotionId)
                .OnDelete(DeleteBehavior.Restrict);
            // The history page lists a student's conversations, most recent first.
            entity.HasIndex(e => new { e.UserId, e.UpdatedAt });
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
        });

        // Exercises entered by students. Same deletion rules as conversations.
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Notion)
                .WithMany()
                .HasForeignKey(e => e.NotionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Statement).IsRequired().HasMaxLength(ExerciseLimits.MaxStatementLength);
            entity.Property(e => e.Status).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });

        modelBuilder.Entity<ExerciseHint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Exercise)
                .WithMany(x => x.Hints)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Content).IsRequired();
            // One hint per level: unlocking twice (double click, two tabs) cannot create duplicates.
            entity.HasIndex(e => new { e.ExerciseId, e.Level }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_ExerciseHints_Level", $"\"Level\" BETWEEN 1 AND {HintLevels.Solution}"));
        });

        modelBuilder.Entity<ExerciseAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Exercise)
                .WithMany(x => x.Attempts)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Answer).IsRequired().HasMaxLength(HintLevels.MaxAnswerLength);
            entity.Property(e => e.Feedback).IsRequired();
            entity.HasIndex(e => new { e.ExerciseId, e.CreatedAt });
        });
    }
}
