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
using MonProfAI.Application.UseCases.Tutor;

namespace MonProfAI.Api.Controllers;

/// <summary>
/// AI tutor. Answers stream as Server-Sent Events:
/// <c>data: {"text":"..."}</c> for each chunk, then <c>event: done</c>,
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

        var query = new AskTutorQuery(userId, request.Message ?? string.Empty, request.History, request.NotionId);
        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            await WriteJsonErrorAsync(StatusCodes.Status400BadRequest, "InvalidRequest",
                string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)), cancellationToken);
            return;
        }

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
            await WriteJsonErrorAsync(StatusFor(ex.Kind), ex.Kind.ToString(), ex.StudentMessage, cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
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

    private async Task WriteJsonErrorAsync(int status, string code, string message, CancellationToken cancellationToken)
    {
        Response.StatusCode = status;
        await Response.WriteAsJsonAsync(new { code, message }, cancellationToken);
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

public record AskTutorRequest(string? Message, List<TutorTurn>? History, Guid? NotionId = null);
