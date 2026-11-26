using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Tutor;
using MonProfAI.Domain.Entities;
using Xunit;

namespace MonProfAI.Tests.UseCases.Tutor;

public class ToModelMessagesTests
{
    private static ConversationMessage Msg(MessageRole role, string content) => new() { Role = role, Content = content };

    private static string[] Shape(List<AiChatMessage> messages) =>
        messages.Select(m => $"{m.Role}:{m.Content}").ToArray();

    [Fact]
    public void AppendsTheNewQuestionAfterTheHistory()
    {
        var result = AskTutorQueryHandler.ToModelMessages(
            new[] { Msg(MessageRole.User, "Q1"), Msg(MessageRole.Assistant, "R1") }, "Q2");

        Assert.Equal(new[] { "User:Q1", "Assistant:R1", "User:Q2" }, Shape(result));
    }

    [Fact]
    public void MergesAQuestionLeftWithoutAnswer()
    {
        // The provider failed on Q1: no answer was saved.
        var result = AskTutorQueryHandler.ToModelMessages(new[] { Msg(MessageRole.User, "Q1") }, "Q2");

        Assert.Equal(new[] { "User:Q1\n\nQ2" }, Shape(result));
    }

    [Fact]
    public void DropsAnAnswerWhoseQuestionFellOutOfTheWindow()
    {
        // The context window started on an answer: the model needs the student to speak first.
        var result = AskTutorQueryHandler.ToModelMessages(
            new[] { Msg(MessageRole.Assistant, "R0"), Msg(MessageRole.User, "Q1"), Msg(MessageRole.Assistant, "R1") }, "Q2");

        Assert.Equal(new[] { "User:Q1", "Assistant:R1", "User:Q2" }, Shape(result));
    }
}
