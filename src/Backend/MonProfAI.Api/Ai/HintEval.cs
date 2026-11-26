using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Exercises;
using MonProfAI.Infrastructure.Ai;
using MonProfAI.Infrastructure.Data;

namespace MonProfAI.Api.Ai;

/// <summary>
/// <c>dotnet run --project src/Backend/MonProfAI.Api -- hint-eval</c>: on test exercises, unlocks the four hints in
/// order against the real model, sends one wrong answer, and has a model grade each output: no final answer before
/// the solution, each hint more precise than the previous one, a wrong answer not corrected for the student.
/// Calls the paid API (a few cents per run). Needs the seed and AI_API_KEY. Writes a Markdown report.
/// </summary>
public static class HintEval
{
    public const string Command = "hint-eval";

    private const string HelperModel = "claude-sonnet-5-5";

    private sealed record TestExercise(string StudentEmail, string NotionTitle, string Statement, string FinalAnswer, string WrongAnswer);

    private static readonly TestExercise[] Exercises =
    {
        new("ines.4e@monprofai.test", "Additionner et soustraire des fractions",
            "Calcule $\\frac{2}{3} + \\frac{5}{4}$ et donne le résultat sous forme de fraction irréductible.",
            "23/12", "7/7"),
        new("ines.4e@monprofai.test", "Calculer une longueur avec le théorème de Pythagore",
            "Le triangle ABC est rectangle en A, avec AB = 6 cm et AC = 8 cm. Calcule la longueur BC.",
            "BC = 10 cm", "BC = 14 cm"),
        new("hugo.3e@monprofai.test", "Résoudre une équation du premier degré",
            "Résous l'équation $5x - 7 = 2x + 8$.",
            "x = 5", "x = 1/3"),
    };

    public static async Task<int> RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<MonProfAIDbContext>();
        var mediator = services.GetRequiredService<IMediator>();
        var options = services.GetRequiredService<AiOptions>();
        var apiKey = app.Configuration[AiOptions.ApiKeyVariable]
                     ?? DotEnvFile.ReadValue(app.Environment.ContentRootPath, AiOptions.ApiKeyVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.Error.WriteLine($"{AiOptions.ApiKeyVariable} manquante : renseigne-la dans .env.");
            return 1;
        }
        var judge = new AnthropicChatService(apiKey, new AiOptions { Model = HelperModel, MaxTokens = 2000 },
            services.GetRequiredService<ILogger<AnthropicChatService>>());

        var report = new StringBuilder($"# Évaluation des indices\n\nModèle : `{options.Model}`. Correcteur : `{HelperModel}`.\n");
        int passed = 0, total = 0;
        void Record(bool ok, string label, string reason)
        {
            total++;
            if (ok) passed++;
            Console.WriteLine($"[{(ok ? "OK" : "KO")}] {label}: {reason}");
            report.Append($"\n- {(ok ? "✅" : "❌")} **{label}** : {reason}\n");
        }

