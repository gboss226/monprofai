using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonProfAI.Application.UseCases.Notions;

namespace MonProfAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Subjects, their chapters per school level, and the notions of each chapter.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectDto>>> GetReferential(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetNotionReferentialQuery(), cancellationToken));
    }
}
