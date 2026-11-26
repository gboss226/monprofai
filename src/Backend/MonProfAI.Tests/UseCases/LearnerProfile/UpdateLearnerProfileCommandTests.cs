using Moq;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.LearnerProfile;
using MonProfAI.Domain.Entities;
using Xunit;
using LearnerProfileEntity = MonProfAI.Domain.Entities.LearnerProfile;

namespace MonProfAI.Tests.UseCases.LearnerProfile;

public class UpdateLearnerProfileCommandTests
{
    private static UpdateLearnerProfileCommand ValidCommand(Guid userId) => new(
        userId,
        "Troisieme",
        new List<string> { "Mathématiques", "Anglais" },
        "Brevet",
        "Diagrams"
    );

    [Fact]
    public async Task Handle_FillsProfileAndMarksOnboardingCompleted()
    {
        var userId = Guid.NewGuid();
        var profile = new LearnerProfileEntity { Id = Guid.NewGuid(), UserId = userId };
        var repo = new Mock<ILearnerProfileRepository>();
        repo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(profile);

        var result = await new UpdateLearnerProfileCommandHandler(repo.Object)
            .Handle(ValidCommand(userId), CancellationToken.None);

        Assert.True(result.OnboardingCompleted);
        Assert.Equal("Troisieme", result.SchoolLevel);
        Assert.Equal("Brevet", result.Goal);
        Assert.Equal("Diagrams", result.PreferredLearningStyle);
        Assert.Equal(new[] { "Mathématiques", "Anglais" }, result.StrugglingSubjects);
        Assert.Equal(SchoolLevel.Troisieme, profile.SchoolLevel);
        repo.Verify(r => r.UpdateAsync(profile), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutProfile_Throws()
    {
        var repo = new Mock<ILearnerProfileRepository>();
        repo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync((LearnerProfileEntity?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateLearnerProfileCommandHandler(repo.Object)
                .Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void Validator_AcceptsValidAnswers()
    {
        var result = new UpdateLearnerProfileCommandValidator().Validate(ValidCommand(Guid.NewGuid()));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsNoStrugglingSubject()
    {
        var command = ValidCommand(Guid.NewGuid()) with { StrugglingSubjects = new List<string>() };
        Assert.True(new UpdateLearnerProfileCommandValidator().Validate(command).IsValid);
    }

    [Theory]
    [InlineData("CM2", "Brevet", "Diagrams")]
    [InlineData("3", "Brevet", "Diagrams")]
    [InlineData("Troisieme", "Licence", "Diagrams")]
    [InlineData("Troisieme", "Brevet", "")]
    public void Validator_RejectsUnknownChoices(string schoolLevel, string goal, string style)
    {
        var command = ValidCommand(Guid.NewGuid()) with
        {
            SchoolLevel = schoolLevel,
            Goal = goal,
            PreferredLearningStyle = style,
        };
        Assert.False(new UpdateLearnerProfileCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Validator_RejectsFreeTextSubject()
    {
        var command = ValidCommand(Guid.NewGuid()) with
        {
            StrugglingSubjects = new List<string> { "Mon prof de maths s'appelle M. Dupont" },
        };
        Assert.False(new UpdateLearnerProfileCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Validator_RejectsDuplicateSubjects()
    {
        var command = ValidCommand(Guid.NewGuid()) with
        {
            StrugglingSubjects = new List<string> { "SVT", "SVT" },
        };
        Assert.False(new UpdateLearnerProfileCommandValidator().Validate(command).IsValid);
    }
}
