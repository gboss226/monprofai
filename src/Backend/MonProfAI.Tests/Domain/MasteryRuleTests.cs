using MonProfAI.Domain.Mastery;
using Xunit;

namespace MonProfAI.Tests.Domain;

/// <summary>Pins the behaviour of the mastery rule: change these tests on purpose when the rule changes.</summary>
public class MasteryRuleTests
{
    private static ExerciseOutcome Success(int hints = 0, int attempts = 1) => new(true, hints, attempts);
    private static readonly ExerciseOutcome Failure = new(false, 4, 2);

    [Fact]
    public void SuccessWithoutHint_IsAStrongRise()
    {
        Assert.Equal(0.75, MasteryRule.Next(0.5, Success()), precision: 6);
    }

    [Fact]
    public void SuccessWithManyHints_IsASlightRise()
    {
        var next = MasteryRule.Next(0.5, Success(hints: 3));

        Assert.Equal(0.55, next, precision: 6);
        Assert.InRange(next - 0.5, 0.01, 0.1);
    }

    [Fact]
    public void Failure_IsADrop()
    {
        Assert.Equal(0.35, MasteryRule.Next(0.5, Failure), precision: 6);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(0.6)]
    [InlineData(0.95)]
    public void TheFewerHints_TheBiggerTheRise_AndFailureIsAlwaysBelow(double current)
    {
        var noHint = MasteryRule.Next(current, Success(0));
        var oneHint = MasteryRule.Next(current, Success(1));
        var twoHints = MasteryRule.Next(current, Success(2));
        var threeHints = MasteryRule.Next(current, Success(3));
        var failure = MasteryRule.Next(current, Failure);

        Assert.True(noHint > oneHint && oneHint > twoHints && twoHints > threeHints);
        Assert.True(threeHints > current);
        Assert.True(failure <= current);
    }

    [Fact]
    public void WrongAnswersBeforeSucceeding_ShrinkTheRise_ButStillRaise()
    {
        var firstTry = MasteryRule.Next(0.5, Success(hints: 0, attempts: 1));
        var thirdTry = MasteryRule.Next(0.5, Success(hints: 0, attempts: 3));
        var tenthTry = MasteryRule.Next(0.5, Success(hints: 0, attempts: 10));

        Assert.True(firstTry > thirdTry && thirdTry > 0.5);
        Assert.Equal(0.625, tenthTry, precision: 6); // penalty capped at half the rate
    }

    [Fact]
    public void ANotionNeverWorkedOn_StartsFromTheInitialScore()
    {
        Assert.Equal(MasteryRule.Next(MasteryRule.InitialScore, Success()), MasteryRule.Next(null, Success()));
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(1.7)]
    public void TheScore_StaysWithinZeroAndOne(double current)
    {
        foreach (var outcome in new[] { Success(), Success(5, 20), Failure, new ExerciseOutcome(true, -1, 0) })
            Assert.InRange(MasteryRule.Next(current, outcome), 0.0, 1.0);
    }

    [Fact]
    public void ASeriesOfExercises_MovesTheScoreCoherently()
    {
        // A student who starts from scratch, succeeds three times without help, fails once, then succeeds with hints.
        double? score = null;
        var history = new List<double>();
        foreach (var outcome in new[] { Success(), Success(), Success(), Failure, Success(2, 2) })
        {
            score = MasteryRule.Next(score, outcome);
            history.Add(score.Value);
        }

        Assert.True(history[0] < history[1] && history[1] < history[2]); // steady progress
        Assert.True(history[2] > 0.9);                                    // well mastered after three clean successes
        Assert.True(history[3] < history[2] && history[3] > 0.6);         // one failure dents it without erasing it
        Assert.True(history[4] > history[3]);                             // a helped success gets some back
    }
}
