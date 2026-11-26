using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.Interfaces;

/// <summary>
/// Tutor conversations. Every read takes the owner's id: a student can only ever reach his own conversations,
/// and someone else's conversation looks exactly like one that does not exist.
/// </summary>
public interface IConversationRepository
{
    Task<Conversation> CreateAsync(Guid userId, Guid? notionId, CancellationToken cancellationToken = default);

    /// <summary>The conversation if it exists and belongs to <paramref name="userId"/>, without its messages.</summary>
    Task<Conversation?> GetOwnedAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Like <see cref="GetOwnedAsync"/>, with the notion (chapter, subject) and all messages, oldest first.</summary>
    Task<Conversation?> GetOwnedWithMessagesAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The <paramref name="count"/> most recent messages of a conversation, oldest first.</summary>
    Task<IReadOnlyList<ConversationMessage>> GetLastMessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken = default);

    /// <summary>Saves a message and moves the conversation's <c>UpdatedAt</c> to its date.</summary>
    Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default);

    /// <summary>The student's conversations that have at least one message, most recent first.</summary>
    Task<IReadOnlyList<ConversationSummary>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record ConversationSummary(
    Guid Id,
    Guid? NotionId,
    string? NotionTitle,
    string? ChapterTitle,
    string FirstMessage,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
