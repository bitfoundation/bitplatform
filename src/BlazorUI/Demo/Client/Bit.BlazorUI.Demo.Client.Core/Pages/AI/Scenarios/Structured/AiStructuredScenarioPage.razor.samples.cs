namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Structured;

public partial class AiStructuredScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask for a comparison"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoContentTransports.Structured());

private readonly string[] suggestions = ["Compare our options"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """"
public static IBitAiTransport Structured() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());

    const string intro = "Four candidates, ranked by how much work each one would be.\n\n";
    await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

    const string json = """
        [
          {"option":"Server-side rendering","effort":"Low","risk":"Low","notes":"Fastest to ship"},
          {"option":"WebAssembly","effort":"Medium","risk":"Low","notes":"Best offline story"},
          {"option":"Hybrid per page","effort":"Medium","risk":"Medium","notes":"Two mental models"},
          {"option":"Rewrite the front end","effort":"High","risk":"High","notes":"Not this quarter"}
        ]
        """;

    // Fourteen characters at a time: invalid JSON until the very last slice.
    foreach (var slice in AiScript.Slice(json, 14))
    {
        await AiScript.Pause(70, token);
        await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "options" });
    }

    await yielder(new BitAiMessageCompletedEvent());
}));
"""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoContentTransports.cs", example1TransportCode),
    ];
}
