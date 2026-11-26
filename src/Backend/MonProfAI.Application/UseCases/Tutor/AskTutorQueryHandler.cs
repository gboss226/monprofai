using System.Runtime.CompilerServices;
using System.Text;
using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Conversations;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Tutor;

public class AskTutorQueryHandler : IStreamRequestHandler<AskTutorQuery, string>
{
    /// <summary>
    /// Messages replayed to the model so that it keeps the thread. Older ones are dropped to bound the cost of a call;
    /// 20 messages is about ten exchanges, enough for one notion.
    /// </summary>
    public const int ContextMessages = 20;

    private readonly IAiChatService _aiChatService;
    private readonly ILearnerProfileRepository _learnerProfileRepository;
    private readonly INotionRepository _notionRepository;
    private readonly IConversationRepository _conversationRepository;

    public AskTutorQueryHandler(
        IAiChatService aiChatService,
        ILearnerProfileRepository learnerProfileRepository,
        INotionRepository notionRepository,
        IConversationRepository conversationRepository)
    {
        _aiChatService = aiChatService;
        _learnerProfileRepository = learnerProfileRepository;
        _notionRepository = notionRepository;
        _conversationRepository = conversationRepository;
    }

    public async IAsyncEnumerable<string> Handle(
        AskTutorQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetOwnedAsync(request.ConversationId, request.UserId, cancellationToken)
                           ?? throw new ConversationNotFoundException();

        var context = await BuildContextAsync(request.UserId, conversation.NotionId, cancellationToken);
        var history = await _conversationRepository.GetLastMessagesAsync(conversation.Id, ContextMessages, cancellationToken);
        var messages = ToModelMessages(history, request.Message);

        await _conversationRepository.AddMessageAsync(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        var answer = new StringBuilder();
        try
        {
            await foreach (var chunk in _aiChatService.StreamReplyAsync(
                               new AiChatRequest(TutorPromptBuilder.Build(context), messages), cancellationToken))
            {
                answer.Append(chunk);
                yield return chunk;
            }
        }
        finally
        {
            // Keep what the student saw, even if the answer was cut short (error, stop button, page closed).
            if (answer.Length > 0)
            {
                await _conversationRepository.AddMessageAsync(new ConversationMessage
                {
                    ConversationId = conversation.Id,
                    Role = MessageRole.Assistant,
                    Content = answer.ToString(),
                    CreatedAt = DateTime.UtcNow,
                }, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// The saved history plus the new question, as the model expects it: starting with the student
    /// and alternating roles. A question left without answer (provider error) is merged with the next one.
    /// </summary>
    public static List<AiChatMessage> ToModelMessages(IEnumerable<ConversationMessage> history, string newMessage)
    {
        var messages = new List<AiChatMessage>();
        var turns = history
            .Select(m => new AiChatMessage(m.Role == MessageRole.Assistant ? AiChatRole.Assistant : AiChatRole.User, m.Content))
            .Append(new AiChatMessage(AiChatRole.User, newMessage))
            .SkipWhile(m => m.Role == AiChatRole.Assistant);

        foreach (var turn in turns)
        {
            if (messages.Count > 0 && messages[^1].Role == turn.Role)
                messages[^1] = messages[^1] with { Content = messages[^1].Content + "\n\n" + turn.Content };
            else
                messages.Add(turn);
        }

        return messages;
    }

    private async Task<TutorPromptContext> BuildContextAsync(Guid userId, Guid? notionId, CancellationToken cancellationToken)
    {
        var profile = await _learnerProfileRepository.GetByUserIdAsync(userId);
        if (notionId is null)
            return new TutorPromptContext(profile, null);

        var notion = await _notionRepository.GetByIdAsync(notionId.Value, cancellationToken);
        if (notion is null)
            return new TutorPromptContext(profile, null);

        var notionIds = notion.Prerequisites.Select(p => p.PrerequisiteNotionId).Append(notion.Id).ToList();
        var masteries = await _notionRepository.GetMasteriesAsync(userId, notionIds, cancellationToken);

        return new TutorPromptContext(profile, notion, masteries.GetValueOrDefault(notion.Id), masteries);
    }
}
