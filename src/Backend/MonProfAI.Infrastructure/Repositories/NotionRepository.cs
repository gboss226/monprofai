using Microsoft.EntityFrameworkCore;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Infrastructure.Repositories;

public class NotionRepository : INotionRepository
{
    private readonly MonProfAIDbContext _context;

    public NotionRepository(MonProfAIDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Subject>> GetReferentialAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .AsNoTracking()
            .Include(s => s.Chapters)
            .ThenInclude(c => c.Notions)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Notion?> GetByIdAsync(Guid notionId, CancellationToken cancellationToken = default)
    {
        return _context.Notions
            .AsNoTracking()
            .Include(n => n.Chapter)
            .ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(n => n.Id == notionId, cancellationToken);
    }
}
