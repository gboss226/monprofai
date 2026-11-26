using Moq;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Notions;
using MonProfAI.Domain.Entities;
using Xunit;

namespace MonProfAI.Tests.UseCases.Notions;

public class GetNotionReferentialQueryHandlerTests
{
    [Fact]
    public async Task Handle_SortsChaptersByLevelThenOrder_AndNotionsByOrder()
    {
        var subject = new Subject { Id = Guid.NewGuid(), Code = "maths", Name = "Mathématiques" };
        subject.Chapters.Add(new Chapter { Title = "3e-1", SchoolLevel = SchoolLevel.Troisieme, Order = 1 });
        var chapter4e2 = new Chapter { Title = "4e-2", SchoolLevel = SchoolLevel.Quatrieme, Order = 2 };
        chapter4e2.Notions.Add(new Notion { Title = "B", Order = 2 });
        chapter4e2.Notions.Add(new Notion { Title = "A", Order = 1 });
        subject.Chapters.Add(chapter4e2);
        subject.Chapters.Add(new Chapter { Title = "4e-1", SchoolLevel = SchoolLevel.Quatrieme, Order = 1 });

        var repository = new Mock<INotionRepository>();
        repository.Setup(r => r.GetReferentialAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { subject });

        var result = await new GetNotionReferentialQueryHandler(repository.Object)
            .Handle(new GetNotionReferentialQuery(), CancellationToken.None);

        var chapters = Assert.Single(result).Chapters;
        Assert.Equal(new[] { "4e-1", "4e-2", "3e-1" }, chapters.Select(c => c.Title));
        Assert.Equal("Quatrieme", chapters[0].SchoolLevel);
        Assert.Equal(new[] { "A", "B" }, chapters[1].Notions.Select(n => n.Title));
    }
}
