using MonProfAI.Api.Ai;
using Xunit;

namespace MonProfAI.Tests.Ai;

public class DotEnvFileTests
{
    [Theory]
    [InlineData("AI_API_KEY=abc", "abc")]
    [InlineData("AI_API_KEY = \"abc def\"", "abc def")]
    [InlineData("export AI_API_KEY='abc'", "abc")]
    [InlineData("AI_API_KEY=", null)]
    [InlineData("# AI_API_KEY=commented", null)]
    [InlineData("OTHER_AI_API_KEY=nope", null)]
    public void ParseValue_ReadsOnlyTheRequestedKey(string line, string? expected)
    {
        Assert.Equal(expected, DotEnvFile.ParseValue(new[] { "DB_HOST=postgres", line }, "AI_API_KEY"));
    }
}
