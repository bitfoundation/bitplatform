namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.DataTable;

public partial class BitAiDataTableDemo
{
    private const string ModelsSampleCode = """"
private readonly JsonElement models = JsonDocument.Parse("""
    [
      { "model": "Demo Large", "contextWindow": 200000, "streaming": true, "price": 15 },
      { "model": "Demo Fast", "contextWindow": 128000, "streaming": true, "price": 1.5 },
      { "model": "Local Small", "contextWindow": 32000, "streaming": false, "price": 0 }
    ]
    """).RootElement;
"""";

    private const string BenchmarksSampleCode = """"
private readonly JsonElement benchmarks = JsonDocument.Parse("""
    [
      { "model": "Demo Large", "contextWindow": 200000, "maxOutput": 32000, "inputPrice": 15, "outputPrice": 75, "latencyMs": 820, "tokensPerSecond": 64, "vision": true, "tools": true, "jsonMode": true, "region": "eu-west", "released": "2026-03-01" },
      { "model": "Demo Fast", "contextWindow": 128000, "maxOutput": 16000, "inputPrice": 1.5, "outputPrice": 6, "latencyMs": 240, "tokensPerSecond": 180, "vision": true, "tools": true, "jsonMode": true, "region": "us-east", "released": "2026-06-15" },
      { "model": "Local Small", "contextWindow": 32000, "maxOutput": 4000, "inputPrice": 0, "outputPrice": 0, "latencyMs": 120, "tokensPerSecond": 95, "vision": false, "tools": false, "jsonMode": true, "region": "on-device", "released": "2025-11-30" }
    ]
    """).RootElement;
"""";

    private readonly string example1RazorCode = """
<BitAiDataTable Rows="models" />
""";
    private readonly string example1CsharpCode = ModelsSampleCode;

    private readonly string example2RazorCode = """
<BitAiDataTable Rows="mixed" />
""";
    private readonly string example2CsharpCode = """"
private readonly JsonElement mixed = JsonDocument.Parse("""
    [
      { "id": 1, "name": "Ada", "active": true, "lastLogin": "2026-10-20" },
      { "id": 2, "name": "Grace", "active": false, "tags": ["compilers", "navy"] },
      { "id": 3, "name": "Linus", "lastLogin": null, "owner": { "team": "kernel" }, "time_zone": "UTC+2" }
    ]
    """).RootElement;
"""";

    private readonly string example3RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskForOptionsAsync" Disabled="optionsSession.IsGenerating">Compare the options</BitButton>

@if (OptionsPart?.Value is { } optionRows)
{
    <BitAiDataTable Rows="optionRows" />
}
""";
    private readonly string example3CsharpCode = """
// A scripted transport that streams four rows of JSON a few characters at a time (see the next tab).
private readonly BitAiChatSession optionsSession = new(AiDemoContentTransports.Structured());
private BitAiRenderScheduler? renderScheduler;

protected override void OnInitialized()
{
    // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
    renderScheduler = new(() => InvokeAsync(StateHasChanged));
    optionsSession.StateChanged += renderScheduler.Request;
}

// The structured part of the latest answer, whose Value is a lenient parse of the JSON received so far.
private BitAiStructuredDataPart? OptionsPart =>
    optionsSession.Conversation.LastMessage?.PartsOf<BitAiStructuredDataPart>().FirstOrDefault();

private Task AskForOptionsAsync() => optionsSession.SubmitAsync("Compare the options");

public async ValueTask DisposeAsync()
{
    if (renderScheduler is not null) await renderScheduler.DisposeAsync();

    await optionsSession.DisposeAsync();
}
""";
    private const string example3TransportCode = """"
public static IBitAiTransport Structured() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
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

        // Fourteen characters at a time: the JSON is incomplete until the last slice.
        foreach (var slice in AiScript.Slice(json, 14))
        {
            await AiScript.Pause(70, token);
            await yielder(new BitAiStructuredDataDeltaEvent(slice) { SchemaName = "options" });
        }

        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example3CodeFiles =
    [
        new("AiDemoContentTransports.cs", example3TransportCode),
    ];

    private readonly string example4RazorCode = """
<BitAiDataTable Rows="benchmarks" AriaLabel="Benchmark results" />
""";
    private readonly string example4CsharpCode = BenchmarksSampleCode;

    private readonly string example5RazorCode = """
<BitParams Parameters="dataTableParams">
    <BitAiDataTable Rows="models" />

    <BitAiDataTable Rows="models" Styles="@(new() { Cell = "font-style: italic" })" />
</BitParams>
""";
    private readonly string example5CsharpCode = ModelsSampleCode + """

private readonly BitAiDataTableParams[] dataTableParams =
[
    new() { Styles = new() { HeaderCell = "text-transform: uppercase", Row = "font-variant-numeric: tabular-nums" } },
];
""";

    private readonly string example6RazorCode = """
<style>
    .custom-table {
        border: none;
        border-radius: 0;
        box-shadow: 0 0.25rem 1rem #0003;
    }

    .custom-table-row:nth-child(even) {
        background-color: #8a2be21a;
    }
</style>

<BitAiDataTable Rows="models"
                Style="border-radius: 1rem; border-color: #8a2be2"
                Styles="@(new() { HeaderCell = "background-color: #8a2be2; color: white", Cell = "font-style: italic" })" />

<BitAiDataTable Rows="models" Classes="@(new() { Root = "custom-table", Row = "custom-table-row" })" />

<div style="--bit-AiDataTable-header-background: #d2691e; --bit-AiDataTable-header-color: white; --bit-AiDataTable-cell-padding: 0.25rem 0.5rem; --bit-AiDataTable-divider-color: #d2691e">
    <BitAiDataTable Rows="models" />
</div>
""";
    private readonly string example6CsharpCode = ModelsSampleCode;

    private readonly string example7RazorCode = """
<BitAiDataTable Dir="BitDir.Rtl" Rows="persianModels" />

<div dir="rtl">
    <BitAiDataTable Rows="persianModels" />
</div>
""";
    private readonly string example7CsharpCode = """"
private readonly JsonElement persianModels = JsonDocument.Parse("""
    [
      { "مدل": "دمو بزرگ", "پنجره زمینه": 200000, "قیمت": 15 },
      { "مدل": "دمو سریع", "پنجره زمینه": 128000, "قیمت": 1.5 }
    ]
    """).RootElement;
"""";
}
