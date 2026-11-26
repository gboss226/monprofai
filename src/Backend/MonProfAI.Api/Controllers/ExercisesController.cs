using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonProfAI.Api.Ai;
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

/// <param name="NotionId">Optional: the notion chosen by the student. Omit it to let the model detect it.</param>
public record CreateExerciseRequest(string? Statement, Guid? NotionId = null);