        foreach (var test in Exercises)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == test.StudentEmail);
            var notion = await db.Notions.FirstOrDefaultAsync(n => n.Title == test.NotionTitle);
            if (user is null || notion is null)
            {
                Console.Error.WriteLine($"Seed manquant pour {test.StudentEmail} / {test.NotionTitle} : lance d'abord `-- seed`.");
                return 1;
            }

            Console.WriteLine($"\n=== {test.Statement} ===");
            report.Append($"\n## {test.NotionTitle}\n\n> {test.Statement}\n\nRéponse attendue : `{test.FinalAnswer}`.\n");
            var created = await mediator.Send(new CreateExerciseCommand(user.Id, test.Statement, notion.Id));

            // The rules: the solution must be refused before the three hints.
            try
            {
                await mediator.Send(new UnlockHintCommand(user.Id, created.Id, 4));
                Record(false, "Solution d'emblée", "la solution a été donnée sans les indices.");
            }
            catch (HintLockedException)
            {
                Record(true, "Solution d'emblée", "refusée avant les 3 indices.");
            }

            var attempt = await mediator.Send(new SubmitAnswerCommand(user.Id, created.Id, test.WrongAnswer));
            report.Append($"\n**Réponse fausse « {test.WrongAnswer} »** → {(attempt.IsCorrect ? "jugée juste" : "jugée fausse")}\n\n{attempt.Feedback}\n");
            Record(!attempt.IsCorrect, "Verdict réponse fausse", attempt.IsCorrect ? "jugée juste à tort." : "jugée fausse.");
            var (noLeak, leakReason) = await GradeAsync(judge,
                $"Exercice : {test.Statement}\nRéponse attendue : {test.FinalAnswer}\nRéponse fausse de l'élève : {test.WrongAnswer}\n" +
                $"Message du professeur :\n{attempt.Feedback}",
                "Le message montre où est l'erreur SANS donner le résultat final attendu ni la bonne étape à la place de l'élève.");
            Record(noLeak, "Correction sans solution", leakReason);

            var hints = new List<HintDto>();
            for (var level = 1; level <= 4; level++)
            {
                var hint = await mediator.Send(new UnlockHintCommand(user.Id, created.Id, level));
                hints.Add(hint);
                report.Append($"\n### Indice {level} · {hint.Title}\n\n{hint.Content}\n");

                if (level < 4)
                {
                    var (ok, reason) = await GradeAsync(judge,
                        $"Exercice : {test.Statement}\nRéponse attendue : {test.FinalAnswer}\nIndice de niveau {level} :\n{hint.Content}",
                        "L'indice NE donne PAS le résultat final de l'exercice (ni la valeur attendue, ni une forme équivalente).");
                    Record(ok, $"Indice {level} sans résultat", reason);
                }
                else
                {
                    var (ok, reason) = await GradeAsync(judge,
                        $"Exercice : {test.Statement}\nRéponse attendue : {test.FinalAnswer}\nSolution :\n{hint.Content}",
                        "La solution est complète, expliquée étape par étape, et arrive au bon résultat final.");
                    Record(ok, "Solution complète et juste", reason);
                }

                if (level > 1)
                {
                    var (ok, reason) = await GradeAsync(judge,
                        $"Exercice : {test.Statement}\nIndice {level - 1} :\n{hints[^2].Content}\n\nIndice {level} :\n{hint.Content}",
                        $"L'indice {level} est plus précis et plus avancé vers la solution que l'indice {level - 1}, " +
                        "et apporte quelque chose de nouveau au lieu de le répéter.");
                    Record(ok, $"Indice {level} plus précis que {level - 1}", reason);
                }
            }
        }

        report.Insert(0, $"<!-- {passed}/{total} -->\n");
        var path = Path.Combine(Path.GetTempPath(), $"hint-eval-{DateTime.Now:yyyyMMdd-HHmmss}.md");
        await File.WriteAllTextAsync(path, report.ToString());
        Console.WriteLine($"\nRésultat : {passed}/{total} vérifications réussies. Rapport : {path}");
        return passed == total ? 0 : 2;
    }

    private static async Task<(bool Pass, string Reason)> GradeAsync(IAiChatService judge, string material, string criterion)
    {
        var prompt = $"{material}\n\nCritère : {criterion}\n\n" +
                     "Réponds uniquement avec un objet JSON : {\"pass\": true|false, \"reason\": \"une phrase en français\"}.";
        var sb = new StringBuilder();
        await foreach (var chunk in judge.StreamReplyAsync(new AiChatRequest(
                           "Tu évalues des indices pédagogiques de façon stricte et factuelle.",
                           new[] { new AiChatMessage(AiChatRole.User, prompt) })))
            sb.Append(chunk);

        try
        {
            using var doc = JsonDocument.Parse(Regex.Match(sb.ToString(), @"\{[\s\S]*\}").Value);
            return (doc.RootElement.GetProperty("pass").GetBoolean(), doc.RootElement.GetProperty("reason").GetString() ?? "");
        }
        catch (Exception)
        {
            return (false, $"Correcteur illisible : {sb}");
        }
    }
}
