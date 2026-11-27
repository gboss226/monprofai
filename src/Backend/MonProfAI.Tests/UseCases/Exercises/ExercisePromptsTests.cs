using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Exercises;
using MonProfAI.Domain.Entities;
using Xunit;

namespace MonProfAI.Tests.UseCases.Exercises;

public class ExercisePromptsTests
{
    private static Exercise Pythagore()
    {
        var exercise = new Exercise
        {
            Statement = "ABC est rectangle en A, AB = 3 cm et AC = 4 cm. Calcule BC.",
            Notion = new Notion
            {
                Title = "Calculer une longueur avec le théorème de Pythagore",
                Chapter = new Chapter
                {
                    Title = "Théorème de Pythagore",
                    SchoolLevel = SchoolLevel.Quatrieme,
                    Subject = new Subject { Name = "Mathématiques", Code = "maths" },
                },
            },
        };
        exercise.Hints.Add(new ExerciseHint { Level = 1, Content = "Dans un triangle rectangle, $a^2 + b^2 = c^2$." });
        exercise.Attempts.Add(new ExerciseAttempt { Answer = "BC = 7", IsCorrect = false, Feedback = "non" });
        return exercise;
    }

    [Fact]
    public void HintTemplate_HasFourLevelsThatGetMoreRevealing()
    {
        var (body, levels) = ExercisePrompts.LoadHintTemplate();

        Assert.Equal(new[] { 1, 2, 3, 4 }, levels.Keys.Order());
        Assert.Contains("uniquement l'indice du niveau demandé", body);
        Assert.Contains("n'utilise pas", levels[1]);        // reminder: not on the exercise's numbers
        Assert.Contains("sans faire aucun calcul", levels[2]); // method: no computation
        Assert.Contains("Arrête-toi là", levels[3]);          // first step only
        Assert.Contains("solution complète", levels[4]);
        Assert.DoesNotContain("[[NIVEAU", body);
        Assert.DoesNotContain("<!--", body);
    }

    [Fact]
    public void BuildHintPrompt_GivesOnlyTheRequestedLevel_AndWhatWasAlreadyGiven()
    {
        var prompt = ExercisePrompts.BuildHintPrompt(new ExercisePromptContext(Pythagore(), null), 2);

        Assert.DoesNotContain("{{", prompt);
        Assert.Contains("**Niveau 2 : la méthode.**", prompt);
        Assert.DoesNotContain("**Niveau 4", prompt);
        Assert.Contains("<enonce>\nABC est rectangle en A, AB = 3 cm et AC = 4 cm. Calcule BC.\n</enonce>", prompt);
        Assert.Contains("Indice 1 (Rappel de la notion) déjà donné", prompt);
        Assert.Contains("<reponse>\nBC = 7\n</reponse>", prompt);
        Assert.Contains("Mathématiques, programme de 4ème", prompt);
    }

    [Fact]
    public void BuildAnswerCheckPrompt_ForbidsGivingTheSolution()
    {
        var prompt = ExercisePrompts.BuildAnswerCheckPrompt(new ExercisePromptContext(Pythagore(), null));

        Assert.DoesNotContain("{{", prompt);
        Assert.Contains("sans jamais lui donner la solution", prompt);
        Assert.Contains("uniquement `JUSTE` ou `FAUX`", prompt);
        Assert.Contains("Calcule BC.", prompt);
    }

    [Theory]
    [InlineData("JUSTE\nBravo, $BC = 5$ cm.", true, "Bravo, $BC = 5$ cm.")]
    [InlineData("**FAUX**\n\nTu as additionné les longueurs.", false, "Tu as additionné les longueurs.")]
    [InlineData("juste.", true, "Bravo, c'est juste !")]
    public void ParseVerdict_ReadsTheFirstLine(string reply, bool expectedCorrect, string expectedFeedback)
    {
        var (isCorrect, feedback) = SubmitAnswerCommandHandler.ParseVerdict(reply);

        Assert.Equal(expectedCorrect, isCorrect);
        Assert.Equal(expectedFeedback, feedback);
    }

    [Theory]
    [InlineData("Ta réponse est juste !")]
    [InlineData("PRESQUE\nencore un effort")]
    public void ParseVerdict_RejectsAReplyWithoutVerdict(string reply)
    {
        var ex = Assert.Throws<AiServiceException>(() => SubmitAnswerCommandHandler.ParseVerdict(reply));
        Assert.Equal(AiErrorKind.InvalidResponse, ex.Kind);
    }
}
