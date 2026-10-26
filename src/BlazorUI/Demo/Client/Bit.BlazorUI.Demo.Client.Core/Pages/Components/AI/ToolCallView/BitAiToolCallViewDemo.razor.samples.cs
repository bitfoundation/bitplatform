namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ToolCallView;

public partial class BitAiToolCallViewDemo
{
    private readonly string example1RazorCode = """
<BitAiToolCallView Call="weatherCall" />

<BitAiOptionsProvider UiOptions="expandedOptions">
    <BitAiToolCallView Call="weatherCall" />
</BitAiOptionsProvider>
""";
    private readonly string example1CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiToolCallPart weatherCall = CreateWeatherCall();
private readonly BitAiUiOptions expandedOptions = BitAiUiOptions.Default with { CollapseToolCalls = false };

private static BitAiToolCallPart CreateWeatherCall()
{
    var call = new BitAiToolCallPart("call_weather", "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Oslo"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Success(Json("""{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""")) with
    {
        Duration = TimeSpan.FromMilliseconds(700),
    });

    return call;
}
"""";

    private readonly string example2RazorCode = """
<BitAiToolCallView Call="unknownToolCall" />

<BitAiToolCallView Call="iconToolCall" />

<BitAiToolCallView Call="registeredToolCall" />
""";
    private readonly string example2CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

// A tool nobody defined: the card shows its raw name, under the wrench.
private readonly BitAiToolCallPart unknownToolCall = CreateCompletedCall("get_stock_price", null,
    """{"symbol":"MSFT"}""", "MSFT: 431.20 USD");

// A definition of the call's own, which names an icon.
private readonly BitAiToolCallPart iconToolCall = CreateCompletedCall("book_meeting",
    new BitAiToolDefinition("book_meeting") { Title = "Book a meeting", Icon = BitIconName.Calendar },
    """{"with":"Ana","on":"Tuesday 10:00"}""", "Booked with Ana for Tuesday at 10:00.");

// No definition on the call: the card reads the one registered for get_weather in Program.cs -
// ai.AddTool(BitAiTool.Create("get_weather", ..., definition => definition with { Title = "Weather", Icon = BitIconName.Globe }))
private readonly BitAiToolCallPart registeredToolCall = CreateCompletedCall("get_weather", null,
    """{"city":"Lisbon"}""", "Lisbon: 17 degrees, clear");

private static BitAiToolCallPart CreateCompletedCall(string toolName, BitAiToolDefinition? definition, string arguments, string result)
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName) { Definition = definition };

    call.CompleteArguments(Json(arguments));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.FromText(result) with { Duration = TimeSpan.FromMilliseconds(400) });

    return call;
}
"""";

    private readonly string example3RazorCode = """
<BitButton OnClick="RunIndexAsync" Disabled="isIndexing">Run the tool</BitButton>

<BitAiOptionsProvider UiOptions="expandedOptions">
    @if (indexCall is not null)
    {
        <BitAiToolCallView @key="indexCall" Call="indexCall" />
    }
</BitAiOptionsProvider>
""";
    private readonly string example3CsharpCode = """"
private BitAiToolCallPart? indexCall;
private bool isIndexing;
private readonly BitAiUiOptions expandedOptions = BitAiUiOptions.Default with { CollapseToolCalls = false };

private async Task RunIndexAsync()
{
    isIndexing = true;
    indexCall = new BitAiToolCallPart(BitAiId.NewToolCallId(), "index_docs")
    {
        Definition = new BitAiToolDefinition("index_docs") { Title = "Index the docs" },
    };
    StateHasChanged();

    // The model writes the arguments a few characters at a time; the card shows them as they arrive.
    foreach (var slice in new[] { """{"fol""", """der":"do""", """cs/","rec""", """ursive":""", "true}" })
    {
        await Task.Delay(250);
        indexCall.AppendArguments(slice);
    }

    indexCall.CompleteArguments();

    await Task.Delay(400);
    indexCall.SetStatus(BitAiToolCallStatus.Running);
    indexCall.SetProgress(new BitAiToolProgress(null, "Listing the files"));

    await Task.Delay(1200);
    indexCall.SetProgress(new BitAiToolProgress(0.4, "Reading 12 of 30 files"));

    await Task.Delay(1200);
    indexCall.SetProgress(new BitAiToolProgress(0.8, "Reading 24 of 30 files"));

    await Task.Delay(1200);
    indexCall.SetResult(BitAiToolOutcome.FromText("Indexed 30 files of docs/ (412 passages)."));

    isIndexing = false;
}
"""";

    private readonly string example4RazorCode = """
