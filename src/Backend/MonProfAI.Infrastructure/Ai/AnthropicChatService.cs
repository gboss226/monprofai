using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.Logging;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Infrastructure.Ai;

/// <summary>
/// <see cref="IAiChatService"/> backed by Claude through the official Anthropic SDK.
/// To switch provider, write another implementation and register it instead: nothing else depends on this class.
/// </summary>
public sealed class AnthropicChatService : IAiChatService
{
    private const string ServerSideFallbackBeta = "server-side-fallback-2026-07-01";

    private readonly AnthropicClient? _client;
    private readonly AiOptions _options;
    private readonly ILogger<AnthropicChatService> _logger;

    /// <param name="apiKey">Value of AI_API_KEY. When empty, every call fails with <see cref="AiErrorKind.NotConfigured"/>.</param>
    /// <param name="httpClient">Optional, for tests: lets them answer in place of the real API.</param>
    public AnthropicChatService(
        string? apiKey, AiOptions options, ILogger<AnthropicChatService> logger, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger;
        if (string.IsNullOrWhiteSpace(apiKey))
            return;

        _client = httpClient is null
            ? new AnthropicClient
            {
                ApiKey = apiKey,
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                MaxRetries = 2,
            }
            : new AnthropicClient
            {
                ApiKey = apiKey,
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                MaxRetries = 0,
                HttpClient = httpClient,
            };
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        AiChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            _logger.LogError("AI call refused: {Variable} is not set", AiOptions.ApiKeyVariable);
            throw new AiServiceException(AiErrorKind.NotConfigured);
        }

        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = request.SystemPrompt,
            Messages = request.Messages.Select(ToParam).ToList(),
            // A tutoring chat does not need deep reasoning; low effort keeps answers fast and cheap.
            OutputConfig = new BetaOutputConfig { Effort = Effort.Low },
            // If a safety classifier declines, let the API retry on a fallback model instead of failing.
            Betas = [ServerSideFallbackBeta],
            Fallbacks = new Default(),
        };

        await using var events = _client.Beta.Messages
            .CreateStreaming(parameters, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        var producedText = false;
        string? stopReason = null;
        while (true)
        {
            BetaRawMessageStreamEvent streamEvent;
            try
            {
                if (!await events.MoveNextAsync())
                    break;
                streamEvent = events.Current;
            }
            catch (Exception ex) when (ex is not AiServiceException && !cancellationToken.IsCancellationRequested)
            {
                throw Translate(ex);
            }

            if (streamEvent.TryPickContentBlockDelta(out var blockDelta) &&
                blockDelta.Delta.TryPickText(out var text) &&
                text.Text.Length > 0)
            {
                producedText = true;
                yield return text.Text;
            }
            else if (streamEvent.TryPickDelta(out var messageDelta) && messageDelta.Delta.StopReason is { } reason)
            {
                stopReason = reason.Raw().ToString();
            }
        }

        if (string.Equals(stopReason, "refusal", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("AI model refused to answer");
            throw new AiServiceException(AiErrorKind.Refused);
        }

        if (!producedText)
        {
            _logger.LogWarning("AI stream ended without text (stop reason: {StopReason})", stopReason);
            throw new AiServiceException(AiErrorKind.InvalidResponse);
        }
    }

    private static BetaMessageParam ToParam(AiChatMessage message)
    {
        if (message.Role == AiChatRole.Assistant)
            return new BetaAssistantMessageParam { Content = message.Content };
        if (message.Images is not { Count: > 0 } images)
            return new BetaUserMessageParam { Content = message.Content };

        // Images first, then the text that refers to them.
        var blocks = new List<BetaContentBlockParam>();
        foreach (var image in images)
        {
            blocks.Add(new BetaImageBlockParam
            {
                Source = new BetaBase64ImageSource
                {
                    Data = Convert.ToBase64String(image.Data),
                    MediaType = image.MediaType,
                },
            });
        }
        blocks.Add(new BetaTextBlockParam { Text = message.Content });
        return new BetaUserMessageParam { Content = blocks };
    }

    private AiServiceException Translate(Exception ex)
    {
        var kind = ex switch
        {
            AnthropicRateLimitException => AiErrorKind.QuotaExceeded,
            AnthropicUnauthorizedException => AiErrorKind.NotConfigured,
            Anthropic5xxException => AiErrorKind.Unavailable,
            AnthropicApiException { StatusCode: (System.Net.HttpStatusCode)402 } => AiErrorKind.QuotaExceeded,
            AnthropicApiException { StatusCode: System.Net.HttpStatusCode.Forbidden } => AiErrorKind.NotConfigured,
            AnthropicApiException => AiErrorKind.InvalidResponse,
            AnthropicSseException => AiErrorKind.InvalidResponse,
            AnthropicInvalidDataException => AiErrorKind.InvalidResponse,
            System.Text.Json.JsonException => AiErrorKind.InvalidResponse,
            TimeoutException or TaskCanceledException or OperationCanceledException => AiErrorKind.Timeout,
            AnthropicIOException or HttpRequestException => AiErrorKind.Unavailable,
            _ => AiErrorKind.Unavailable,
        };

        _logger.LogError(ex, "AI provider call failed ({Kind})", kind);
        return new AiServiceException(kind, ex);
    }
}
