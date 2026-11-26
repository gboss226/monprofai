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
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.UserId == userId, cancellationToken);
    }
}
