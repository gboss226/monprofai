using Microsoft.EntityFrameworkCore;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly MonProfAIDbContext _context;

    public ConversationRepository(MonProfAIDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation> CreateAsync(Guid userId, Guid? notionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var conversation = new Conversation { UserId = userId, NotionId = notionId, CreatedAt = now, UpdatedAt = now };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public Task<Conversation?> GetOwnedAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);
    }

    public Task<Conversation?> GetOwnedWithMessagesAsync(
        Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .AsNoTracking()
            .Include(c => c.Notion!)
            .ThenInclude(n => n.Chapter)
            .ThenInclude(ch => ch.Subject)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetLastMessagesAsync(
        Guid conversationId, int count, CancellationToken cancellationToken = default)
    {
        var latest = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        latest.Reverse();
        return latest;
    }

    public async Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .FirstAsync(c => c.Id == message.ConversationId, cancellationToken);
        conversation.UpdatedAt = message.CreatedAt;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSummary>> ListForUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Messages.Any())
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummary(
                c.Id,
                c.NotionId,
                c.Notion != null ? c.Notion.Title : null,
                c.Notion != null ? c.Notion.Chapter.Title : null,
                c.Messages
                    .Where(m => m.Role == MessageRole.User)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault() ?? "",
                c.Messages.Count,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
