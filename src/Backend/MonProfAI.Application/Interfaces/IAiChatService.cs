namespace MonProfAI.Application.Interfaces;

/// <summary>
/// Provider-agnostic access to a chat model. Implementations live in Infrastructure (one per provider)
/// and must translate every provider failure into an <see cref="AiServiceException"/>.
/// </summary>
public interface IAiChatService
{
    /// <summary>Streams the model's answer as text chunks, in order.</summary>
    /// <exception cref="AiServiceException">The provider failed, timed out, refused or answered badly.</exception>
    IAsyncEnumerable<string> StreamReplyAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}

public enum AiChatRole
{
    User,
    Assistant,
}

/// <param name="Images">Pictures sent with the text (student messages only), e.g. a photo of an exercise.</param>
public sealed record AiChatMessage(AiChatRole Role, string Content, IReadOnlyList<AiImage>? Images = null);

/// <param name="MediaType">"image/jpeg", "image/png", "image/webp" or "image/gif".</param>
public sealed record AiImage(string MediaType, byte[] Data)
{
    public static readonly IReadOnlySet<string> SupportedMediaTypes =
        new HashSet<string> { "image/jpeg", "image/png", "image/webp", "image/gif" };
}

/// <param name="SystemPrompt">Instructions for the model, never shown to the student.</param>
/// <param name="Messages">Conversation so far, oldest first, ending with the student's new message.</param>
public sealed record AiChatRequest(string SystemPrompt, IReadOnlyList<AiChatMessage> Messages);

public enum AiErrorKind
{
    /// <summary>No API key or an invalid one: a server configuration problem.</summary>
    NotConfigured,
    Timeout,
    /// <summary>The provider's rate limit or spending quota is exhausted.</summary>
    QuotaExceeded,
    /// <summary>Provider down, overloaded, or unreachable.</summary>
    Unavailable,
    /// <summary>The provider answered, but with nothing usable (malformed stream, empty or cut-off answer).</summary>
    InvalidResponse,
    /// <summary>The model declined to answer.</summary>
    Refused,
}

/// <summary>A failure of the AI provider, with a message safe to show to a student.</summary>
public sealed class AiServiceException : Exception
{
    public AiServiceException(AiErrorKind kind, Exception? innerException = null)
        : base($"AI provider error: {kind}", innerException)
    {
        Kind = kind;
    }

    public AiErrorKind Kind { get; }

    /// <summary>French message for the student. Never contains provider details.</summary>
    public string StudentMessage => Kind switch
    {
        AiErrorKind.Timeout =>
            "Ton professeur IA met trop de temps à répondre. Réessaie dans quelques instants.",
        AiErrorKind.QuotaExceeded =>
            "Ton professeur IA reçoit trop de questions en ce moment. Réessaie dans quelques minutes.",
        AiErrorKind.InvalidResponse =>
            "La réponse de ton professeur IA n'a pas pu être lue. Repose ta question.",
        AiErrorKind.Refused =>
            "Ton professeur IA ne peut pas répondre à cette question. Essaie de la formuler autrement.",
        _ =>
            "Ton professeur IA n'est pas disponible pour le moment. Réessaie plus tard.",
    };
}
