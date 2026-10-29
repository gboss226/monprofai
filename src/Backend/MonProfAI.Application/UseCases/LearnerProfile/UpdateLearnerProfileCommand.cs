using MediatR;

namespace MonProfAI.Application.UseCases.LearnerProfile;

public record UpdateLearnerProfileCommand(
    Guid UserId,
    string SchoolLevel,
    List<string> StrugglingSubjects,
    string Goal,
    string PreferredLearningStyle
) : IRequest<LearnerProfileDto>;