<BitAiToolCallView Call="failedCall" />

<BitAiToolCallView Call="declinedCall" />

<BitAiToolCallView Call="cancelledCall" />
""";
    private readonly string example4CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiToolCallPart failedCall = CreateFailedCall();
private readonly BitAiToolCallPart declinedCall = CreateDeclinedCall();

private static BitAiToolCallPart CreateFailedCall()
{
    var call = new BitAiToolCallPart("call_atlantis", "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Atlantis"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Failure(new BitAiError("No weather station reports for Atlantis.")
    {
        Code = "unknown_city",
        Kind = BitAiErrorKind.Tool,
    }) with
    {
        Duration = TimeSpan.FromMilliseconds(1200),
    });

    return call;
}

// What a session records for a call the user declined.
private static BitAiToolCallPart CreateDeclinedCall()
{
    var call = new BitAiToolCallPart("call_delete", "delete_records")
    {
        Definition = new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive },
    };

    call.CompleteArguments(Json("""{"filter":"status = 'archived'"}"""));
    call.SetStatus(BitAiToolCallStatus.AwaitingApproval);
    call.SetApproval(BitAiApprovalDecision.Rejected);
    call.SetResult(BitAiToolOutcome.Failure(new BitAiError("The user declined this tool call.")
    {
        Code = "rejected",
        Kind = BitAiErrorKind.Tool,
    }));
    call.SetStatus(BitAiToolCallStatus.Rejected);

    return call;
}

// What a session records for a call that was running when the user stopped the turn.
private readonly BitAiToolCallPart cancelledCall = CreateCancelledCall();

private static BitAiToolCallPart CreateCancelledCall()
{
    var call = new BitAiToolCallPart("call_export", "export_report")
    {
        Definition = new BitAiToolDefinition("export_report") { Title = "Export the report" },
    };

    call.CompleteArguments(Json("""{"format":"pdf","pages":"all"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetStatus(BitAiToolCallStatus.Cancelled);

    return call;
}
"""";

    private readonly string example5RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskToDeleteAsync" Disabled="approvalSession.IsGenerating">Delete the archived records</BitButton>

@if (approvalSession.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant) is { } approvalMessage)
{
    foreach (var call in approvalMessage.PartsOf<BitAiToolCallPart>())
    {
        <BitAiToolCallView @key="call.PartId" Call="call" Message="approvalMessage" Session="approvalSession" />
    }
}
""";
    private readonly string example5CsharpCode = """"
[Inject] private IBitAiToolRegistry ToolRegistry { get; set; } = default!;

private BitAiRenderScheduler sessionRenders = default!;
private BitAiChatSession approvalSession = default!;

protected override void OnInitialized()
{
    // The session raises StateChanged on every event it applies; the scheduler turns those into at most one render
    // of the page per frame interval. The cards follow their own calls in between.
    sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

    // A scripted model that asks for delete_records, then answers once the tool has returned. The tool itself is the
    // one registered with AddBitBlazorUIAIServices, declared Destructive; a policy of its own per session keeps an
    // "Always allow" to this page.
    approvalSession = new BitAiChatSession(
        AiDemoToolTransports.CallsTool(
            "delete_records",
            """{"filter":"status = 'archived'"}""",
            "Deleted 128 archived records."),
        tools: ToolRegistry,
        approvalPolicy: new BitAiDefaultToolApprovalPolicy());
    approvalSession.StateChanged += sessionRenders.Request;
}

private Task AskToDeleteAsync() => approvalSession.SubmitAsync("Delete the archived records");

public async ValueTask DisposeAsync()
{
    await approvalSession.DisposeAsync();
    await sessionRenders.DisposeAsync();
}
"""";

    private readonly string example6RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="SearchDocsAsync" Disabled="searchSession.IsGenerating">Search the docs</BitButton>

@if (searchSession.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant) is { } searchMessage)
{
    foreach (var call in searchMessage.PartsOf<BitAiToolCallPart>())
    {
        <BitAiToolCallView @key="call.PartId" Call="call" Message="searchMessage" Session="searchSession" />
    }
}
""";
    private readonly string example6CsharpCode = """"
[Inject] private IBitAiToolRegistry ToolRegistry { get; set; } = default!;

private BitAiRenderScheduler sessionRenders = default!;
private BitAiChatSession searchSession = default!;

protected override void OnInitialized()
{
    sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

    searchSession = new BitAiChatSession(
        AiDemoToolTransports.CallsTool(
            "search_docs",
            """{"query":"render modes"}""",
            "Two passages matched."),
        tools: ToolRegistry);
    searchSession.StateChanged += sessionRenders.Request;
}

