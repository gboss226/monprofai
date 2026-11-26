using Microsoft.EntityFrameworkCore;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;
using MonProfAI.Infrastructure.Seeding;
using MonProfAI.Infrastructure.Services;
using Xunit;

namespace MonProfAI.Tests.Seeding;

public class DevDataSeederTests
{
    private static MonProfAIDbContext NewContext(string databaseName) =>
        new(new DbContextOptionsBuilder<MonProfAIDbContext>().UseInMemoryDatabase(databaseName).Options);

    private static DevDataSeeder NewSeeder(MonProfAIDbContext db) =>
        new(db, new AuthService("test-secret-key-that-is-long-enough-for-hmac-sha256!"));

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesEverything()
    {
        await using var db = NewContext(Guid.NewGuid().ToString());

        var result = await NewSeeder(db).SeedAsync();

        var notionCount = DevSeedData.Chapters.Sum(c => c.Notions.Count);
        Assert.Equal(1, result.SubjectsCreated);
        Assert.Equal(DevSeedData.Chapters.Count, result.ChaptersCreated);
        Assert.Equal(notionCount, result.NotionsCreated);
        Assert.Equal(DevSeedData.Prerequisites.Count, result.PrerequisitesCreated);
        Assert.Equal(DevSeedData.Students.Count, result.UsersCreated);
        Assert.Equal(DevSeedData.Students.Count, result.ProfilesCreated);
        Assert.Equal(DevSeedData.Students.Sum(s => s.Masteries.Count), result.MasteriesCreated);

        Assert.Equal(notionCount, await db.Notions.CountAsync());
        Assert.Contains(await db.Chapters.ToListAsync(), c => c.SchoolLevel == SchoolLevel.Quatrieme);
        Assert.Contains(await db.Chapters.ToListAsync(), c => c.SchoolLevel == SchoolLevel.Troisieme);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_CreatesNoDuplicates()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var db = NewContext(databaseName))
            await NewSeeder(db).SeedAsync();

        await using var secondDb = NewContext(databaseName);
        var second = await NewSeeder(secondDb).SeedAsync();

        Assert.Equal(0, second.Total);
        Assert.Equal(1, await secondDb.Subjects.CountAsync());
        Assert.Equal(DevSeedData.Chapters.Count, await secondDb.Chapters.CountAsync());
        Assert.Equal(DevSeedData.Prerequisites.Count, await secondDb.NotionPrerequisites.CountAsync());
        Assert.Equal(DevSeedData.Students.Count, await secondDb.Users.CountAsync());
        Assert.Equal(DevSeedData.Students.Count, await secondDb.LearnerProfiles.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_SeededStudentsCanLogInWithDocumentedPassword()
    {
        await using var db = NewContext(Guid.NewGuid().ToString());
        await NewSeeder(db).SeedAsync();

        var authService = new AuthService("test-secret-key-that-is-long-enough-for-hmac-sha256!");
        foreach (var student in DevSeedData.Students)
        {
            var user = await db.Users.SingleAsync(u => u.Email == student.Email);
            Assert.True(authService.VerifyPassword(DevSeedData.StudentPassword, user.PasswordHash));
        }
    }

    [Fact]
    public async Task SeedAsync_LeavesOtherUsersUntouched()
    {
        await using var db = NewContext(Guid.NewGuid().ToString());
        var realUser = new User
        {
            Email = "someone@example.com",
            FullName = "Compte existant",
            PasswordHash = "unchanged-hash",
            Role = UserRole.Teacher,
        };
        db.Users.Add(realUser);
        await db.SaveChangesAsync();

        await NewSeeder(db).SeedAsync();

        var reloaded = await db.Users.SingleAsync(u => u.Email == "someone@example.com");
        Assert.Equal("unchanged-hash", reloaded.PasswordHash);
        Assert.Equal(UserRole.Teacher, reloaded.Role);
    }

    [Fact]
    public void SeedData_IsConsistent()
    {
        var keys = DevSeedData.Chapters.SelectMany(c => c.Notions).Select(n => n.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());

        foreach (var edge in DevSeedData.Prerequisites)
        {
            Assert.Contains(edge.NotionKey, keys);
            Assert.Contains(edge.PrerequisiteKey, keys);
            Assert.NotEqual(edge.NotionKey, edge.PrerequisiteKey);
        }

        // The prerequisite graph must not contain cycles.
        var visiting = new HashSet<string>();
        var done = new HashSet<string>();
        bool HasCycle(string key)
        {
            if (done.Contains(key)) return false;
            if (!visiting.Add(key)) return true;
            var cycle = DevSeedData.Prerequisites
                .Where(e => e.NotionKey == key)
                .Any(e => HasCycle(e.PrerequisiteKey));
            visiting.Remove(key);
            done.Add(key);
            return cycle;
        }
        Assert.DoesNotContain(keys, HasCycle);

        foreach (var student in DevSeedData.Students)
        {
            Assert.EndsWith(".test", student.Email);
            Assert.All(student.StrugglingSubjects, s => Assert.Contains(s, SchoolSubjects.All));
            Assert.All(student.Masteries, m => Assert.Contains(m.NotionKey, keys));
            Assert.All(student.Masteries, m => Assert.InRange(m.Score, 0.0, 1.0));
        }
    }
}
