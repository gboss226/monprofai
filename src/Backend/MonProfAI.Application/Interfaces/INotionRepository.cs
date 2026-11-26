using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.Interfaces;

public interface INotionRepository
{
    /// <summary>Every subject with its chapters and their notions, in display order.</summary>
    Task<IReadOnlyList<Subject>> GetReferentialAsync(CancellationToken cancellationToken = default);

    /// <summary>A notion with its chapter and subject, or null.</summary>
    Task<Notion?> GetByIdAsync(Guid notionId, CancellationToken cancellationToken = default);
}
