using Microsoft.EntityFrameworkCore;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Infrastructure.Seeding;

/// <summary>Number of rows created by one run. A second run on the same database creates nothing.</summary>
public sealed record SeedResult(
    int SubjectsCreated,
    int ChaptersCreated,
    int NotionsCreated,
    int PrerequisitesCreated,
    int UsersCreated,
    int ProfilesCreated,
    int MasteriesCreated)
{
    public int Total => SubjectsCreated + ChaptersCreated + NotionsCreated + PrerequisitesCreated
                        + UsersCreated + ProfilesCreated + MasteriesCreated;
}

/// <summary>
/// Fills a database with <see cref="DevSeedData"/>. Idempotent: every row is looked up by its natural key
/// (subject code, chapter level and order, notion order, user email...) and updated instead of duplicated.
/// Rows that are not part of the seed are never touched.
/// </summary>
public sealed class DevDataSeeder
{
    private readonly MonProfAIDbContext _db;
    private readonly IAuthService _authService;

    public DevDataSeeder(MonProfAIDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        // The in-memory provider used by tests has no transactions.
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var (subjects, chapters, notionsByKey, notionsCreated) = await SeedReferentialAsync(cancellationToken);
        var prerequisites = await SeedPrerequisitesAsync(notionsByKey, cancellationToken);
        var (users, profiles, masteries) = await SeedStudentsAsync(notionsByKey, cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new SeedResult(subjects, chapters, notionsCreated, prerequisites, users, profiles, masteries);
    }

    private async Task<(int Subjects, int Chapters, Dictionary<string, Notion> NotionsByKey, int Notions)>
        SeedReferentialAsync(CancellationToken cancellationToken)
    {
        int subjectsCreated = 0, chaptersCreated = 0, notionsCreated = 0;

        var subject = await _db.Subjects.SingleOrDefaultAsync(s => s.Code == DevSeedData.SubjectCode, cancellationToken);
        if (subject is null)
        {
            subject = new Subject { Code = DevSeedData.SubjectCode };
            _db.Subjects.Add(subject);
            subjectsCreated++;
        }
        subject.Name = DevSeedData.SubjectName;

        var existingChapters = await _db.Chapters
            .Where(c => c.SubjectId == subject.Id)
            .Include(c => c.Notions)
            .ToListAsync(cancellationToken);

        var notionsByKey = new Dictionary<string, Notion>();

        foreach (var chapterData in DevSeedData.Chapters)
        {
            var chapter = existingChapters.SingleOrDefault(c =>
                c.SchoolLevel == chapterData.Level && c.Order == chapterData.Order);
            if (chapter is null)
            {
                chapter = new Chapter { Subject = subject, SchoolLevel = chapterData.Level, Order = chapterData.Order };
                _db.Chapters.Add(chapter);
                chaptersCreated++;
            }
            chapter.Title = chapterData.Title;

            for (var i = 0; i < chapterData.Notions.Count; i++)
            {
                var notionData = chapterData.Notions[i];
                var order = i + 1;
                var notion = chapter.Notions.SingleOrDefault(n => n.Order == order);
                if (notion is null)
                {
                    notion = new Notion { Chapter = chapter, Order = order };
                    chapter.Notions.Add(notion);
                    notionsCreated++;
                }
                notion.Title = notionData.Title;
                notion.ShortDescription = notionData.Description;
                notionsByKey.Add(notionData.Key, notion);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (subjectsCreated, chaptersCreated, notionsByKey, notionsCreated);
    }

    private async Task<int> SeedPrerequisitesAsync(
        IReadOnlyDictionary<string, Notion> notionsByKey, CancellationToken cancellationToken)
    {
        var notionIds = notionsByKey.Values.Select(n => n.Id).ToList();
        var existing = (await _db.NotionPrerequisites
                .Where(p => notionIds.Contains(p.NotionId))
                .Select(p => new { p.NotionId, p.PrerequisiteNotionId })
                .ToListAsync(cancellationToken))
            .Select(p => (p.NotionId, p.PrerequisiteNotionId))
            .ToHashSet();

        var created = 0;
        foreach (var edge in DevSeedData.Prerequisites)
        {
            var notionId = notionsByKey[edge.NotionKey].Id;
            var prerequisiteId = notionsByKey[edge.PrerequisiteKey].Id;
            if (!existing.Add((notionId, prerequisiteId)))
                continue;

            _db.NotionPrerequisites.Add(new NotionPrerequisite
            {
                NotionId = notionId,
                PrerequisiteNotionId = prerequisiteId,
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<(int Users, int Profiles, int Masteries)> SeedStudentsAsync(
        IReadOnlyDictionary<string, Notion> notionsByKey, CancellationToken cancellationToken)
    {
        int usersCreated = 0, profilesCreated = 0, masteriesCreated = 0;
        var now = DateTime.UtcNow;

        foreach (var student in DevSeedData.Students)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == student.Email, cancellationToken);
            if (user is null)
            {
                user = new User { Email = student.Email, CreatedAt = now };
                _db.Users.Add(user);
                usersCreated++;
            }
            user.FullName = student.FullName;
            user.Role = UserRole.Student;
            user.IsActive = true;
            // Re-hashing on every run would rewrite the row for nothing: BCrypt salts differ each time.
            if (user.PasswordHash is null || !_authService.VerifyPassword(DevSeedData.StudentPassword, user.PasswordHash))
            {
                user.PasswordHash = _authService.HashPassword(DevSeedData.StudentPassword);
                if (_db.Entry(user).State != EntityState.Added)
                    user.UpdatedAt = now;
            }

            var profile = await _db.LearnerProfiles.SingleOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            if (profile is null)
            {
                profile = new LearnerProfile { User = user, CreatedAt = now };
                _db.LearnerProfiles.Add(profile);
                profilesCreated++;
            }
            profile.SchoolLevel = student.Level;
            profile.StrugglingSubjects = student.StrugglingSubjects.ToList();
            profile.Goal = student.Goal;
            profile.PreferredLearningStyle = student.Style;
            profile.LearningObjectives = student.Objectives;
            profile.OnboardingCompleted = student.OnboardingCompleted;
            profile.UpdatedAt = now;

            var masteries = await _db.NotionMasteries
                .Where(m => m.UserId == user.Id)
                .ToListAsync(cancellationToken);

            foreach (var masteryData in student.Masteries)
            {
                var notionId = notionsByKey[masteryData.NotionKey].Id;
                var mastery = masteries.SingleOrDefault(m => m.NotionId == notionId);
                if (mastery is null)
                {
                    mastery = new NotionMastery
                    {
                        User = user,
                        NotionId = notionId,
                        CreatedAt = now,
                        LastActivity = now.AddDays(-masteryData.DaysAgo),
                    };
                    _db.NotionMasteries.Add(mastery);
                    masteriesCreated++;
                }
                mastery.MasteryScore = masteryData.Score;
                mastery.AttemptCount = masteryData.Attempts;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (usersCreated, profilesCreated, masteriesCreated);
    }
}