private Task SearchDocsAsync() => searchSession.SubmitAsync("Find the docs on render modes");

public async ValueTask DisposeAsync()
{
    await searchSession.DisposeAsync();
    await sessionRenders.DisposeAsync();
}
"""";
    private const string example6ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // The tool the model calls. Its progress reports reach the card (or the renderer) while it runs.
    ai.AddTool(BitAiTool.Create(
        "search_docs",
        "Search the product documentation and return matching passages.",
        AiDemoJsonContext.Default.SearchArgs,
        async (args, invocation, token) =>
        {
            invocation.Progress.Report(new BitAiToolProgress(0.2, "Querying the index"));
            await Task.Delay(500, token);
            invocation.Progress.Report(new BitAiToolProgress(0.7, "Ranking results"));
            await Task.Delay(500, token);

            return BitAiToolOutcome.Success(AiDemoCatalog.SearchHits(args.Query), AiDemoJsonContext.Default.SearchResult) with
            {
                Text = $"2 passages matched “{args.Query}”.",
            };
        },
        definition => definition with { Title = "Search docs", Icon = BitIconName.Search }));

    // Wherever search_docs is called, SearchResultCard draws it instead of the default card. The component is handed
    // the call as its Call parameter; RegisterTool(name, context => builder => ...) takes a delegate instead.
    ai.ConfigureRenderers(renderers => renderers.RegisterTool<SearchResultCard>("search_docs"));
});
""";
    private const string example6CardCode = """
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
                                Text="@(hit.TryGetProperty("score", out var score) && score.TryGetDouble(out var value) ? value.ToString("P0", CultureInfo.InvariantCulture) : null)" />
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
    private readonly DemoCodeFile[] example6CodeFiles =
    [
        new("Program.cs", example6ProgramCode),
        new("SearchResultCard.razor", example6CardCode),
    ];

    private readonly string example7RazorCode = """
