using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MonProfAI.Application.Interfaces;
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
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AI_API_KEY", SecretApiKey);
            builder.UseSetting("Ai:RateLimit:PermitLimit", permitLimit.ToString());
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAiChatService>(fake);
                services.AddScoped(_ => profiles.Object);
            });
        });
        return (factory, fake);
    }

    private static async Task<HttpClient> AuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var (token, _) = await new AuthService(JwtSecret).GenerateTokensAsync(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
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
        Assert.Equal("data: {\"text\":\"Bonjour\"}\n\ndata: {\"text\":\" !\"}\n\nevent: done\ndata: {}\n\n", body);
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

        Assert.StartsWith("data: {\"text\":\"Le théorème\"}\n\nevent: error\n", body);
        Assert.Contains("n'est pas disponible", body);
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

    private sealed record ErrorBody(string Code, string Message);
}
