namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.CustomTool;

public partial class AiCustomToolScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Search the documentation"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """"
private BitAiChatSession? session;

private readonly string[] suggestions = ["Find the docs on render modes"];

[Inject] private IBitAiToolRegistry Tools { get; set; } = default!;

protected override void OnInitialized() => session = new BitAiChatSession(
    AiDemoToolTransports.CallsTool(
        "search_docs",
        """{"query":"render modes"}""",
        "Two passages matched. The first covers choosing a mode per component; the second covers what happens when a circuit is lost."),
    tools: Tools);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
"""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    ai.AddTool(BitAiTool.Create(
        "search_docs",
        "Search the product documentation and return matching passages.",
        AiDemoJsonContext.Default.SearchArgs,
        async (args, invocation, token) =>
        {
            // Reported to the card while the tool runs.
            invocation.Progress.Report(new BitAiToolProgress(0.2, "Querying the index"));
            await Task.Delay(500, token);
            invocation.Progress.Report(new BitAiToolProgress(0.7, "Ranking results"));
            await Task.Delay(500, token);

            var source = new BitAiCitationSource("docs-render-modes", "Render modes")
            {
                Kind = BitAiSourceKind.Document,
                SiteName = "Product documentation",
                Url = "https://example.com/docs/render-modes",
            };

            // The value the card reads, the text the model reads, and the source the reply lists.
            return BitAiToolOutcome.Success(SearchHits(args.Query), AiDemoJsonContext.Default.SearchResult) with
            {
                Text = $"2 passages matched “{args.Query}”.",
                Citations = [new BitAiCitation(BitAiId.New("cit"), source)],
            };
        },
        definition => definition with { Title = "Search docs", Icon = BitIconName.Search }));

    // Wherever search_docs is called, SearchResultCard draws it instead of the default card.
    ai.ConfigureRenderers(renderers => renderers.RegisterTool<SearchResultCard>("search_docs"));
});

static SearchResult SearchHits(string query) => new(query,
[
    new SearchHit("Render modes", 0.94),
    new SearchHit("Circuit lifetime", 0.81),
]);
""";

    private const string example1JsonContextCode = """
public sealed record SearchArgs(string Query);

public sealed record SearchResult(string Query, IReadOnlyList<SearchHit> Hits);

public sealed record SearchHit(string Title, double Score);

// The web defaults write "hits", "title" and "score", which is what SearchResultCard reads back.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SearchArgs))]
[JsonSerializable(typeof(SearchResult))]
public partial class AiDemoJsonContext : JsonSerializerContext
{
}
""";

    private const string example1TransportCode = """
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport CallsTool(string toolName, string arguments, string answer, string? declinedAnswer = null) =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        var outcome = AiScript.ToolOutcomeThisTurn(request);

        await yielder(new BitAiMessageStartedEvent());

        // First round trip: ask for the tool, streaming its arguments, and stop; the session runs it.
        if (outcome is null)
        {
            await yielder(new BitAiStatusEvent("Deciding what to look up"));
            await AiScript.Pause(500, token);

            var callId = BitAiId.NewToolCallId();
            await yielder(new BitAiToolCallStartedEvent(callId, toolName));

            foreach (var slice in AiScript.Slice(arguments, 6))
            {
                await AiScript.Pause(40, token);
                await yielder(new BitAiToolCallDeltaEvent(callId, slice));
            }

            await yielder(new BitAiToolCallCompletedEvent(callId));
            await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
            return;
        }

        // Second round trip: the result is on the request.
        var reply = outcome.IsSuccess ? answer : declinedAnswer ?? $"I did not run {toolName}, so nothing was changed.";

        await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, reply));
        await yielder(new BitAiMessageCompletedEvent());
    }));
""";

    private const string example1CardCode = """
@using System.Globalization
@using System.Text.Json

<div class="search-card">
    <div class="search-card-head">
        <BitIcon IconName="@BitIconName.Search" Size="BitSize.Small" />
        <span class="search-card-title">
            @(Call.Status is BitAiToolCallStatus.Completed ? "Searched the documentation" : "Searching the documentation")
        </span>
        @if (Call.Duration is { } duration)
        {
            <BitTag Size="BitSize.Small" Variant="BitVariant.Outline" Color="BitColor.SecondaryForeground" Text="@($"{duration.TotalMilliseconds:F0}ms")" />
        }
    </div>

    <div class="search-card-body">
        @if (Query is { Length: > 0 } query)
        {
            <div class="search-card-query">@query</div>
        }

        @if (Hits is { } hits)
        {
            <ul class="search-card-hits">
                @foreach (var hit in hits.EnumerateArray())
                {
                    <li>
                        <span>@(hit.TryGetProperty("title", out var title) ? title.GetString() : null)</span>
                        <BitTag Size="BitSize.Small" Variant="BitVariant.Text" Color="BitColor.Info"
                                Text="@(hit.TryGetProperty("score", out var score) && score.TryGetDouble(out var value) ? value.ToString("0%", CultureInfo.InvariantCulture) : null)" />
                    </li>
                }
            </ul>
        }
        else if (Call.Progress is { } progress)
        {
            <BitProgress Percent="@((progress.Fraction ?? 0) * 100)" Description="@progress.Message" AriaLabel="Search progress" />
        }
    </div>
</div>

@code {
    // Handed over by the registry, which renders the component with the call as its Call parameter.
    [Parameter, EditorRequired] public BitAiToolCallPart Call { get; set; } = default!;

    // Checked rather than assumed: a custom renderer is handed data a model produced.
    private JsonElement? Hits => Call.Result?.Value is { } value
        && value.ValueKind is JsonValueKind.Object
        && value.TryGetProperty("hits", out var hits)
        && hits.ValueKind is JsonValueKind.Array
            ? hits
            : null;

    private string? Query => Call.DisplayArguments is { } args
        && args.ValueKind is JsonValueKind.Object
        && args.TryGetProperty("query", out var value)
            ? value.GetString()
            : null;
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("SearchResultCard.razor", example1CardCode),
        new("AiDemoJsonContext.cs", example1JsonContextCode),
        new("AiDemoToolTransports.cs", example1TransportCode),
    ];
}