<BitAiToolCallView Disabled Call="awaitingCall" />
""";
    private readonly string example7CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiToolCallPart awaitingCall = CreateAwaitingCall();

private static BitAiToolCallPart CreateAwaitingCall()
{
    var call = new BitAiToolCallPart("call_email", "send_email")
    {
        Definition = new BitAiToolDefinition("send_email") { Title = "Send email", Risk = BitAiToolRisk.Write, RequiresApproval = true },
        ApprovalReason = "Send the summary to the 3 people on the thread?",
    };

    call.CompleteArguments(Json("""{"to":["ana@example.com","li@example.com","sam@example.com"],"subject":"Weekly summary"}"""));
    call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

    return call;
}
"""";

    private readonly string example8RazorCode = """
<BitParams Parameters="toolCallViewParams">
    <BitAiToolCallView Call="weatherCall" />
    <BitAiToolCallView Call="failedCall" />
    <BitAiToolCallView Call="weatherCall" Disabled="false" />
</BitParams>
""";
    private readonly string example8CsharpCode = """"
private readonly BitAiToolCallViewParams[] toolCallViewParams =
[
    new() { Disabled = true, Styles = new() { Root = "border-style: dashed", Title = "font-style: italic" } },
];

private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiToolCallPart weatherCall = CreateWeatherCall();
private readonly BitAiToolCallPart failedCall = CreateFailedCall();

private static BitAiToolCallPart CreateWeatherCall()
{
    var call = new BitAiToolCallPart("call_weather", "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Oslo"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Success(Json("""{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""")) with
    {
        Duration = TimeSpan.FromMilliseconds(700),
    });

    return call;
}

private static BitAiToolCallPart CreateFailedCall()
{
    var call = new BitAiToolCallPart("call_atlantis", "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Atlantis"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Failure(new BitAiError("No weather station reports for Atlantis.")
    {
        Code = "unknown_city",
        Kind = BitAiErrorKind.Tool,
    }) with
    {
        Duration = TimeSpan.FromMilliseconds(1200),
    });

    return call;
}
"""";

    private readonly string example9RazorCode = """
<style>
    .custom-tool {
        border-width: 2px;
        border-style: dashed;
    }

    .custom-tool-root {
        border: none;
        border-radius: 0;
        border-inline-start: 4px solid #0b7a75;
    }

    .custom-tool-header {
        color: color-mix(in srgb, #0b7a75 65%, var(--bit-clr-fg-pri));
        letter-spacing: 0.05em;
        text-transform: uppercase;
    }

    .custom-tool-title {
        font-weight: bold;
    }
</style>

<BitAiToolCallView Call="weatherCall" Style="box-shadow: 0 0.25rem 1rem #0003" />

<BitAiToolCallView Call="weatherCall" Class="custom-tool" />

<BitAiToolCallView Call="weatherCall"
                   Styles="@(new() { Header = "padding: 0.75rem 1rem",
                                     Icon = "color: color-mix(in srgb, #0b7a75 65%, var(--bit-clr-fg-pri))",
                                     Title = "font-style: italic",
                                     Status = "color: color-mix(in srgb, #0b7a75 65%, var(--bit-clr-fg-pri)); font-weight: bold" })" />

<BitAiToolCallView Call="weatherCall"
                   Classes="@(new() { Root = "custom-tool-root",
                                      Header = "custom-tool-header",
                                      Title = "custom-tool-title" })" />

<div style="--bit-AiToolCallView-background: #8b5cf61a; --bit-AiToolCallView-border-color: #a78bfa; --bit-AiToolCallView-failed-border-color: #be185d; --bit-AiToolCallView-header-color: color-mix(in srgb, #8b5cf6, var(--bit-clr-fg-pri)); --bit-AiToolCallView-radius: 1rem;">
    <BitAiToolCallView Call="weatherCall" />
    <br />
    <BitAiToolCallView Call="failedCall" />
</div>
""";
    private readonly string example9CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiToolCallPart weatherCall = CreateWeatherCall();
private readonly BitAiToolCallPart failedCall = CreateFailedCall();

private static BitAiToolCallPart CreateWeatherCall()
{
    var call = new BitAiToolCallPart("call_weather", "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Oslo"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Success(Json("""{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""")) with
    {
        Duration = TimeSpan.FromMilliseconds(700),
    });

    return call;
}

private static BitAiToolCallPart CreateFailedCall()
{
    var call = new BitAiToolCallPart("call_atlantis", "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
    };

    call.CompleteArguments(Json("""{"city":"Atlantis"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(BitAiToolOutcome.Failure(new BitAiError("No weather station reports for Atlantis.")
    {
        Code = "unknown_city",
        Kind = BitAiErrorKind.Tool,
    }) with
    {
        Duration = TimeSpan.FromMilliseconds(1200),
    });

    return call;
}
"""";

    private readonly string example10RazorCode = """
<BitAiOptionsProvider UiOptions="persianOptions">
    <BitAiToolCallView Dir="BitDir.Rtl" Call="rtlCall" />
    <BitAiToolCallView Dir="BitDir.Rtl" Call="rtlFailedCall" />
</BitAiOptionsProvider>
""";
    private readonly string example10CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
private readonly BitAiToolCallPart rtlCall = CreateRtlCall(succeeded: true);
private readonly BitAiToolCallPart rtlFailedCall = CreateRtlCall(succeeded: false);

private static BitAiToolCallPart CreateRtlCall(bool succeeded)
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), "get_weather")
    {
        Definition = new BitAiToolDefinition("get_weather") { Title = "آب و هوا" },
    };

    call.CompleteArguments(Json(succeeded ? """{"city":"تهران"}""" : """{"city":"آتلانتیس"}"""));
    call.SetStatus(BitAiToolCallStatus.Running);
    call.SetResult(succeeded
        ? BitAiToolOutcome.FromText("تهران: ۱۸ درجه، آفتابی") with { Duration = TimeSpan.FromMilliseconds(650) }
        : BitAiToolOutcome.Failure("هیچ ایستگاه هواشناسی‌ای برای آتلانتیس گزارشی ندارد.") with { Duration = TimeSpan.FromMilliseconds(1200) });

    return call;
}

// The words of the card, in Persian.
private sealed class PersianAiStrings : BitAiStrings
{
    public override string CallingTool(string tool) => $"در حال اجرای {tool}";
    public override string CalledTool(string tool) => $"{tool} اجرا شد";
    public override string ToolFailed(string tool) => $"{tool} ناموفق بود";
    public override string ToolCancelled(string tool) => $"{tool} لغو شد";
    public override string ToolRejected(string tool) => $"{tool} رد شد";
    public override string ApproveTool(string tool) => $"اجازه‌ی اجرای {tool} داده شود؟";
    public override string ToolAwaitingApproval(string tool) => $"{tool} منتظر اجازه‌ی شماست";
    public override string Approve => "اجازه";
    public override string ApproveAlways => "همیشه اجازه";
    public override string Reject => "رد";
    public override string Arguments => "ورودی‌ها";
    public override string Result => "نتیجه";
    public override string FormatDuration(TimeSpan duration) => $"{duration.TotalSeconds:F1} ثانیه";
}
"""";
}
