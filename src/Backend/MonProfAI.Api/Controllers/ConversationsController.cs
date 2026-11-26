using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonProfAI.Application.UseCases.Conversations;

namespace MonProfAI.Api.Controllers;

/// <summary>The logged-in student's tutor conversations. Nobody else's are ever reachable here.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>My conversations, most recent first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _mediator.Send(new ListConversationsQuery(userId), cancellationToken));
    }

    /// <summary>One of my conversations with its messages. 404 if it does not exist or is someone else's.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var conversation = await _mediator.Send(new GetConversationQuery(userId, id), cancellationToken);
        return conversation is null
            ? NotFound(new { code = "ConversationNotFound", message = "Cette conversation est introuvable." })
            : Ok(conversation);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
