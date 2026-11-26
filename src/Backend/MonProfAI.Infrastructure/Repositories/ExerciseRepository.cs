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

    private async Task SetStatusAsync(Guid exerciseId, ExerciseStatus status, CancellationToken cancellationToken)
    {
        var exercise = await _context.Exercises.FirstAsync(e => e.Id == exerciseId, cancellationToken);
        exercise.Status = status;
    }
}
