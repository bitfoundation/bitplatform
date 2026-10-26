namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Reasoning;

public partial class AiReasoningScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask something with a trade-off"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoTransports.Reasoning());

private readonly string[] suggestions = ["What does a round trip actually cost?"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """"
public static IBitAiTransport Reasoning() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());

    // Reasoning first: a word at a time, as BitAiReasoningDeltaEvents.
    const string thinking =
        "The user is asking about the cost of a round trip. I should separate the fixed cost of " +
        "the connection from the per-token cost, and give a number rather than an adjective.";

    // A word every 70 ms: about two seconds of thinking, long enough to watch the panel stream open before it
    // collapses into "Thought for ...".
    await foreach (var evt in AiScript.Think(thinking, 70, token)) await yielder(evt);
    await AiScript.Pause(250, token);

    // Then the answer, as ordinary text deltas.
    const string answer = """
        Two costs are worth separating.

        **Connection setup** is roughly constant: ...
        """;

    await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
    await yielder(AiScript.Usage(request, answer));
    await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.Stop));
}));

// AiScript.Think: one reasoning delta per word.
public static async IAsyncEnumerable<BitAiEvent> Think(string text, int delayMs = 10,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    foreach (var word in text.Split(' '))
    {
        await Task.Delay(delayMs, cancellationToken);
        yield return new BitAiReasoningDeltaEvent(word + " ");
    }
}
"""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoTransports.cs", example1TransportCode),
    ];
}
