using System.Net;
using System.Text;

namespace MonProfAI.Tests.Ai;

/// <summary>Stands in for the Anthropic API: answers every request with a canned response, never touching the network.</summary>
public sealed class FakeAnthropicHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    public FakeAnthropicHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _respond = respond;
    }

    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>JSON bodies of the requests, read before the SDK disposes them.</summary>
    public List<string> Bodies { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return await _respond(request, cancellationToken);
    }

    public static FakeAnthropicHandler Returning(HttpStatusCode status, string body, string contentType = "application/json") =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType),
        }));

    public static FakeAnthropicHandler Streaming(params string[] textChunks) =>
        Returning(HttpStatusCode.OK, Sse(textChunks, "end_turn"), "text/event-stream");

    public static string Error(string type, string message = "simulated") =>
        $$$"""{"type":"error","error":{"type":"{{{type}}}","message":"{{{message}}}"}}""";

    /// <summary>A Messages API event stream that writes <paramref name="textChunks"/> then stops with <paramref name="stopReason"/>.</summary>
    public static string Sse(IEnumerable<string> textChunks, string stopReason)
    {
        var sb = new StringBuilder();
        void Event(string name, string data) => sb.Append($"event: {name}\ndata: {data}\n\n");

        Event("message_start",
            """{"type":"message_start","message":{"id":"msg_test","type":"message","role":"assistant","model":"claude-opus-5-5","content":[],"stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":12,"output_tokens":1}}}""");
        Event("content_block_start",
            """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""");
        foreach (var chunk in textChunks)
        {
            var escaped = System.Text.Json.JsonSerializer.Serialize(chunk);
            Event("content_block_delta",
                $$$"""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":{{{escaped}}}}}""");
        }
        Event("content_block_stop", """{"type":"content_block_stop","index":0}""");
        Event("message_delta",
            $$$"""{"type":"message_delta","delta":{"stop_reason":"{{{stopReason}}}","stop_sequence":null},"usage":{"output_tokens":7}}""");
        Event("message_stop", """{"type":"message_stop"}""");
        return sb.ToString();
    }
}
