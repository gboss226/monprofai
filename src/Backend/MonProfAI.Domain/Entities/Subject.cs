namespace MonProfAI.Domain.Entities;

/// <summary>A school subject, e.g. "maths". Root of the notion referential.</summary>
public class Subject
{
    public Guid Id { get; set; }

    /// <summary>Stable technical code, e.g. "maths". Unique.</summary>
    public string Code { get; set; } = null!;

    /// <summary>Display name, e.g. "Mathématiques".</summary>
    public string Name { get; set; } = null!;

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
