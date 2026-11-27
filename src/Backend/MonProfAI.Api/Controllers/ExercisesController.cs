using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonProfAI.Api.Ai;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Exercises;

namespace MonProfAI.Api.Controllers;

/// <summary>Exercises entered by the logged-in student. Nobody else's are ever reachable here.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExercisesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExercisesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Saves an exercise. Without a notion, the model picks one from the referential; if it cannot,
    /// nothing is saved and the answer is 422 so that the student chooses the notion himself.
    /// </summary>
    [HttpPost]
    // Detecting the notion calls the model: it shares the AI tutor's per-student budget.
    [EnableRateLimiting(AiSetup.RateLimitPolicy)]
    public async Task<ActionResult<CreatedExerciseDto>> Create(
        [FromBody] CreateExerciseRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _mediator.Send(
                new CreateExerciseCommand(userId, request.Statement ?? string.Empty, request.NotionId), cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { code = "InvalidRequest", message = ex.Message });
        }
        catch (NotionNotDetectedException)
        {
            return UnprocessableEntity(new
            {
                code = "NotionNotDetected",
                message = "Je n'ai pas réussi à trouver la notion de cet exercice. Choisis-la dans la liste.",
            });
        }
    }

    /// <summary>
    /// Reads the statement from a photo of an exercise and returns it as text, for the student to check before
    /// saving it. Nothing is stored, the photo included.
    /// </summary>
    [HttpPost("transcribe")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageFormat.MaxBytes + 64 * 1024)]
    [EnableRateLimiting(AiSetup.RateLimitPolicy)]
    public async Task<ActionResult<TranscriptionDto>> Transcribe(IFormFile? image, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out _))
            return Unauthorized();

        if (image is null || image.Length == 0)
            return BadRequest(new { code = "InvalidRequest", message = "Ajoute une photo de ton exercice." });
        if (image.Length > ImageFormat.MaxBytes)
            return BadRequest(new { code = "ImageTooLarge", message = "La photo est trop lourde (5 Mo maximum)." });

        byte[] bytes;
        await using (var stream = image.OpenReadStream())
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer, cancellationToken);
            bytes = buffer.ToArray();
        }

        var mediaType = ImageFormat.Sniff(bytes);
        if (mediaType is null)
            return BadRequest(new { code = "UnsupportedImage", message = "Ce fichier n'est pas une photo (JPEG, PNG, WebP ou GIF)." });

        try
        {
            var statement = await _mediator.Send(
                new TranscribeExerciseCommand(new AiImage(mediaType, bytes)), cancellationToken);
            return Ok(new TranscriptionDto(statement));
        }
        catch (ExerciseNotReadableException)
        {
            return UnprocessableEntity(new
            {
                code = "NotReadable",
                message = "Je n'arrive pas à lire d'énoncé sur cette photo. Reprends-la bien à plat, nette et éclairée, ou tape l'énoncé.",
            });
        }
        catch (AiServiceException ex)
        {
            return StatusCode(ex.Kind == AiErrorKind.Timeout ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status503ServiceUnavailable,
                new { code = ex.Kind.ToString(), message = ex.StudentMessage });
        }
    }

    /// <summary>One of my exercises. 404 if it does not exist or is someone else's.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var exercise = await _mediator.Send(new GetExerciseQuery(userId, id), cancellationToken);
        return exercise is null
            ? NotFound(new { code = "ExerciseNotFound", message = "Cet exercice est introuvable." })
            : Ok(exercise);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

/// <param name="Statement">Text read from the photo, formulas in LaTeX. Meant to be checked by the student.</param>
public record TranscriptionDto(string Statement);

/// <param name="NotionId">Optional: the notion chosen by the student. Omit it to let the model detect it.</param>
public record CreateExerciseRequest(string? Statement, Guid? NotionId = null);
