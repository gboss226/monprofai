using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.Interfaces;

/// <summary>Students' exercises. Reads take the owner's id: nobody else's exercise is ever returned.</summary>
public interface IExerciseRepository
{
    Task<Exercise> CreateAsync(Exercise exercise, CancellationToken cancellationToken = default);

    /// <summary>The exercise with its notion (chapter, subject) if it belongs to <paramref name="userId"/>.</summary>
    Task<Exercise?> GetOwnedAsync(Guid exerciseId, Guid userId, CancellationToken cancellationToken = default);
}
