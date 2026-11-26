using System.Runtime.CompilerServices;
using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

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
        var profile = await _learnerProfileRepository.GetByUserIdAsync(request.UserId);
        var notion = request.NotionId is { } notionId
            ? await _notionRepository.GetByIdAsync(notionId, cancellationToken)
            : null;

        var messages = (request.History ?? Array.Empty<TutorTurn>())
            .Select(t => new AiChatMessage(t.Role == "assistant" ? AiChatRole.Assistant : AiChatRole.User, t.Content))
            .Append(new AiChatMessage(AiChatRole.User, request.Message))
            .ToList();

        var chatRequest = new AiChatRequest(BuildSystemPrompt(profile, notion), messages);

        await foreach (var chunk in _aiChatService.StreamReplyAsync(chatRequest, cancellationToken))
            yield return chunk;
    }

    public static string BuildSystemPrompt(Domain.Entities.LearnerProfile? profile, Notion? notion = null)
    {
        var prompt =
            "Tu es MonProfAI, un professeur particulier bienveillant pour des élèves du collège et du lycée en France. " +
            "Réponds en français, avec des phrases simples adaptées à l'âge de l'élève. " +
            "Aide l'élève à comprendre et à raisonner par lui-même : explique la méthode, donne des indices " +
            "et pose des questions plutôt que de livrer directement la réponse d'un exercice. " +
            "Si la question n'a rien à voir avec les études, ramène gentiment la conversation vers le travail scolaire. " +
            "Ne demande jamais d'informations personnelles à l'élève. " +
            // The chat renders Markdown and KaTeX: only these delimiters are recognised.
            "Mets en forme tes réponses en Markdown, sans titres. Écris toute expression mathématique en LaTeX, " +
            "entre $...$ dans le texte et entre $$...$$ pour une formule sur sa propre ligne.";

        if (notion is not null)
        {
            prompt += $" La séance porte sur la notion « {notion.Title} » du chapitre « {notion.Chapter.Title} » " +
                      $"({notion.Chapter.Subject.Name}, {LevelLabel(notion.Chapter.SchoolLevel)}).";
            if (!string.IsNullOrWhiteSpace(notion.ShortDescription))
                prompt += $" Contenu de la notion : {notion.ShortDescription}";
            prompt += " Reste centré sur cette notion ; si l'élève s'en éloigne, aide-le brièvement puis ramène-le à la notion.";
        }

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
