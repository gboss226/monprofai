using MediatR;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public class UpdateLearnerProfileCommandHandler : IRequestHandler<UpdateLearnerProfileCommand, LearnerProfileDto>
{
    private readonly ILearnerProfileRepository _learnerProfileRepository;

    public UpdateLearnerProfileCommandHandler(ILearnerProfileRepository learnerProfileRepository)
    {
        _learnerProfileRepository = learnerProfileRepository;
    }

    public async Task<LearnerProfileDto> Handle(UpdateLearnerProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _learnerProfileRepository.GetByUserIdAsync(request.UserId)
            ?? throw new InvalidOperationException("Learner profile not found");

        profile.SchoolLevel = Enum.Parse<SchoolLevel>(request.SchoolLevel);
        profile.StrugglingSubjects = request.StrugglingSubjects;
        profile.Goal = Enum.Parse<LearningGoal>(request.Goal);
        profile.PreferredLearningStyle = Enum.Parse<LearningStyle>(request.PreferredLearningStyle);
        profile.OnboardingCompleted = true;
        profile.UpdatedAt = DateTime.UtcNow;

        await _learnerProfileRepository.UpdateAsync(profile);

        return new LearnerProfileDto(
            profile.Id,
            profile.UserId,
            profile.SchoolLevel.ToString(),
            profile.StrugglingSubjects,
            profile.Goal?.ToString(),
            profile.PreferredLearningStyle?.ToString(),
            profile.LearningObjectives,
            profile.OnboardingCompleted,
            profile.UpdatedAt
        );
    }
}
