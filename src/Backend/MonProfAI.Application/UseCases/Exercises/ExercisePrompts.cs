using System.Text;
using System.Text.RegularExpressions;
using MonProfAI.Application.UseCases.Tutor;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.Exercises;

/// <summary>What the hint and answer-check prompts know about the exercise.</summary>
public sealed record ExercisePromptContext(
    Exercise Exercise,
    Domain.Entities.LearnerProfile? Profile);

/// <summary>
/// Builds the prompts of <c>Prompts/ExerciseHintPrompt.md</c> and <c>Prompts/AnswerCheckPrompt.md</c>
/// (embedded resources, editable without touching code).
/// </summary>
public static class ExercisePrompts
{
    public const string StudentTag = "{{ELEVE}}";
    public const string NotionTag = "{{NOTION}}";
    public const string StatementTag = "{{ENONCE}}";
    public const string HistoryTag = "{{DEJA_DONNE}}";
    public const string LevelInstructionTag = "{{CONSIGNE_NIVEAU}}";

    public static readonly IReadOnlyDictionary<int, string> LevelTitles = new Dictionary<int, string>
    {
        [HintLevels.NotionReminder] = "Rappel de la notion",
        [HintLevels.Method] = "Méthode",
        [HintLevels.FirstStep] = "Première étape",
        [HintLevels.Solution] = "Solution complète",
    };

    private static readonly Lazy<(string Body, IReadOnlyDictionary<int, string> Levels)> HintTemplate = new(LoadHintTemplate);
    private static readonly Lazy<string> CheckTemplate = new(LoadCheckTemplate);

    public static string BuildHintPrompt(ExercisePromptContext context, int level) =>
        Fill(HintTemplate.Value.Body, context)
            .Replace(LevelInstructionTag, HintTemplate.Value.Levels[level]);

    public static string BuildAnswerCheckPrompt(ExercisePromptContext context) => Fill(CheckTemplate.Value, context);

    /// <summary>The hint template, split into its common body and its [[NIVEAU n]] sections.</summary>
    public static (string Body, IReadOnlyDictionary<int, string> Levels) LoadHintTemplate()
    {
        var text = Load("ExerciseHintPrompt.md");
        var parts = Regex.Split(text, @"^\[\[NIVEAU (\d)\]\]\s*$", RegexOptions.Multiline);
        var body = parts[0].Trim();
        var levels = new Dictionary<int, string>();
        for (var i = 1; i + 1 < parts.Length; i += 2)
            levels[int.Parse(parts[i])] = parts[i + 1].Trim();

        for (var level = 1; level <= HintLevels.Solution; level++)
        {
            if (!levels.ContainsKey(level))
                throw new InvalidOperationException($"ExerciseHintPrompt.md has no [[NIVEAU {level}]] section.");
        }
        CheckTags("ExerciseHintPrompt.md", body, StudentTag, NotionTag, StatementTag, HistoryTag, LevelInstructionTag);
        return (body, levels);
    }

    public static string LoadCheckTemplate()
    {
        var text = Load("AnswerCheckPrompt.md");
        CheckTags("AnswerCheckPrompt.md", text, StudentTag, NotionTag, StatementTag, HistoryTag);
        return text;
    }

    private static string Fill(string template, ExercisePromptContext context)
    {
        var exercise = context.Exercise;
        var chapter = exercise.Notion.Chapter;
        var notion =
            $"« {exercise.Notion.Title} », chapitre « {chapter.Title} » " +
            $"({chapter.Subject.Name}, programme de {TutorPromptBuilder.LevelLabel(chapter.SchoolLevel)})." +
            (string.IsNullOrWhiteSpace(exercise.Notion.ShortDescription) ? "" : $" {exercise.Notion.ShortDescription}");

        return template
            .Replace(StudentTag, TutorPromptBuilder.DescribeStudent(context.Profile))
            .Replace(NotionTag, notion)
            .Replace(StatementTag, exercise.Statement)
            .Replace(HistoryTag, DescribeHistory(exercise));
    }

    /// <summary>Hints already given and answers already tried, oldest first, so that nothing is repeated.</summary>
    public static string DescribeHistory(Exercise exercise)
    {
        var hints = exercise.Hints.OrderBy(h => h.Level).ToList();
        var attempts = exercise.Attempts.OrderBy(a => a.CreatedAt).ToList();
        if (hints.Count == 0 && attempts.Count == 0)
            return "Rien pour l'instant : c'est sa première demande.";

        var sb = new StringBuilder();
        foreach (var hint in hints)
            sb.Append($"Indice {hint.Level} ({LevelTitles[hint.Level]}) déjà donné :\n<indice>\n{hint.Content}\n</indice>\n\n");
        foreach (var attempt in attempts)
            sb.Append($"Réponse proposée par l'élève ({(attempt.IsCorrect ? "jugée juste" : "jugée fausse")}) :\n" +
                      $"<reponse>\n{attempt.Answer}\n</reponse>\n\n");
        return sb.ToString().TrimEnd();
    }

    private static string Load(string fileName)
    {
        var assembly = typeof(ExercisePrompts).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Regex.Replace(reader.ReadToEnd(), @"<!--[\s\S]*?-->", string.Empty).Trim();
    }

    private static void CheckTags(string fileName, string text, params string[] tags)
    {
        foreach (var tag in tags)
        {
            var count = Regex.Matches(text, Regex.Escape(tag)).Count;
            if (count != 1)
                throw new InvalidOperationException($"{fileName} must contain {tag} exactly once (found {count}).");
        }
    }
}
