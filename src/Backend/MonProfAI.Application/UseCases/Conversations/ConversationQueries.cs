using FluentValidation;
using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Conversations;

/// <summary>Thrown when a conversation does not exist or belongs to another student (indistinguishable on purpose).</summary>
public sealed class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException() : base("Conversation not found") { }
}

// ---------- Open (resume or start) ----------

/// <summary>
/// Returns the conversation to write to: <paramref name="ConversationId"/> if the student owns it,
/// otherwise a new conversation about <paramref name="NotionId"/>.
/// </summary>
/// <exception cref="ConversationNotFoundException">ConversationId is not one of the student's.</exception>
/// <exception cref="ValidationException">NotionId does not exist.</exception>
public sealed record OpenConversationCommand(Guid UserId, Guid? ConversationId, Guid? NotionId) : IRequest<Guid>;

public class OpenConversationCommandHandler : IRequestHandler<OpenConversationCommand, Guid>
{
    private readonly IConversationRepository _conversations;
    private readonly INotionRepository _notions;

    public OpenConversationCommandHandler(IConversationRepository conversations, INotionRepository notions)
    {
        _conversations = conversations;
        _notions = notions;
    }

    public async Task<Guid> Handle(OpenConversationCommand request, CancellationToken cancellationToken)
    {
        if (request.ConversationId is { } conversationId)
        {
            var existing = await _conversations.GetOwnedAsync(conversationId, request.UserId, cancellationToken);
            return existing?.Id ?? throw new ConversationNotFoundException();
        }

        if (request.NotionId is { } notionId && await _notions.GetByIdAsync(notionId, cancellationToken) is null)
            throw new ValidationException("Cette notion n'existe pas");

        var conversation = await _conversations.CreateAsync(request.UserId, request.NotionId, cancellationToken);
        return conversation.Id;
    }
}

// ---------- List ----------

public sealed record ListConversationsQuery(Guid UserId) : IRequest<IReadOnlyList<ConversationSummaryDto>>;

/// <param name="Preview">Start of the student's first message, to recognise the conversation.</param>
public sealed record ConversationSummaryDto(
    Guid Id,
    Guid? NotionId,
    string? NotionTitle,
    string? ChapterTitle,
    string Preview,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class ListConversationsQueryHandler : IRequestHandler<ListConversationsQuery, IReadOnlyList<ConversationSummaryDto>>
{
    public const int PreviewLength = 90;

    private readonly IConversationRepository _conversations;

    public ListConversationsQueryHandler(IConversationRepository conversations)
    {
        _conversations = conversations;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> Handle(
        ListConversationsQuery request, CancellationToken cancellationToken)
    {
        var summaries = await _conversations.ListForUserAsync(request.UserId, cancellationToken);
        return summaries
            .Select(s => new ConversationSummaryDto(
                s.Id, s.NotionId, s.NotionTitle, s.ChapterTitle, Truncate(s.FirstMessage),
                s.MessageCount, s.CreatedAt, s.UpdatedAt))
            .ToList();
    }

    private static string Truncate(string text)
    {
        var singleLine = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= PreviewLength ? singleLine : singleLine[..(PreviewLength - 1)].TrimEnd() + "…";
    }
}

// ---------- Detail ----------

/// <summary>A conversation with all its messages, or null if it is not one of the student's.</summary>
public sealed record GetConversationQuery(Guid UserId, Guid ConversationId) : IRequest<ConversationDto?>;

public sealed record ConversationDto(
    Guid Id,
    Guid? NotionId,
    DateTime CreatedAt,
    IReadOnlyList<ConversationMessageDto> Messages);

/// <param name="Role">"user" or "assistant", as in the chat API.</param>
public sealed record ConversationMessageDto(Guid Id, string Role, string Content, DateTime CreatedAt);

public class GetConversationQueryHandler : IRequestHandler<GetConversationQuery, ConversationDto?>
{
    private readonly IConversationRepository _conversations;

    public GetConversationQueryHandler(IConversationRepository conversations)
    {
        _conversations = conversations;
    }

    public async Task<ConversationDto?> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetOwnedWithMessagesAsync(
            request.ConversationId, request.UserId, cancellationToken);
        if (conversation is null)
            return null;

        return new ConversationDto(
            conversation.Id,
            conversation.NotionId,
            conversation.CreatedAt,
            conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ConversationMessageDto(m.Id, RoleName(m.Role), m.Content, m.CreatedAt))
                .ToList());
    }

    public static string RoleName(MessageRole role) => role == MessageRole.Assistant ? "assistant" : "user";
}
