using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;
using MonProfAI.Infrastructure.Services;
using Xunit;

namespace MonProfAI.Tests.Exercises;

/// <summary>Hints and answers on the real API, an in-memory database and a scripted model (no real call).</summary>
public class ExerciseHintEndpointTests : IDisposable
{
    private const string JwtSecret = "your-super-secret-key-change-this-in-production-at-least-32-characters-long!";

    private readonly Guid _notionId = Guid.NewGuid();
    private readonly ScriptedAi _ai = new();
    private readonly WebApplicationFactory<Program> _factory;

    private sealed class ScriptedAi : IAiChatService
    {
        /// <summary>Answers to an answer check. Hint requests get "Indice n généré".</summary>
        public string Verdict { get; set; } = "FAUX\nRegarde ton addition.";
        public List<AiChatRequest> Requests { get; } = new();

        public async IAsyncEnumerable<string> StreamReplyAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            await Task.Yield();
            var message = request.Messages[^1].Content;
            yield return message.StartsWith("Donne-moi l'indice de niveau ")
                ? $"Indice {message["Donne-moi l'indice de niveau ".Length]} généré"
                : Verdict;
        }
    }

    public ExerciseHintEndpointTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Ai:RateLimit:PermitLimit", "100");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAiChatService>(_ai);
                services.RemoveAll<DbContextOptions<MonProfAIDbContext>>();
                services.AddDbContext<MonProfAIDbContext>(o => o.UseInMemoryDatabase(databaseName));
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonProfAIDbContext>();
        var chapter = new Chapter
        {
            Subject = new Subject { Code = "maths", Name = "Mathématiques" },
            Title = "Théorème de Pythagore",
            SchoolLevel = SchoolLevel.Quatrieme,
            Order = 7,
        };
        chapter.Notions.Add(new Notion { Id = _notionId, Title = "Calculer une longueur avec Pythagore", Order = 1 });
        db.Add(chapter);
        db.SaveChanges();
    }

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> Student()
    {
        var client = _factory.CreateClient();
        var (token, _) = await new AuthService(JwtSecret).GenerateTokensAsync(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> NewExercise(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises",
            new { statement = "ABC est rectangle en A, AB = 3 cm et AC = 4 cm. Calcule BC.", notionId = _notionId });
        return (await response.Content.ReadFromJsonAsync<IdBody>())!.Id;
    }

    private static Task<HttpResponseMessage> Hint(HttpClient client, Guid exerciseId, int level) =>
        client.PostAsJsonAsync($"/api/exercises/{exerciseId}/hints", new { level });

    private static Task<HttpResponseMessage> Answer(HttpClient client, Guid exerciseId, string answer) =>
        client.PostAsJsonAsync($"/api/exercises/{exerciseId}/attempts", new { answer });

    private sealed record IdBody(Guid Id);

    private sealed record HintBody(int Level, string Title, string Content);

    private sealed record AttemptBody(string Answer, bool IsCorrect, string Feedback);

    private sealed record ExerciseBody(
        string Status, List<HintBody> Hints, List<AttemptBody> Attempts, int? NextHintLevel, string? SolutionLockedReason);

    private sealed record ErrorBody(string Code, string Message);

    [Fact]
    public async Task Hints_ComeInOrder_AndStayDisplayed()
    {
        var client = await Student();
        var id = await NewExercise(client);

        Assert.Equal(HttpStatusCode.Conflict, (await Hint(client, id, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Hint(client, id, 4)).StatusCode);
        Assert.Empty(_ai.Requests);

        for (var level = 1; level <= 4; level++)
        {
            var response = await Hint(client, id, level);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"Indice {level} généré", (await response.Content.ReadFromJsonAsync<HintBody>())!.Content);
        }

        var exercise = await client.GetFromJsonAsync<ExerciseBody>($"/api/exercises/{id}");
        Assert.Equal(new[] { 1, 2, 3, 4 }, exercise!.Hints.Select(h => h.Level));
        Assert.Equal(new[] { "Rappel de la notion", "Méthode", "Première étape", "Solution complète" },
            exercise.Hints.Select(h => h.Title));
        Assert.Null(exercise.NextHintLevel);
        Assert.Equal("InProgress", exercise.Status);
    }

    [Fact]
    public async Task EachHint_IsGeneratedWithItsLevelAndThePreviousHints()
    {
        var client = await Student();
        var id = await NewExercise(client);
        await Hint(client, id, 1);

        await Hint(client, id, 2);

        var prompt = _ai.Requests[^1].SystemPrompt;
        Assert.Contains("**Niveau 2 : la méthode.**", prompt);
        Assert.Contains("Indice 1 (Rappel de la notion) déjà donné :\n<indice>\nIndice 1 généré\n</indice>", prompt);
    }

    [Fact]
    public async Task AnUnlockedHint_IsNotGeneratedTwice()
    {
        var client = await Student();
        var id = await NewExercise(client);
        await Hint(client, id, 1);

        var again = await Hint(client, id, 1);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Single(_ai.Requests);
    }

    [Fact]
    public async Task Solution_IsLockedUntilThreeWrongAnswers()
    {
        var client = await Student();
        var id = await NewExercise(client);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var locked = await Hint(client, id, 4);
            var reason = (await locked.Content.ReadFromJsonAsync<ErrorBody>())!;
            Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
            Assert.Equal("HintLocked", reason.Code);
            await Answer(client, id, $"BC = {attempt + 5}");
        }

        Assert.Equal(HttpStatusCode.OK, (await Hint(client, id, 4)).StatusCode);
    }

    [Fact]
    public async Task Answer_WrongThenRight_RecordsVerdictsAndSolvesTheExercise()
    {
        var client = await Student();
        var id = await NewExercise(client);

        var wrong = await (await Answer(client, id, "BC = 7")).Content.ReadFromJsonAsync<AttemptBody>();
        _ai.Verdict = "JUSTE\nBravo, $BC = 5$ cm.";
        var right = await (await Answer(client, id, "BC = 5 cm")).Content.ReadFromJsonAsync<AttemptBody>();

        Assert.False(wrong!.IsCorrect);
        Assert.Equal("Regarde ton addition.", wrong.Feedback);
        Assert.True(right!.IsCorrect);
        var exercise = await client.GetFromJsonAsync<ExerciseBody>($"/api/exercises/{id}");
        Assert.Equal("Solved", exercise!.Status);
        Assert.Equal(new[] { "BC = 7", "BC = 5 cm" }, exercise.Attempts.Select(a => a.Answer));
        // Once solved, the full correction can be read straight away.
        Assert.Null(exercise.SolutionLockedReason);
        Assert.Contains("<reponse>\nBC = 5 cm\n</reponse>", _ai.Requests[^1].Messages[^1].Content);
    }

    [Fact]
    public async Task Answer_WhenTheModelGivesNoVerdict_Returns502AndRecordsNothing()
    {
        var client = await Student();
        var id = await NewExercise(client);
        _ai.Verdict = "Hmm, c'est presque ça.";

        var response = await Answer(client, id, "BC = 5");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var exercise = await client.GetFromJsonAsync<ExerciseBody>($"/api/exercises/{id}");
        Assert.Empty(exercise!.Attempts);
    }

    [Fact]
    public async Task Answer_Empty_Returns400()
    {
        var client = await Student();
        var id = await NewExercise(client);

        Assert.Equal(HttpStatusCode.BadRequest, (await Answer(client, id, "  ")).StatusCode);
    }

    [Fact]
    public async Task AnotherStudent_CannotUnlockHintsOrAnswer()
    {
        var id = await NewExercise(await Student());
        var intruder = await Student();

        Assert.Equal(HttpStatusCode.NotFound, (await Hint(intruder, id, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Answer(intruder, id, "BC = 5")).StatusCode);
        Assert.Empty(_ai.Requests);
    }
}
