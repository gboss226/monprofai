namespace MonProfAI.Domain.Entities;

/// <summary>An atomic notion to master, e.g. "Additionner deux fractions de même dénominateur".</summary>
public class Notion
{
    public Guid Id { get; set; }

    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? ShortDescription { get; set; }

    /// <summary>Position of the notion within its chapter (1-based).</summary>
    public int Order { get; set; }

    /// <summary>Notions that must be mastered before this one.</summary>
    public ICollection<NotionPrerequisite> Prerequisites { get; set; } = new List<NotionPrerequisite>();

    /// <summary>Notions that depend on this one.</summary>
    public ICollection<NotionPrerequisite> RequiredBy { get; set; } = new List<NotionPrerequisite>();
}
