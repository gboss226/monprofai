namespace MonProfAI.Domain.Entities;

/// <summary>A chapter of a subject for a given school level, e.g. "Fractions" in 5ème maths.</summary>
public class Chapter
{
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public SchoolLevel SchoolLevel { get; set; }
    public string Title { get; set; } = null!;

    /// <summary>Position of the chapter within its subject and school level (1-based).</summary>
    public int Order { get; set; }

    public ICollection<Notion> Notions { get; set; } = new List<Notion>();
}
