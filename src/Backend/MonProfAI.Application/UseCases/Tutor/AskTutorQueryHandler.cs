using System.Runtime.CompilerServices;
using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Tutor;

public class AskTutorQueryHandler : IStreamRequestHandler<AskTutorQuery, string>
{
    private readonly IAiChatService _aiChatService;
    private readonly ILearnerProfileRepository _learnerProfileRepository;

    public AskTutorQueryHandler(IAiChatService aiChatService, ILearnerProfileRepository learnerProfileRepository)
    {
        _aiChatService = aiChatService;
        _learnerProfileRepository = learnerProfileRepository;
    }

    public async IAsyncEnumerable<string> Handle(
        AskTutorQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var profile = await _learnerProfileRepository.GetByUserIdAsync(request.UserId);

        var messages = (request.History ?? Array.Empty<TutorTurn>())
            .Select(t => new AiChatMessage(t.Role == "assistant" ? AiChatRole.Assistant : AiChatRole.User, t.Content))
            .Append(new AiChatMessage(AiChatRole.User, request.Message))
            .ToList();

        var chatRequest = new AiChatRequest(BuildSystemPrompt(profile), messages);

        await foreach (var chunk in _aiChatService.StreamReplyAsync(chatRequest, cancellationToken))
            yield return chunk;
    }

    public static string BuildSystemPrompt(Domain.Entities.LearnerProfile? profile)
    {
        var prompt =
            "Tu es MonProfAI, un professeur particulier bienveillant pour des élèves du collège et du lycée en France. " +
            "Réponds en français, avec des phrases simples adaptées à l'âge de l'élève. " +
            "Aide l'élève à comprendre et à raisonner par lui-même : explique la méthode, donne des indices " +
            "et pose des questions plutôt que de livrer directement la réponse d'un exercice. " +
            "Si la question n'a rien à voir avec les études, ramène gentiment la conversation vers le travail scolaire. " +
            "Ne demande jamais d'informations personnelles à l'élève.";

        if (profile is { OnboardingCompleted: true })
        {
            prompt += $" L'élève est en {LevelLabel(profile.SchoolLevel)}.";
            if (profile.StrugglingSubjects.Count > 0)
                prompt += $" Matières où il se sent en difficulté : {string.Join(", ", profile.StrugglingSubjects)}.";
            if (profile.PreferredLearningStyle is { } style)
                prompt += $" Il préfère apprendre avec {StyleLabel(style)}.";
        }

        return prompt;
    }

    private static string LevelLabel(SchoolLevel level) => level switch
    {
        SchoolLevel.Sixieme => "6ème",
        SchoolLevel.Cinquieme => "5ème",
        SchoolLevel.Quatrieme => "4ème",
        SchoolLevel.Troisieme => "3ème",
        SchoolLevel.Seconde => "2nde",
        SchoolLevel.Premiere => "1ère",
        _ => "Terminale",
    };

    private static string StyleLabel(LearningStyle style) => style switch
    {
        LearningStyle.ConcreteExamples => "des exemples concrets",
        LearningStyle.Diagrams => "des schémas",
        LearningStyle.ShortExplanations => "des explications courtes",
        _ => "des explications détaillées",
    };
}
