using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.Interfaces;

public interface INotionRepository
{
    /// <summary>Every subject with its chapters and their notions, in display order.</summary>
    Task<IReadOnlyList<Subject>> GetReferentialAsync(CancellationToken cancellationToken = default);

    /// <summary>A notion with its chapter, subject and prerequisite notions, or null.</summary>
    Task<Notion?> GetByIdAsync(Guid notionId, CancellationToken cancellationToken = default);

    /// <summary>The student's mastery of the given notions, by notion id. Notions never worked on are absent.</summary>
    Task<IReadOnlyDictionary<Guid, NotionMastery>> GetMasteriesAsync(
        Guid userId, IReadOnlyCollection<Guid> notionIds, CancellationToken cancellationToken = default);
}
