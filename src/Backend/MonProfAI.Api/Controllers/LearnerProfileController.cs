using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonProfAI.Application.UseCases.LearnerProfile;
using System.Security.Claims;

namespace MonProfAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LearnerProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public LearnerProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<LearnerProfileDto>> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        try
        {
            var profile = await _mediator.Send(new GetLearnerProfileQuery(userGuid));
            return Ok(profile);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Profile not found" });
        }
    }

    [HttpPost("onboarding")]
    public async Task<ActionResult<LearnerProfileDto>> CompleteOnboarding(
        [FromBody] CompleteOnboardingRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var userGuid))
            return Unauthorized();

        try
        {
            var profile = await _mediator.Send(new UpdateLearnerProfileCommand(
                userGuid,
                request.SchoolLevel,
                request.StrugglingSubjects,
                request.Goal,
                request.PreferredLearningStyle
            ));
            return Ok(profile);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Profile not found" });
        }
    }
}

public record CompleteOnboardingRequest(
    string SchoolLevel,
    List<string> StrugglingSubjects,
    string Goal,
    string PreferredLearningStyle
);
