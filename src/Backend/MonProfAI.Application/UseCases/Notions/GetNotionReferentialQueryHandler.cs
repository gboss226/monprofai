using MediatR;
using MonProfAI.Application.Interfaces;

namespace MonProfAI.Application.UseCases.Notions;

public class GetNotionReferentialQueryHandler : IRequestHandler<GetNotionReferentialQuery, IReadOnlyList<SubjectDto>>
{
    private readonly INotionRepository _notionRepository;

    public GetNotionReferentialQueryHandler(INotionRepository notionRepository)
    {
        _notionRepository = notionRepository;
    }

    public async Task<IReadOnlyList<SubjectDto>> Handle(
        GetNotionReferentialQuery request, CancellationToken cancellationToken)
    {
        var subjects = await _notionRepository.GetReferentialAsync(cancellationToken);

        return subjects
            .Select(s => new SubjectDto(
                s.Id,
                s.Code,
                s.Name,
                s.Chapters
                    .OrderBy(c => c.SchoolLevel)
                    .ThenBy(c => c.Order)
                    .Select(c => new ChapterDto(
                        c.Id,
                        c.SchoolLevel.ToString(),
                        c.Order,
                        c.Title,
                        c.Notions
                            .OrderBy(n => n.Order)
                            .Select(n => new NotionDto(n.Id, n.Order, n.Title, n.ShortDescription))
                            .ToList()))
                    .ToList()))
            .ToList();
    }
}
