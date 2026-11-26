namespace MonProfAI.Domain.Entities;

/// <summary>Dependency edge: <see cref="Notion"/> requires <see cref="PrerequisiteNotion"/> to be mastered first.</summary>
public class NotionPrerequisite
{
    public Guid NotionId { get; set; }
    public Notion Notion { get; set; } = null!;

    public Guid PrerequisiteNotionId { get; set; }
    public Notion PrerequisiteNotion { get; set; } = null!;
}
