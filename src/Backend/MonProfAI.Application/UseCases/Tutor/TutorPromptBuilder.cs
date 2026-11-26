using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Tutor;

/// <summary>Everything the tutor knows about the session, gathered by the handler.</summary>
/// <param name="NotionMastery">The student's mastery of <paramref name="Notion"/>, if he has worked on it.</param>
/// <param name="PrerequisiteMasteries">Mastery of each prerequisite of the notion, by notion id (missing = never worked on).</param>
public sealed record TutorPromptContext(
    Domain.Entities.LearnerProfile? Profile,
    Notion? Notion,
    NotionMastery? NotionMastery = null,
    IReadOnlyDictionary<Guid, NotionMastery>? PrerequisiteMasteries = null);

/// <summary>
/// Builds the tutor's system prompt: the pedagogical instructions from <c>TutorSystemPrompt.md</c>
/// (an embedded resource, editable without touching code) plus the student's and the notion's context.
/// </summary>
public static class TutorPromptBuilder
{
    public const string StudentPlaceholder = "{{ELEVE}}";
    public const string NotionPlaceholder = "{{NOTION}}";

    private static readonly Lazy<string> Template = new(LoadTemplate);

    public static string Build(TutorPromptContext context) =>
        Template.Value
            .Replace(StudentPlaceholder, DescribeStudent(context.Profile))
            .Replace(NotionPlaceholder, DescribeNotion(context));

    /// <summary>The template with its HTML comment (notes for editors) removed.</summary>
    public static string LoadTemplate()
    {
        var assembly = typeof(TutorPromptBuilder).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith("TutorSystemPrompt.md"));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var template = Regex.Replace(reader.ReadToEnd(), @"<!--[\s\S]*?-->", string.Empty).Trim();

        foreach (var placeholder in new[] { StudentPlaceholder, NotionPlaceholder })
        {
            var count = Regex.Matches(template, Regex.Escape(placeholder)).Count;
            if (count != 1)
                throw new InvalidOperationException(
                    $"TutorSystemPrompt.md must contain {placeholder} exactly once (found {count}).");
        }

        return template;
    }

    private static string DescribeStudent(Domain.Entities.LearnerProfile? profile)
    {
        if (profile is not { OnboardingCompleted: true })
            return "L'élève n'a pas encore rempli son profil : tu ne connais ni sa classe ni ses préférences. " +
                   "Pars du niveau de la notion et ajuste-toi d'après ses réponses.";

        var lines = new List<string> { $"- Classe : {LevelLabel(profile.SchoolLevel)}." };

        if (profile.PreferredLearningStyle is { } style)
            lines.Add($"- Façon d'apprendre préférée : {StyleInstruction(style)}");

        if (profile.Goal is { } goal)
            lines.Add($"- Objectif : {GoalLabel(goal)}.");

        if (profile.StrugglingSubjects.Count > 0)
            lines.Add($"- Matières où il se sent en difficulté : {string.Join(", ", profile.StrugglingSubjects)}. " +
                      "Sois particulièrement rassurant dans ces matières.");

        return string.Join("\n", lines);
    }

    private static string DescribeNotion(TutorPromptContext context)
    {
        if (context.Notion is not { } notion)
            return "Aucune notion n'a été choisie : aide l'élève sur la question scolaire qu'il pose.";

        var chapter = notion.Chapter;
        var lines = new List<string>
        {
            $"- Notion : « {notion.Title} »",
            $"- Chapitre : « {chapter.Title} » ({chapter.Subject.Name}, programme de {LevelLabel(chapter.SchoolLevel)})",
        };
        if (!string.IsNullOrWhiteSpace(notion.ShortDescription))
            lines.Add($"- Contenu : {notion.ShortDescription}");

        lines.Add($"- Maîtrise de l'élève : {MasteryLabel(context.NotionMastery)}");

        var prerequisites = notion.Prerequisites
            .Select(p => p.PrerequisiteNotion)
            .Where(p => p is not null)
            .ToList();
        if (prerequisites.Count > 0)
        {
            lines.Add("- Prérequis de cette notion :");
            foreach (var prerequisite in prerequisites)
            {
                var prerequisiteMastery = context.PrerequisiteMasteries?.GetValueOrDefault(prerequisite.Id);
                lines.Add($"  - « {prerequisite.Title} » : {MasteryLabel(prerequisiteMastery)}");
            }
            lines.Add("  Si l'élève bloque à cause d'un prérequis fragile, revois-le brièvement avant de continuer.");
        }

        lines.Add("Reste centré sur cette notion. Si l'élève s'en éloigne, aide-le brièvement puis ramène-le à la notion.");
        return string.Join("\n", lines);
    }

    /// <summary>Turns a mastery score (0 to 1) into guidance the model can act on.</summary>
    public static string MasteryLabel(NotionMastery? mastery)
    {
        if (mastery is null || mastery.AttemptCount == 0)
            return "jamais travaillée sur MonProfAI. Pars des bases et vérifie ce qu'il sait déjà.";

        var percent = (int)Math.Round(mastery.MasteryScore * 100);
        var attempts = mastery.AttemptCount.ToString(CultureInfo.InvariantCulture);
        return mastery.MasteryScore switch
        {
            < 0.4 => $"fragile ({percent} % après {attempts} essais). Reprends les bases, lentement, avec des exemples simples.",
            < 0.75 => $"en cours d'acquisition ({percent} % après {attempts} essais). Consolide avec des exemples variés.",
            _ => $"bien maîtrisée ({percent} % après {attempts} essais). Va à l'essentiel et propose des questions plus exigeantes.",
        };
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

    private static string StyleInstruction(LearningStyle style) => style switch
    {
        LearningStyle.ConcreteExamples => "les exemples concrets. Pars d'une situation de la vie courante avant la règle.",
        LearningStyle.Diagrams => "les schémas. Décris des représentations visuelles (droite graduée, figure, tableau simple).",
        LearningStyle.ShortExplanations => "les explications courtes. Va droit au but, une idée par message.",
        _ => "les explications détaillées. Justifie chaque étape, il aime comprendre le pourquoi.",
    };

    private static string GoalLabel(LearningGoal goal) => goal switch
    {
        LearningGoal.Brevet => "réussir le brevet",
        LearningGoal.Bac => "réussir le bac",
        LearningGoal.ImproveGrades => "remonter ses notes",
        _ => "progresser",
    };
}
