using Microsoft.EntityFrameworkCore;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Infrastructure.Repositories;

public class LearnerProfileRepository : ILearnerProfileRepository
{
    private readonly MonProfAIDbContext _context;

    public LearnerProfileRepository(MonProfAIDbContext context)
    {
        _context = context;
    }

    public Task<LearnerProfile?> GetByUserIdAsync(Guid userId)
    {
        return _context.LearnerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task CreateAsync(LearnerProfile profile)
    {
        _context.LearnerProfiles.Add(profile);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LearnerProfile profile)
    {
        _context.LearnerProfiles.Update(profile);
        await _context.SaveChangesAsync();
    }
}
