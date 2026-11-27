using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonProfAI.Application.UseCases.Progress;

namespace MonProfAI.Api.Controllers;

/// <summary>The logged-in student's progress: mastery per notion and latest finished exercises.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ProgressDto>> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        return Ok(await _mediator.Send(new GetProgressQuery(userId), cancellationToken));
    }
}
