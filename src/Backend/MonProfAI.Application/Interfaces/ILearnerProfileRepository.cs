using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.Interfaces;

public interface ILearnerProfileRepository
{
    Task<LearnerProfile?> GetByUserIdAsync(Guid userId);
    Task CreateAsync(LearnerProfile profile);
    Task UpdateAsync(LearnerProfile profile);
}
