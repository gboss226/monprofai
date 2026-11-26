using MediatR;

namespace MonProfAI.Application.UseCases.Tutor;

/// <summary>
/// A student's question in one of his conversations (see <c>OpenConversationCommand</c>). Streams the answer as text
/// chunks. The question and the answer are saved, and the latest messages are replayed to the model as context.
/// </summary>
/// <exception cref="Conversations.ConversationNotFoundException">The conversation is not one of the student's.</exception>
public sealed record AskTutorQuery(Guid UserId, Guid ConversationId, string Message) : IStreamRequest<string>;

/// <param name="Role">"user" or "assistant".</param>
public sealed record TutorTurn(string Role, string Content);
