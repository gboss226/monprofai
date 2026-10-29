using MediatR;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public record GetLearnerProfileQuery(Guid UserId) : IRequest<LearnerProfileDto>;

public record LearnerProfileDto(
    Guid Id,
    Guid UserId,
    string SchoolLevel,
    List<string> StrugglingSubjects,
    string? Goal,
    string? PreferredLearningStyle,
    string? LearningObjectives,
    bool OnboardingCompleted,
    DateTime UpdatedAt
);
