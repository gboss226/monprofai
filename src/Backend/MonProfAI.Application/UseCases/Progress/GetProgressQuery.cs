using MediatR;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Application.UseCases.Progress;

/// <summary>The student's mastery of each notion he has worked on, and his latest finished exercises.</summary>
public sealed record GetProgressQuery(Guid UserId) : IRequest<ProgressDto>;

public sealed record ProgressDto(IReadOnlyList<NotionMasteryDto> Masteries, IReadOnlyList<RecentResultDto> RecentResults);

/// <param name="Score">From 0 to 1.</param>
/// <param name="ExercisesCount">Exercises finished on this notion.</param>
public sealed record NotionMasteryDto(Guid NotionId, double Score, int ExercisesCount, DateTime LastActivity);

public sealed record RecentResultDto(
    Guid ExerciseId,
    Guid NotionId,
    string NotionTitle,
    bool Succeeded,
    int HintsUsed,
    int Attempts,
    double MasteryBefore,
    double MasteryAfter,
    DateTime CompletedAt);

public class GetProgressQueryHandler : IRequestHandler<GetProgressQuery, ProgressDto>
{
    public const int RecentResultsCount = 10;

    private readonly INotionRepository _notions;
    private readonly IExerciseRepository _exercises;

    public GetProgressQueryHandler(INotionRepository notions, IExerciseRepository exercises)
    {
        _notions = notions;
        _exercises = exercises;
    }

    public async Task<ProgressDto> Handle(GetProgressQuery request, CancellationToken cancellationToken)
    {
        var masteries = await _notions.GetAllMasteriesAsync(request.UserId, cancellationToken);
        var results = await _exercises.GetRecentResultsAsync(request.UserId, RecentResultsCount, cancellationToken);

        return new ProgressDto(
            masteries
                .Select(m => new NotionMasteryDto(m.NotionId, m.MasteryScore, m.AttemptCount, m.LastActivity))
                .ToList(),
            results
                .Select(r => new RecentResultDto(
                    r.ExerciseId, r.Exercise.NotionId, r.Exercise.Notion.Title, r.Succeeded, r.HintsUsed, r.Attempts,
                    r.MasteryBefore, r.MasteryAfter, r.CompletedAt))
                .ToList());
    }
}
