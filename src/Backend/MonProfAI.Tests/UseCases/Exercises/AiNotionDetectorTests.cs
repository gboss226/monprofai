using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Exercises;
using MonProfAI.Domain.Entities;
using Xunit;

namespace MonProfAI.Tests.UseCases.Exercises;

public class AiNotionDetectorTests
{
    private static readonly Notion Fractions = new() { Id = Guid.NewGuid(), Title = "Additionner des fractions", Order = 1 };
    private static readonly Notion Pythagore = new() { Id = Guid.NewGuid(), Title = "Calculer une longueur avec Pythagore", Order = 1 };

    private sealed class ScriptedAi : IAiChatService
    {
        private readonly Func<IAsyncEnumerable<string>> _reply;
        public ScriptedAi(Func<IAsyncEnumerable<string>> reply) => _reply = reply;
        public AiChatRequest? LastRequest { get; private set; }

        public IAsyncEnumerable<string> StreamReplyAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return _reply();
        }
    }

    private static async IAsyncEnumerable<string> Say(string text)
    {
        await Task.Yield();
        yield return text;
    }

    private static async IAsyncEnumerable<string> Fail()
    {
        await Task.Yield();
        throw new AiServiceException(AiErrorKind.Unavailable);
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static AiNotionDetector NewDetector(IAiChatService ai)
    {
        var subject = new Subject { Name = "Mathématiques", Code = "maths" };
        var fractions = new Chapter { Title = "Fractions", SchoolLevel = SchoolLevel.Quatrieme, Order = 2 };
        fractions.Notions.Add(Fractions);
        var pythagore = new Chapter { Title = "Théorème de Pythagore", SchoolLevel = SchoolLevel.Quatrieme, Order = 7 };
        pythagore.Notions.Add(Pythagore);
        // Out of order on purpose: codes follow the chapter order, not the storage order.
        subject.Chapters.Add(pythagore);
        subject.Chapters.Add(fractions);

        var notions = new Mock<INotionRepository>();
        notions.Setup(r => r.GetReferentialAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { subject });
        return new AiNotionDetector(ai, notions.Object, NullLogger<AiNotionDetector>.Instance);
    }

    [Fact]
    public async Task DetectAsync_ReturnsTheNotionMatchingTheCode()
    {
        var ai = new ScriptedAi(() => Say("N2"));

        var id = await NewDetector(ai).DetectAsync("ABC est rectangle en A, AB = 3, AC = 4. Calcule BC.", SchoolLevel.Quatrieme);

        Assert.Equal(Pythagore.Id, id);
        var prompt = ai.LastRequest!.Messages.Single().Content;
        Assert.Contains("N1 | Mathématiques | 4ème | Fractions | Additionner des fractions", prompt);
        Assert.Contains("N2 | Mathématiques | 4ème | Théorème de Pythagore | Calculer une longueur avec Pythagore", prompt);
        Assert.Contains("<enonce>\nABC est rectangle en A, AB = 3, AC = 4. Calcule BC.\n</enonce>", prompt);
        Assert.Contains("L'élève est en 4ème", prompt);
    }

    [Theory]
    [InlineData("AUCUNE")]
    [InlineData("N9")]
    [InlineData("Je pense que c'est Pythagore")]
    public async Task DetectAsync_WhenTheAnswerIsNotAValidCode_ReturnsNull(string answer)
    {
        Assert.Null(await NewDetector(new ScriptedAi(() => Say(answer))).DetectAsync("Fais un dessin.", null));
    }

    [Fact]
    public async Task DetectAsync_WhenTheModelIsUnavailable_ReturnsNull()
    {
        Assert.Null(await NewDetector(new ScriptedAi(Fail)).DetectAsync("2/3 + 5/4", null));
    }

    [Theory]
    [InlineData("N1", 0)]
    [InlineData(" n1", null)]          // codes are upper case
    [InlineData("Réponse : N3.", 2)]
    [InlineData("N0", null)]
    [InlineData("N12", null)]
    [InlineData("AUCUNE", null)]
    public void ParseAnswer_AcceptsOnlyKnownCodes(string answer, int? expected)
    {
        Assert.Equal(expected, AiNotionDetector.ParseAnswer(answer, candidateCount: 3));
    }
}
