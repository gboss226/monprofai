using MonProfAI.Application.UseCases.Tutor;
using MonProfAI.Domain.Entities;
using Xunit;
using LearnerProfileEntity = MonProfAI.Domain.Entities.LearnerProfile;

namespace MonProfAI.Tests.UseCases.Tutor;

public class TutorPromptBuilderTests
{
    private static readonly Notion Pythagore = new()
    {
        Id = Guid.NewGuid(),
        Title = "Calculer une longueur avec le théorème de Pythagore",
        ShortDescription = "Dans un triangle rectangle, calculer l'hypoténuse ou un côté de l'angle droit.",
        Order = 1,
        Chapter = new Chapter
        {
            Title = "Théorème de Pythagore",
            SchoolLevel = SchoolLevel.Quatrieme,
            Order = 7,
            Subject = new Subject { Code = "maths", Name = "Mathématiques" },
        },
    };

    private static readonly Notion RacineCarree = new() { Id = Guid.NewGuid(), Title = "Carré et racine carrée", Order = 4 };

    static TutorPromptBuilderTests()
    {
        Pythagore.Prerequisites.Add(new NotionPrerequisite
        {
            NotionId = Pythagore.Id,
            PrerequisiteNotionId = RacineCarree.Id,
            PrerequisiteNotion = RacineCarree,
        });
    }

    private static LearnerProfileEntity Ines() => new()
    {
        SchoolLevel = SchoolLevel.Quatrieme,
        OnboardingCompleted = true,
        StrugglingSubjects = new List<string> { "Mathématiques" },
        Goal = LearningGoal.ImproveGrades,
        PreferredLearningStyle = LearningStyle.ConcreteExamples,
    };

    [Fact]
    public void Template_KeepsThePedagogicalRules()
    {
        var template = TutorPromptBuilder.LoadTemplate();

        Assert.Contains("une seule question courte", template);          // explain then check understanding
        Assert.Contains("ne répète pas ton explication", template);       // change approach after a mistake
        Assert.Contains("Tu ne fais jamais un exercice", template);       // no homework done for the student
        Assert.Contains("Contenus inappropriés pour un mineur", template); // refuse unsuitable topics
        Assert.DoesNotContain("<!--", template);                          // editor notes are stripped
    }

    [Fact]
    public void Build_ReplacesEveryPlaceholder()
    {
        var prompt = TutorPromptBuilder.Build(new TutorPromptContext(Ines(), Pythagore));

        Assert.DoesNotContain("{{", prompt);
        Assert.DoesNotContain(TutorPromptBuilder.StudentPlaceholder, prompt);
        Assert.DoesNotContain(TutorPromptBuilder.NotionPlaceholder, prompt);
    }

    [Fact]
    public void Build_IncludesTheStudentProfile()
    {
        var prompt = TutorPromptBuilder.Build(new TutorPromptContext(Ines(), Pythagore));

        Assert.Contains("- Classe : 4ème.", prompt);
        Assert.Contains("les exemples concrets", prompt);
        Assert.Contains("remonter ses notes", prompt);
        Assert.Contains("Matières où il se sent en difficulté : Mathématiques", prompt);
    }

    [Fact]
    public void Build_WithoutCompletedProfile_SaysSo()
    {
        var prompt = TutorPromptBuilder.Build(new TutorPromptContext(new LearnerProfileEntity(), Pythagore));

        Assert.Contains("n'a pas encore rempli son profil", prompt);
        Assert.DoesNotContain("- Classe :", prompt);
    }

    [Fact]
    public void Build_IncludesTheNotionAndItsPrerequisitesWithMastery()
    {
        var masteries = new Dictionary<Guid, NotionMastery>
        {
            [Pythagore.Id] = new() { NotionId = Pythagore.Id, MasteryScore = 0.35, AttemptCount = 5 },
            [RacineCarree.Id] = new() { NotionId = RacineCarree.Id, MasteryScore = 0.9, AttemptCount = 8 },
        };

        var prompt = TutorPromptBuilder.Build(
            new TutorPromptContext(Ines(), Pythagore, masteries[Pythagore.Id], masteries));

        Assert.Contains("- Notion : « Calculer une longueur avec le théorème de Pythagore »", prompt);
        Assert.Contains("Mathématiques, programme de 4ème", prompt);
        Assert.Contains("- Maîtrise de l'élève : fragile (35 % après 5 essais)", prompt);
        Assert.Contains("« Carré et racine carrée » : bien maîtrisée (90 % après 8 essais)", prompt);
    }

    [Fact]
    public void Build_WithoutNotion_LetsTheTutorHelpFreely()
    {
        var prompt = TutorPromptBuilder.Build(new TutorPromptContext(Ines(), null));

        Assert.Contains("Aucune notion n'a été choisie", prompt);
    }

    [Theory]
    [InlineData(0.0, 0, "jamais travaillée")]
    [InlineData(0.2, 3, "fragile (20 %")]
    [InlineData(0.6, 4, "en cours d'acquisition (60 %")]
    [InlineData(0.95, 9, "bien maîtrisée (95 %")]
    public void MasteryLabel_DependsOnTheScore(double score, int attempts, string expected)
    {
        var mastery = new NotionMastery { MasteryScore = score, AttemptCount = attempts };

        Assert.StartsWith(expected, TutorPromptBuilder.MasteryLabel(mastery));
    }
}
