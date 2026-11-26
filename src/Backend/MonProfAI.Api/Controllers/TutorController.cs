using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonProfAI.Api.Ai;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Conversations;
using MonProfAI.Application.UseCases.Tutor;

namespace MonProfAI.Api.Controllers;

/// <summary>
/// AI tutor. Answers stream as Server-Sent Events: first <c>event: conversation</c> with
/// <c>{"conversationId"}</c>, then <c>data: {"text":"..."}</c> for each chunk, then <c>event: done</c>,
/// or <c>event: error</c> with <c>{"code","message"}</c> if the provider fails mid-answer.
/// Failures before the first chunk come back as a plain JSON error with an HTTP status instead.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TutorController : ControllerBase
{
    // Keep accents and apostrophes readable in the stream instead of é escapes.
    // Safe here: the payload is read by JSON.parse, never inlined into HTML.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly IMediator _mediator;
    private readonly IValidator<AskTutorQuery> _validator;

    public TutorController(IMediator mediator, IValidator<AskTutorQuery> validator)
    {
        _mediator = mediator;
        _validator = validator;
    }

    [HttpPost("ask")]
    [EnableRateLimiting(AiSetup.RateLimitPolicy)]
    public async Task Ask([FromBody] AskTutorRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Check the question before creating anything, so an invalid one leaves no empty conversation behind.
        var message = request.Message ?? string.Empty;
        var validation = await _validator.ValidateAsync(new AskTutorQuery(userId, Guid.Empty, message), cancellationToken);
        if (!validation.IsValid)
        {
            await WriteJsonErrorAsync(StatusCodes.Status400BadRequest, "InvalidRequest",
                string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)), null, cancellationToken);
            return;
        }

        Guid conversationId;
        try
        {
            conversationId = await _mediator.Send(
                new OpenConversationCommand(userId, request.ConversationId, request.NotionId), cancellationToken);
        }
        catch (ConversationNotFoundException)
        {
            await WriteJsonErrorAsync(StatusCodes.Status404NotFound, "ConversationNotFound",
                "Cette conversation est introuvable.", null, cancellationToken);
            return;
        }
        catch (ValidationException ex)
        {
            await WriteJsonErrorAsync(StatusCodes.Status400BadRequest, "InvalidRequest", ex.Message, null, cancellationToken);
            return;
        }

        var query = new AskTutorQuery(userId, conversationId, message);
        await using var chunks = _mediator.CreateStream(query, cancellationToken).GetAsyncEnumerator(cancellationToken);

        // Wait for the first chunk before committing to a stream, so that early failures
        // (timeout, quota, missing key...) get a real HTTP status.
        bool hasChunk;
        try
        {
            hasChunk = await chunks.MoveNextAsync();
        }
        catch (AiServiceException ex)
        {
            // The question is saved: the conversation id lets the client keep writing to it.
            await WriteJsonErrorAsync(StatusFor(ex.Kind), ex.Kind.ToString(), ex.StudentMessage, conversationId, cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await WriteEventAsync("conversation", new { conversationId }, cancellationToken);
            while (hasChunk)
            {
                await WriteEventAsync(null, new { text = chunks.Current }, cancellationToken);
                hasChunk = await chunks.MoveNextAsync();
            }

            await WriteEventAsync("done", new { }, cancellationToken);
        }
        catch (AiServiceException ex)
        {
            await WriteEventAsync("error", new { code = ex.Kind.ToString(), message = ex.StudentMessage }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The student closed the page: nothing left to send.
        }
    }

    private static int StatusFor(AiErrorKind kind) => kind switch
    {
        AiErrorKind.Timeout => StatusCodes.Status504GatewayTimeout,
        AiErrorKind.InvalidResponse => StatusCodes.Status502BadGateway,
        AiErrorKind.Refused => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status503ServiceUnavailable,
    };

    private async Task WriteJsonErrorAsync(
        int status, string code, string message, Guid? conversationId, CancellationToken cancellationToken)
    {
        Response.StatusCode = status;
        await Response.WriteAsJsonAsync(new { code, message, conversationId }, cancellationToken);
    }

    private async Task WriteEventAsync(string? eventName, object payload, CancellationToken cancellationToken)
    {
        var frame = eventName is null
            ? $"data: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n"
            : $"event: {eventName}\ndata: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n";
        await Response.WriteAsync(frame, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}

/// <param name="ConversationId">Conversation to continue. Omit it to start a new one about <paramref name="NotionId"/>.</param>
/// <param name="NotionId">Notion of a new conversation. Ignored when continuing one.</param>
public record AskTutorRequest(string? Message, Guid? ConversationId = null, Guid? NotionId = null);
