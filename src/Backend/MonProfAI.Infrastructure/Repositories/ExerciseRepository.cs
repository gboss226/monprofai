using Microsoft.EntityFrameworkCore;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Infrastructure.Repositories;

public class ExerciseRepository : IExerciseRepository
{
    private readonly MonProfAIDbContext _context;

    public ExerciseRepository(MonProfAIDbContext context)
    {
        _context = context;
    }

    public async Task<Exercise> CreateAsync(Exercise exercise, CancellationToken cancellationToken = default)
    {
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync(cancellationToken);
        return exercise;
    }

    public Task<Exercise?> GetOwnedAsync(Guid exerciseId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Exercises
            .AsNoTracking()
            .Include(e => e.Notion)
            .ThenInclude(n => n.Chapter)
            .ThenInclude(c => c.Subject)
            .Include(e => e.Hints.OrderBy(h => h.Level))
            .Include(e => e.Attempts.OrderBy(a => a.CreatedAt))
            .Include(e => e.Result)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.UserId == userId, cancellationToken);
    }

    public async Task<bool> AddHintAsync(ExerciseHint hint, ExerciseStatus status, CancellationToken cancellationToken = default)
    {
        await SetStatusAsync(hint.ExerciseId, status, cancellationToken);
        _context.ExerciseHints.Add(hint);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique (ExerciseId, Level): the same hint was unlocked by another request meanwhile.
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task AddAttemptAsync(ExerciseAttempt attempt, ExerciseStatus status, CancellationToken cancellationToken = default)
    {
        await SetStatusAsync(attempt.ExerciseId, status, cancellationToken);
        _context.ExerciseAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<double?> GetMasteryScoreAsync(Guid userId, Guid notionId, CancellationToken cancellationToken = default)
    {
        var mastery = await _context.NotionMasteries.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.NotionId == notionId, cancellationToken);
        return mastery?.MasteryScore;
    }

    public async Task<bool> SaveResultAsync(
        ExerciseResult result, Guid userId, Guid notionId, CancellationToken cancellationToken = default)
    {
        if (await _context.ExerciseResults.AnyAsync(r => r.ExerciseId == result.ExerciseId, cancellationToken))
            return false;

        var mastery = await _context.NotionMasteries
            .FirstOrDefaultAsync(m => m.UserId == userId && m.NotionId == notionId, cancellationToken);
        if (mastery is null)
        {
            mastery = new NotionMastery { UserId = userId, NotionId = notionId, CreatedAt = result.CompletedAt };
            _context.NotionMasteries.Add(mastery);
        }
        mastery.MasteryScore = result.MasteryAfter;
        mastery.AttemptCount++;
        mastery.LastActivity = result.CompletedAt;

        _context.ExerciseResults.Add(result);
        try
        {
            // One SaveChanges: the result and the mastery are written together or not at all.
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique ExerciseId: another request finished the same exercise meanwhile.
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<IReadOnlyList<ExerciseResult>> GetRecentResultsAsync(
        Guid userId, int count, CancellationToken cancellationToken = default)
    {
        return await _context.ExerciseResults
            .AsNoTracking()
            .Include(r => r.Exercise)
            .ThenInclude(e => e.Notion)
            .Where(r => r.Exercise.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    private async Task SetStatusAsync(Guid exerciseId, ExerciseStatus status, CancellationToken cancellationToken)
    {
        var exercise = await _context.Exercises.FirstAsync(e => e.Id == exerciseId, cancellationToken);
        exercise.Status = status;
    }
}
