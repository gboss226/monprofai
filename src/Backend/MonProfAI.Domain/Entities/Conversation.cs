namespace MonProfAI.Domain.Entities;

/// <summary>A tutoring session between a student and the AI tutor, optionally about one notion.</summary>
public class Conversation
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Notion chosen when the conversation started. Null for a free conversation.</summary>
    public Guid? NotionId { get; set; }
    public Notion? Notion { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Time of the last message, used to sort the student's history.</summary>
    public DateTime UpdatedAt { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}

public enum MessageRole
{
    User = 0,
    Assistant = 1,
}

/// <summary>One message of a <see cref="Conversation"/>, from the student or the tutor.</summary>
public class ConversationMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public MessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
