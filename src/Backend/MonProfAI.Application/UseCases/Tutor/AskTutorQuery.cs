using MediatR;

namespace MonProfAI.Application.UseCases.Tutor;

/// <summary>A student's question to the AI tutor. Streams the answer as text chunks.</summary>
/// <param name="History">Previous turns of the conversation, oldest first. The client keeps it; nothing is stored.</param>
/// <param name="NotionId">Notion the session is about, chosen by the student. Optional.</param>
public sealed record AskTutorQuery(
    Guid UserId, string Message, IReadOnlyList<TutorTurn>? History = null, Guid? NotionId = null)
    : IStreamRequest<string>;

/// <param name="Role">"user" or "assistant".</param>
public sealed record TutorTurn(string Role, string Content);
