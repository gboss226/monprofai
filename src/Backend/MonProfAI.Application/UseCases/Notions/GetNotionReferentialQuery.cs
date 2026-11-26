using MediatR;

namespace MonProfAI.Application.UseCases.Notions;

/// <summary>The notion referential (subjects → chapters → notions), sorted for display.</summary>
public record GetNotionReferentialQuery : IRequest<IReadOnlyList<SubjectDto>>;

public record SubjectDto(Guid Id, string Code, string Name, IReadOnlyList<ChapterDto> Chapters);

/// <param name="SchoolLevel">Enum name, e.g. "Quatrieme", as in the learner profile.</param>
public record ChapterDto(Guid Id, string SchoolLevel, int Order, string Title, IReadOnlyList<NotionDto> Notions);

public record NotionDto(Guid Id, int Order, string Title, string? ShortDescription);
