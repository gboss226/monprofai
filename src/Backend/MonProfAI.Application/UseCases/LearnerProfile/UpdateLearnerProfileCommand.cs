using MediatR;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public record UpdateLearnerProfileCommand(
    Guid UserId,
    string SchoolLevel,
    string? LearningObjectives,
    string? ExplanationPreferences
) : IRequest<LearnerProfileDto>;
