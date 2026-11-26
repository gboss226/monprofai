using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MonProfAI.Application.Interfaces;
using MonProfAI.Infrastructure.Ai;
using Xunit;

namespace MonProfAI.Tests.Ai;

/// <summary>Every test answers through <see cref="FakeAnthropicHandler"/>: no real API call is ever made.</summary>
public class AnthropicChatServiceTests
{
    private const string FakeKey = "sk-ant-api03-test-not-a-real-key";

    private static readonly AiChatRequest Question = new(
        "Tu es un professeur.",
        new[] { new AiChatMessage(AiChatRole.User, "C'est quoi une fraction ?") });

    private static AnthropicChatService NewService(FakeAnthropicHandler handler, int timeoutSeconds = 30) =>
        new(FakeKey, new AiOptions { TimeoutSeconds = timeoutSeconds }, NullLogger<AnthropicChatService>.Instance,
            new HttpClient(handler));

    private static async Task<List<string>> CollectAsync(IAiChatService service)
    {
        var chunks = new List<string>();
        await foreach (var chunk in service.StreamReplyAsync(Question))
            chunks.Add(chunk);
        return chunks;
    }

    [Fact]
    public async Task StreamReplyAsync_StreamsTextChunksInOrder()
    {
        var handler = FakeAnthropicHandler.Streaming("Une fraction ", "est un ", "partage.");

        var chunks = await CollectAsync(NewService(handler));

        Assert.Equal(new[] { "Une fraction ", "est un ", "partage." }, chunks);
        var request = Assert.Single(handler.Requests);
        // The key from AI_API_KEY is what authenticates the call (the SDK picks the header).
        var sentCredentials = request.Headers
            .Where(h => h.Key is "Authorization" or "x-api-key")
            .SelectMany(h => h.Value);
        Assert.Contains(sentCredentials, v => v.Contains(FakeKey));
    }

    [Fact]
    public async Task StreamReplyAsync_SendsImagesBeforeTheText()
    {
        var handler = FakeAnthropicHandler.Streaming("Calcule $x$.");
        var photo = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        var request = new AiChatRequest("Recopie.", new[]
        {
            new AiChatMessage(AiChatRole.User, "Recopie l'énoncé.", new[] { new AiImage("image/png", photo) }),
        });

        await foreach (var _ in NewService(handler).StreamReplyAsync(request)) { }

        using var body = System.Text.Json.JsonDocument.Parse(handler.Bodies.Single());
        var content = body.RootElement.GetProperty("messages")[0].GetProperty("content");
        Assert.Equal("image", content[0].GetProperty("type").GetString());
        Assert.Equal("base64", content[0].GetProperty("source").GetProperty("type").GetString());
        Assert.Equal("image/png", content[0].GetProperty("source").GetProperty("media_type").GetString());
        Assert.Equal(Convert.ToBase64String(photo), content[0].GetProperty("source").GetProperty("data").GetString());
        Assert.Equal("text", content[1].GetProperty("type").GetString());
        Assert.Equal("Recopie l'énoncé.", content[1].GetProperty("text").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "rate_limit_error", AiErrorKind.QuotaExceeded)]
    [InlineData(HttpStatusCode.PaymentRequired, "billing_error", AiErrorKind.QuotaExceeded)]
    [InlineData(HttpStatusCode.Unauthorized, "authentication_error", AiErrorKind.NotConfigured)]
    [InlineData(HttpStatusCode.InternalServerError, "api_error", AiErrorKind.Unavailable)]
    [InlineData((HttpStatusCode)529, "overloaded_error", AiErrorKind.Unavailable)]
    [InlineData(HttpStatusCode.BadRequest, "invalid_request_error", AiErrorKind.InvalidResponse)]
    public async Task StreamReplyAsync_MapsProviderErrors(HttpStatusCode status, string errorType, AiErrorKind expected)
    {
        var handler = FakeAnthropicHandler.Returning(status, FakeAnthropicHandler.Error(errorType));

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => CollectAsync(NewService(handler)));

        Assert.Equal(expected, ex.Kind);
        Assert.DoesNotContain(FakeKey, ex.StudentMessage);
    }

    [Fact]
    public async Task StreamReplyAsync_WhenProviderIsTooSlow_ThrowsTimeout()
    {
        var handler = new FakeAnthropicHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        });

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => CollectAsync(NewService(handler, timeoutSeconds: 1)));

        Assert.Equal(AiErrorKind.Timeout, ex.Kind);
    }

    [Fact]
    public async Task StreamReplyAsync_WhenStreamIsGarbage_ThrowsInvalidResponse()
    {
        var handler = FakeAnthropicHandler.Returning(HttpStatusCode.OK,
            "event: content_block_delta\ndata: {not json\n\n", "text/event-stream");

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => CollectAsync(NewService(handler)));

        Assert.Equal(AiErrorKind.InvalidResponse, ex.Kind);
    }

    [Fact]
    public async Task StreamReplyAsync_WhenAnswerIsEmpty_ThrowsInvalidResponse()
    {
        var handler = FakeAnthropicHandler.Returning(HttpStatusCode.OK,
            FakeAnthropicHandler.Sse(Array.Empty<string>(), "end_turn"), "text/event-stream");

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => CollectAsync(NewService(handler)));

        Assert.Equal(AiErrorKind.InvalidResponse, ex.Kind);
    }

    [Fact]
    public async Task StreamReplyAsync_WhenModelRefuses_ThrowsRefused()
    {
        var handler = FakeAnthropicHandler.Returning(HttpStatusCode.OK,
            FakeAnthropicHandler.Sse(Array.Empty<string>(), "refusal"), "text/event-stream");

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => CollectAsync(NewService(handler)));

        Assert.Equal(AiErrorKind.Refused, ex.Kind);
    }

    [Fact]
    public async Task StreamReplyAsync_WithoutApiKey_ThrowsNotConfiguredWithoutCallingTheApi()
    {
        var handler = FakeAnthropicHandler.Streaming("never sent");
        var service = new AnthropicChatService(null, new AiOptions(), NullLogger<AnthropicChatService>.Instance,
            new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => CollectAsync(service));

        Assert.Equal(AiErrorKind.NotConfigured, ex.Kind);
        Assert.Empty(handler.Requests);
    }
}
