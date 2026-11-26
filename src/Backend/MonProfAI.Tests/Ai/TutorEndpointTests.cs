using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;
using MonProfAI.Infrastructure.Services;
using Xunit;

namespace MonProfAI.Tests.Ai;

/// <summary>
/// Runs the real API in memory with the AI provider replaced by a fake, so the route, auth,
/// streaming format, error mapping and rate limit are tested without any real model call.
/// </summary>
public class TutorEndpointTests
{
    // Must match appsettings.json, which the in-memory API loads.
    private const string JwtSecret = "your-super-secret-key-change-this-in-production-at-least-32-characters-long!";
    private const string SecretApiKey = "sk-ant-secret-that-must-never-leak";

    private sealed class FakeChatService : IAiChatService
    {
        public Func<AiChatRequest, IAsyncEnumerable<string>> Reply { get; set; } = _ => Chunks("Bonjour", " !");

        public AiChatRequest? LastRequest { get; private set; }

        public IAsyncEnumerable<string> StreamReplyAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Reply(request);
        }
    }

    private static async IAsyncEnumerable<string> Chunks(params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    private static async IAsyncEnumerable<string> FailAfter(
        AiErrorKind kind, string[] chunksFirst, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var chunk in chunksFirst)
            yield return chunk;
        await Task.Yield();
        throw new AiServiceException(kind);
    }

    private static (WebApplicationFactory<Program> Factory, FakeChatService Fake) CreateApi(int permitLimit = 30)
    {
        var fake = new FakeChatService();
        var profiles = new Mock<ILearnerProfileRepository>();
        var notions = new Mock<INotionRepository>();
        notions.Setup(r => r.GetByIdAsync(KnownNotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(KnownNotion);
        notions.Setup(r => r.GetMasteriesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, NotionMastery>());
        var databaseName = Guid.NewGuid().ToString();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AI_API_KEY", SecretApiKey);
            builder.UseSetting("Ai:RateLimit:PermitLimit", permitLimit.ToString());
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAiChatService>(fake);
                services.AddScoped(_ => profiles.Object);
                services.AddScoped(_ => notions.Object);
                // Conversations are saved for real, in an in-memory database private to this test.
                services.RemoveAll<DbContextOptions<MonProfAIDbContext>>();
                services.AddDbContext<MonProfAIDbContext>(o => o.UseInMemoryDatabase(databaseName));
            });
        });
        return (factory, fake);
    }

    private static async Task<HttpClient> AuthenticatedClient(WebApplicationFactory<Program> factory, Guid? userId = null)
    {
        var client = factory.CreateClient();
        var (token, _) = await new AuthService(JwtSecret).GenerateTokensAsync(userId ?? Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Guid ConversationIdFrom(string sseBody)
    {
        var match = Regex.Match(sseBody, "event: conversation\ndata: \\{\"conversationId\":\"([0-9a-f-]+)\"\\}");
        Assert.True(match.Success, "No conversation event in: " + sseBody);
        return Guid.Parse(match.Groups[1].Value);
    }

    private static Task<HttpResponseMessage> Ask(HttpClient client, string message = "C'est quoi Pythagore ?") =>
        client.PostAsJsonAsync("/api/tutor/ask", new { message });

    [Fact]
    public async Task Ask_WithoutLogin_Returns401()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;

        var response = await Ask(factory.CreateClient());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(fake.LastRequest);
    }

    [Fact]
    public async Task Ask_StreamsTheModelAnswerAsServerSentEvents()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        var client = await AuthenticatedClient(factory);

        var response = await Ask(client);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var conversationId = ConversationIdFrom(body);
        Assert.Equal(
            $"event: conversation\ndata: {{\"conversationId\":\"{conversationId}\"}}\n\n" +
            "data: {\"text\":\"Bonjour\"}\n\ndata: {\"text\":\" !\"}\n\nevent: done\ndata: {}\n\n",
            body);
        Assert.Equal("C'est quoi Pythagore ?", fake.LastRequest!.Messages[^1].Content);
        Assert.DoesNotContain(SecretApiKey, body);
    }

    [Theory]
    [InlineData(AiErrorKind.Timeout, HttpStatusCode.GatewayTimeout)]
    [InlineData(AiErrorKind.QuotaExceeded, HttpStatusCode.ServiceUnavailable)]
    [InlineData(AiErrorKind.NotConfigured, HttpStatusCode.ServiceUnavailable)]
    [InlineData(AiErrorKind.InvalidResponse, HttpStatusCode.BadGateway)]
    public async Task Ask_WhenProviderFailsBeforeAnswering_ReturnsStatusAndStudentMessage(
        AiErrorKind kind, HttpStatusCode expectedStatus)
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        fake.Reply = _ => FailAfter(kind, Array.Empty<string>());
        var client = await AuthenticatedClient(factory);

        var response = await Ask(client);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(kind.ToString(), error!.Code);
        Assert.Equal(new AiServiceException(kind).StudentMessage, error.Message);
        Assert.DoesNotContain(SecretApiKey, error.Message);
    }

    [Fact]
    public async Task Ask_WhenProviderFailsMidAnswer_SendsAnErrorEvent()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        fake.Reply = _ => FailAfter(AiErrorKind.Unavailable, new[] { "Le théorème" });
        var client = await AuthenticatedClient(factory);

        var body = await (await Ask(client)).Content.ReadAsStringAsync();

        Assert.Contains("data: {\"text\":\"Le théorème\"}\n\nevent: error\n", body);
        Assert.Contains("n'est pas disponible", body);

        // What the student saw of the answer is kept.
        var saved = await (await client.GetAsync($"/api/conversations/{ConversationIdFrom(body)}"))
            .Content.ReadFromJsonAsync<ConversationBody>();
        Assert.Equal(new[] { "user", "assistant" }, saved!.Messages.Select(m => m.Role));
        Assert.Equal("Le théorème", saved.Messages[1].Content);
    }

    [Fact]
    public async Task Ask_StartsAConversation_ThatTheStudentFindsInHisHistory()
    {
        var (factory, _) = CreateApi();
        using var __ = factory;
        var client = await AuthenticatedClient(factory);

        var body = await (await client.PostAsJsonAsync("/api/tutor/ask",
            new { message = "Je bloque sur Pythagore", notionId = KnownNotion.Id })).Content.ReadAsStringAsync();
        var conversationId = ConversationIdFrom(body);

        var list = await client.GetFromJsonAsync<List<SummaryBody>>("/api/conversations");
        var summary = Assert.Single(list!);
        Assert.Equal(conversationId, summary.Id);
        Assert.Equal(KnownNotion.Id, summary.NotionId);
        Assert.Equal("Je bloque sur Pythagore", summary.Preview);
        Assert.Equal(2, summary.MessageCount);

        var conversation = await client.GetFromJsonAsync<ConversationBody>($"/api/conversations/{conversationId}");
        Assert.Equal(new[] { "Je bloque sur Pythagore", "Bonjour !" }, conversation!.Messages.Select(m => m.Content));
    }

    [Fact]
    public async Task Ask_InAnExistingConversation_ReplaysItsMessagesToTheModel()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        var client = await AuthenticatedClient(factory);
        var first = await (await client.PostAsJsonAsync("/api/tutor/ask",
            new { message = "Explique-moi", notionId = KnownNotion.Id })).Content.ReadAsStringAsync();
        var conversationId = ConversationIdFrom(first);
        fake.Reply = _ => Chunks("Exact !");

        var second = await (await client.PostAsJsonAsync("/api/tutor/ask",
            new { message = "Ça fait 5 cm ?", conversationId })).Content.ReadAsStringAsync();

        Assert.Equal(conversationId, ConversationIdFrom(second));
        Assert.Equal(
            new[] { "User:Explique-moi", "Assistant:Bonjour !", "User:Ça fait 5 cm ?" },
            fake.LastRequest!.Messages.Select(m => $"{m.Role}:{m.Content}"));
        // The notion chosen at the start still frames the conversation.
        Assert.Contains("« Calculer une longueur avec le théorème de Pythagore »", fake.LastRequest.SystemPrompt);
    }

    [Fact]
    public async Task AnotherStudentsConversation_IsInvisibleAndCannotBeContinued()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        var owner = await AuthenticatedClient(factory);
        var body = await (await Ask(owner, "Ma question privée")).Content.ReadAsStringAsync();
        var conversationId = ConversationIdFrom(body);
        var intruder = await AuthenticatedClient(factory);
        fake.Reply = _ => Chunks("ne doit pas arriver");

        var read = await intruder.GetAsync($"/api/conversations/{conversationId}");
        var list = await intruder.GetFromJsonAsync<List<SummaryBody>>("/api/conversations");
        var write = await intruder.PostAsJsonAsync("/api/tutor/ask", new { message = "Coucou", conversationId });

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Empty(list!);
        Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
        var ownerView = await owner.GetFromJsonAsync<ConversationBody>($"/api/conversations/{conversationId}");
        Assert.Equal(2, ownerView!.Messages.Count);
    }

    [Fact]
    public async Task Ask_WithEmptyQuestion_Returns400WithoutCallingTheModel()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        var client = await AuthenticatedClient(factory);

        var response = await Ask(client, "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(fake.LastRequest);
    }

    [Fact]
    public async Task Ask_BeyondThePerUserLimit_Returns429()
    {
        var (factory, _) = CreateApi(permitLimit: 2);
        using var __ = factory;
        var client = await AuthenticatedClient(factory);

        Assert.Equal(HttpStatusCode.OK, (await Ask(client)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Ask(client)).StatusCode);
        var third = await Ask(client);
        var error = await third.Content.ReadFromJsonAsync<ErrorBody>();

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal("RateLimited", error!.Code);

        // Another student is not affected.
        var otherStudent = await AuthenticatedClient(factory);
        Assert.Equal(HttpStatusCode.OK, (await Ask(otherStudent)).StatusCode);
    }

    private static readonly Notion KnownNotion = new()
    {
        Id = Guid.NewGuid(),
        Title = "Calculer une longueur avec le théorème de Pythagore",
        Order = 1,
        Chapter = new Chapter
        {
            Title = "Théorème de Pythagore",
            SchoolLevel = SchoolLevel.Quatrieme,
            Order = 7,
            Subject = new Subject { Code = "maths", Name = "Mathématiques" },
        },
    };

    [Fact]
    public async Task Ask_WithANotion_FocusesTheTutorOnIt()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        var client = await AuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/tutor/ask",
            new { message = "Je ne comprends pas", notionId = KnownNotion.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("« Calculer une longueur avec le théorème de Pythagore »", fake.LastRequest!.SystemPrompt);
        Assert.Contains("4ème", fake.LastRequest.SystemPrompt);
    }

    [Fact]
    public async Task Ask_WithAnUnknownNotion_Returns400()
    {
        var (factory, fake) = CreateApi();
        using var _ = factory;
        var client = await AuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/tutor/ask",
            new { message = "Bonjour", notionId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(fake.LastRequest);
    }

    private sealed record ErrorBody(string Code, string Message);

    private sealed record SummaryBody(Guid Id, Guid? NotionId, string Preview, int MessageCount);

    private sealed record MessageBody(string Role, string Content);

    private sealed record ConversationBody(Guid Id, List<MessageBody> Messages);
}
