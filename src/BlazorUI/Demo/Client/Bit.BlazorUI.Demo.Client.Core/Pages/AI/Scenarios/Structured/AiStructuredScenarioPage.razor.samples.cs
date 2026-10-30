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
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
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

    private readonly string example2RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask for a comparison"
               Suggestions="suggestions" />
</div>

<aside aria-label="Request options">
    <h4>Request options</h4>
    <BitToggle Label="Ask for JSON matching the schema" @bind-Value="askForJson" @bind-Value:after="ApplyResponseFormat" />
    <p>ResponseFormat: <code>@(askForJson ? "FromSchema(schema, \"options\")" : "null")</code></p>
</aside>
""";

    private readonly string example2CsharpCode = """"
// The shape asked for: a JSON Schema, written by hand here, and the name the part is given.
private static readonly BitAiResponseFormat OptionsFormat = BitAiResponseFormat.FromSchema(AiScript.Json("""
    {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["option", "effort", "risk"],
        "properties": {
          "option": { "type": "string" },
          "effort": { "type": "string", "enum": ["Low", "Medium", "High"] },
          "risk": { "type": "string", "enum": ["Low", "Medium", "High"] },
          "notes": { "type": "string" }
        }
      }
    }
    """), "options");

// The request options go with every request of the session.
private readonly BitAiChatSession session = new(AiDemoContentTransports.StructuredAsRequested(), options: new BitAiChatSessionOptions
{
    RequestOptions = new BitAiRequestOptions { ResponseFormat = OptionsFormat },
});

private bool askForJson = true;

private readonly string[] suggestions = ["Compare our options"];

// The options are an immutable record the session reads at the start of every request: a new one is the change.
private void ApplyResponseFormat() => session.Options = session.Options with
{
    RequestOptions = new BitAiRequestOptions { ResponseFormat = askForJson ? OptionsFormat : null },
};

public ValueTask DisposeAsync() => session.DisposeAsync();
"""";

    private const string example2TransportCode = """"
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport StructuredAsRequested() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    // What the request asks for. A real transport hands it to its provider, which holds the model to it.
    var format = request.Options?.ResponseFormat ?? BitAiResponseFormat.Text;

    await yielder(new BitAiMessageStartedEvent());

    if (format.Kind is BitAiResponseFormatKind.Text)
    {
        const string text = """
            No response format was asked for, so here is the comparison as text.

            | Option | Effort | Risk | Notes |
            | --- | --- | --- | --- |
            | Server-side rendering | Low | Low | Fastest to ship |
            ...
            """;

        await foreach (var evt in AiScript.Write(text, 5, 10, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, text));
        await yielder(new BitAiMessageCompletedEvent());
        return;
    }

    var intro = $"Asked for {(format.Kind is BitAiResponseFormatKind.JsonSchema ? "JSON matching a schema" : "JSON")}, so here are the four candidates as data.\n\n";
    await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

    const string json = """
        [
          {"option":"Server-side rendering","effort":"Low","risk":"Low","notes":"Fastest to ship"},
          ...
        ]
        """;

    // Named after the requested schema, which is what picks a renderer registered for it.
    foreach (var slice in AiScript.Slice(json, 14))
    {
        await AiScript.Pause(70, token);
        await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = format.Name });
    }

    await yielder(AiScript.Usage(request, json));
    await yielder(new BitAiMessageCompletedEvent());
}));
"""";

    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("AiDemoContentTransports.cs", example2TransportCode),
    ];
}
