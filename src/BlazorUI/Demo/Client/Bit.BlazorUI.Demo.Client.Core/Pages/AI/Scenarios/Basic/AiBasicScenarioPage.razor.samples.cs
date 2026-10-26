namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Basic;

public partial class AiBasicScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask me anything"
               EmptyBody="This demo echoes what you send, a few characters at a time."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private BitAiChatSession? session;

private readonly string[] suggestions =
[
    "What can these components do?",
    "Show me streaming",
    "Explain render modes",
];

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

// A session over the transport registered with AddBitBlazorUIAIServices.
protected override void OnInitialized() => session = SessionFactory.Create();

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());
});
""";

    private const string example1TransportCode = """
// The smallest useful transport: it streams the prompt back. A real one wraps a provider's SDK and
// turns its stream into the same events.
public static IBitAiTransport Echo() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    var said = AiScript.LastUserMessage(request);
    var text = said?.GetText() ?? string.Empty;
    var files = said?.Attachments.Count ?? 0;

    // A message can carry files and no text at all, and an echo of nothing would read as no answer.
    var reply = text.Length > 0 ? $"You said: {text}"
              : files == 1 ? "You sent 1 file."
              : $"You sent {files} files.";

    await yielder(new BitAiMessageStartedEvent());
    await foreach (var evt in AiScript.Write(reply, 3, 12, token)) await yielder(evt);
    await yielder(AiScript.Usage(request, reply));
    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private const string example1ScriptCode = """
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

// The demo's helpers, which every scripted transport of the AI scenarios is written with: a transport built from a
// delegate, and the few steps a script repeats. An app's own transport implements IBitAiTransport directly instead
// (see EchoTransport.cs).
public sealed class AiScriptedTransport(Func<BitAiRequest, CancellationToken, IAsyncEnumerable<BitAiEvent>> script)
    : IBitAiTransport
{
    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default) =>
        script(request, cancellationToken);
}

public static class AiScript
{
    // Streams text a few characters at a time, the way a model does.
    public static async IAsyncEnumerable<BitAiEvent> Write(string text, int chunk = 4, int delayMs = 14,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < text.Length; i += chunk)
        {
            await Task.Delay(delayMs, cancellationToken);
            yield return new BitAiTextDeltaEvent(text.Substring(i, Math.Min(chunk, text.Length - i)));
        }
    }

    // Streams reasoning, a word at a time.
    public static async IAsyncEnumerable<BitAiEvent> Think(string text, int delayMs = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in text.Split(' '))
        {
            await Task.Delay(delayMs, cancellationToken);
            yield return new BitAiReasoningDeltaEvent(word + " ");
        }
    }

    public static Task Pause(int milliseconds, CancellationToken cancellationToken) => Task.Delay(milliseconds, cancellationToken);

    public static IBitAiMessage? LastUserMessage(BitAiRequest request) => request.Messages.LastOrDefault(m => m.Role.IsUser);

    public static string LastUserText(BitAiRequest request) => LastUserMessage(request)?.GetText() ?? string.Empty;

    // Whether a tool returned since the latest user message: the session ran it, and the model should now answer.
    public static bool HasToolResultThisTurn(BitAiRequest request) => ToolOutcomeThisTurn(request) is not null;

    // The outcome of the latest tool call that returned since the latest user message. A call the user declined comes
    // back as a failure.
    public static BitAiToolOutcome? ToolOutcomeThisTurn(BitAiRequest request)
    {
        for (var i = request.Messages.Count - 1; i >= 0; i--)
        {
            var message = request.Messages[i];
            if (message.Role.IsUser) return null;
            if (message.Parts.OfType<BitAiToolCallPart>().LastOrDefault(c => c.Result is not null) is { } call) return call.Result;
        }

        return null;
    }

    // Plausible token usage: about four characters a token, priced at 3 and 15 USD a million.
    public static BitAiUsageUpdatedEvent Usage(BitAiRequest request, string output)
    {
        var input = request.Messages.Sum(m => m.GetText().Length) / 4;
        var generated = output.Length / 4;

        return new BitAiUsageUpdatedEvent(new BitAiTokenUsage
        {
            InputTokens = input,
            OutputTokens = generated,
            Cost = (input * 3m + generated * 15m) / 1_000_000m,
            Currency = "USD",
        });
    }

    // Cuts a long payload into slices, the way a model streams it.
    public static IEnumerable<string> Slice(string value, int size)
    {
        for (var i = 0; i < value.Length; i += size) yield return value.Substring(i, Math.Min(size, value.Length - i));
    }

    // A JSON literal as a value that outlives its document.
    public static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    // Turns a script that awaits a callback per event into an event stream, so a script reads top to bottom.
    public static async IAsyncEnumerable<BitAiEvent> Stream([EnumeratorCancellation] CancellationToken token,
        Func<Func<BitAiEvent, Task>, Task> body)
    {
        var channel = Channel.CreateUnbounded<BitAiEvent>();

        var producer = Task.Run(async () =>
        {
            try
            {
                await body(evt => channel.Writer.WriteAsync(evt, token).AsTask());
                channel.Writer.TryComplete();
            }
            catch (Exception exception)
            {
                channel.Writer.TryComplete(exception);
            }
        }, token);

        await foreach (var evt in channel.Reader.ReadAllAsync(token)) yield return evt;
        await producer;
    }
}
""";

    private const string example1EchoTransportCode = """
using System.Runtime.CompilerServices;

// The same echo as a class of its own, without the demo's helpers - the shape of the transport an app writes, around
// its provider's SDK instead of a loop. Register it with ai.UseTransport<EchoTransport>().
public sealed class EchoTransport : IBitAiTransport
{
    public async IAsyncEnumerable<BitAiEvent> StreamAsync(
        BitAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // The whole conversation is in request.Messages, oldest first; the echo only reads the newest user message.
        var said = request.Messages.LastOrDefault(m => m.Role.IsUser);
        var reply = $"You said: {said?.GetText()}";

        yield return new BitAiMessageStartedEvent();

        // Honouring the token is what makes the stop button stop the stream.
        for (var i = 0; i < reply.Length; i += 3)
        {
            await Task.Delay(12, cancellationToken);
            yield return new BitAiTextDeltaEvent(reply.Substring(i, Math.Min(3, reply.Length - i)));
        }

        yield return new BitAiUsageUpdatedEvent(new BitAiTokenUsage
        {
            InputTokens = request.Messages.Sum(m => m.GetText().Length) / 4,
            OutputTokens = reply.Length / 4,
        });

        yield return new BitAiMessageCompletedEvent();
    }
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoTransports.cs", example1TransportCode),
        new("AiScriptedTransport.cs", example1ScriptCode),
        new("EchoTransport.cs", example1EchoTransportCode),
    ];
}
