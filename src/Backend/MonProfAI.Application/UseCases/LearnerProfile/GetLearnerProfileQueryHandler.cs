using MediatR;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public class GetLearnerProfileQueryHandler : IRequestHandler<GetLearnerProfileQuery, LearnerProfileDto>
{
    private readonly ILearnerProfileRepository _learnerProfileRepository;

    public GetLearnerProfileQueryHandler(ILearnerProfileRepository learnerProfileRepository)
    {
        _learnerProfileRepository = learnerProfileRepository;
    }

    public async Task<LearnerProfileDto> Handle(GetLearnerProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _learnerProfileRepository.GetByUserIdAsync(request.UserId)
            ?? throw new InvalidOperationException("Learner profile not found");

        return new LearnerProfileDto(
            profile.Id,
            profile.UserId,
            profile.SchoolLevel.ToString(),
            profile.LearningObjectives,
            profile.ExplanationPreferences,
            profile.UpdatedAt
        );
    }
}
