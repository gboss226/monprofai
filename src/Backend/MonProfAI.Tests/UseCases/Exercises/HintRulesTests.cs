using MonProfAI.Application.UseCases.Exercises;
using MonProfAI.Domain.Entities;
using Xunit;

namespace MonProfAI.Tests.UseCases.Exercises;

public class HintRulesTests
{
    private static Exercise With(int[] hintLevels, int wrongAttempts = 0, ExerciseStatus status = ExerciseStatus.InProgress)
    {
        var exercise = new Exercise { Status = status };
        foreach (var level in hintLevels)
            exercise.Hints.Add(new ExerciseHint { Level = level, Content = $"indice {level}" });
        for (var i = 0; i < wrongAttempts; i++)
            exercise.Attempts.Add(new ExerciseAttempt { Answer = "?", IsCorrect = false, Feedback = "non" });
        return exercise;
    }

    [Theory]
    [InlineData(new int[0], 1, true)]
    [InlineData(new int[0], 2, false)]
    [InlineData(new[] { 1 }, 2, true)]
    [InlineData(new[] { 1 }, 3, false)]
    [InlineData(new[] { 1, 2 }, 3, true)]
    [InlineData(new[] { 1, 2 }, 4, false)]
    [InlineData(new[] { 1, 2, 3 }, 4, true)]
    [InlineData(new[] { 1, 2 }, 2, true)] // already unlocked: shown again
    [InlineData(new int[0], 5, false)]
    [InlineData(new int[0], 0, false)]
    public void Hints_UnlockOneByOneInOrder(int[] unlocked, int level, bool allowed)
    {
        Assert.Equal(allowed, HintRules.CanUnlock(With(unlocked), level).Allowed);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Solution_UnlocksAfterSeveralWrongAnswers(int wrongAttempts, bool allowed)
    {
        Assert.Equal(allowed, HintRules.CanUnlock(With(new int[0], wrongAttempts), HintLevels.Solution).Allowed);
    }

    [Fact]
    public void Solution_UnlocksOnceTheExerciseIsSolved()
    {
        Assert.True(HintRules.CanUnlock(With(new[] { 1 }, status: ExerciseStatus.Solved), HintLevels.Solution).Allowed);
    }

    [Fact]
    public void Solution_LockedReason_CountsTheRemainingAttempts()
    {
        var decision = HintRules.CanUnlock(With(new[] { 1 }, wrongAttempts: 1), HintLevels.Solution);

        Assert.False(decision.Allowed);
        Assert.Contains("après 2 essai(s) de plus", decision.Reason);
    }

    [Theory]
    [InlineData(new int[0], 1)]
    [InlineData(new[] { 1, 2 }, 3)]
    [InlineData(new[] { 1, 2, 3, 4 }, null)]
    public void NextLevel_IsTheFirstMissingOne(int[] unlocked, int? expected)
    {
        Assert.Equal(expected, HintRules.NextLevel(With(unlocked)));
    }
}
