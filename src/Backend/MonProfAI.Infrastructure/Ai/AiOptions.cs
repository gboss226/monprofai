namespace MonProfAI.Infrastructure.Ai;

/// <summary>Settings of the "Ai" configuration section. The API key is not here: it comes from AI_API_KEY.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Environment variable (or .env entry) that holds the provider's API key.</summary>
    public const string ApiKeyVariable = "AI_API_KEY";

    /// <summary>Provider adapter to use. Only "Anthropic" exists today.</summary>
    public string Provider { get; set; } = "Anthropic";

    public string Model { get; set; } = "claude-opus-5-5";

    /// <summary>Hard cap on the length of one answer, which also caps its cost.</summary>
    public int MaxTokens { get; set; } = 8000;

    /// <summary>Time allowed for one call to the provider, retries included.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    public RateLimitOptions RateLimit { get; set; } = new();

    public sealed class RateLimitOptions
    {
        /// <summary>Questions one student may ask per window.</summary>
        public int PermitLimit { get; set; } = 30;

        public int WindowMinutes { get; set; } = 60;
    }
}
