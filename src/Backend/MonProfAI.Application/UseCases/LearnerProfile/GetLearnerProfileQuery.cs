using MediatR;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public record GetLearnerProfileQuery(Guid UserId) : IRequest<LearnerProfileDto>;

public record LearnerProfileDto(
    Guid Id,
    Guid UserId,
    string SchoolLevel,
    string? LearningObjectives,
    string? ExplanationPreferences,
    DateTime UpdatedAt
);
