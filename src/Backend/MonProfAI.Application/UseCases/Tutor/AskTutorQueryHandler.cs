using System.Runtime.CompilerServices;
using MediatR;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Application.UseCases.Tutor;

public class AskTutorQueryHandler : IStreamRequestHandler<AskTutorQuery, string>
{
    private readonly IAiChatService _aiChatService;
    private readonly ILearnerProfileRepository _learnerProfileRepository;
    private readonly INotionRepository _notionRepository;

    public AskTutorQueryHandler(
        IAiChatService aiChatService,
        ILearnerProfileRepository learnerProfileRepository,
        INotionRepository notionRepository)
    {
        _aiChatService = aiChatService;
        _learnerProfileRepository = learnerProfileRepository;
        _notionRepository = notionRepository;
    }

    public async IAsyncEnumerable<string> Handle(
        AskTutorQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(request, cancellationToken);

        var messages = (request.History ?? Array.Empty<TutorTurn>())
            .Select(t => new AiChatMessage(t.Role == "assistant" ? AiChatRole.Assistant : AiChatRole.User, t.Content))
            .Append(new AiChatMessage(AiChatRole.User, request.Message))
            .ToList();

        var chatRequest = new AiChatRequest(TutorPromptBuilder.Build(context), messages);

        await foreach (var chunk in _aiChatService.StreamReplyAsync(chatRequest, cancellationToken))
            yield return chunk;
    }

    private async Task<TutorPromptContext> BuildContextAsync(AskTutorQuery request, CancellationToken cancellationToken)
    {
        var profile = await _learnerProfileRepository.GetByUserIdAsync(request.UserId);
        if (request.NotionId is not { } notionId)
            return new TutorPromptContext(profile, null);

        var notion = await _notionRepository.GetByIdAsync(notionId, cancellationToken);
        if (notion is null)
            return new TutorPromptContext(profile, null);

        var notionIds = notion.Prerequisites.Select(p => p.PrerequisiteNotionId).Append(notion.Id).ToList();
        var masteries = await _notionRepository.GetMasteriesAsync(request.UserId, notionIds, cancellationToken);

        return new TutorPromptContext(
            profile,
            notion,
            masteries.GetValueOrDefault(notion.Id),
            masteries);
    }
}
