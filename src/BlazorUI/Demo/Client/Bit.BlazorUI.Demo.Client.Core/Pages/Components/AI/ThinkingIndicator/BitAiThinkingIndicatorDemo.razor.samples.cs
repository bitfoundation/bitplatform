namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ThinkingIndicator;

public partial class BitAiThinkingIndicatorDemo
{
    private readonly string example1RazorCode = """
<BitAiThinkingIndicator />
""";

    private readonly string example2RazorCode = """
<BitAiThinkingIndicator Label="Searching the documentation" />

<BitAiThinkingIndicator Label="Reading 3 attachments" />

<BitAiThinkingIndicator Label="Waiting for the weather service" />
""";

    private readonly string example3RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskAsync" Disabled="session.IsGenerating">Ask about the weather</BitButton>

@if (session.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant) is { } reply)
{
    @if (reply.PartsOf<BitAiTextPart>().Any(p => p.IsEmpty is false))
    {
        <div>@reply.GetText()</div>
    }
    else if (session.IsGenerating)
    {
        <BitAiThinkingIndicator Label="@WaitLabel(reply)" />
    }
}
""";
    private readonly string example3CsharpCode = """"
[Inject] private IBitAiToolRegistry ToolRegistry { get; set; } = default!;

private BitAiRenderScheduler sessionRenders = default!;
private BitAiChatSession session = default!;

protected override void OnInitialized()
{
    // At most one render of the page per frame interval, however often the session changes.
    sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

    // A scripted model that reports a status, calls get_weather and answers once the tool has returned.
    session = new BitAiChatSession(
        AiDemoToolTransports.CallsTool(
            "get_weather",
            """{"city":"Lisbon"}""",
            "It is 17 °C and clear in Lisbon - a good afternoon for a walk."),
        tools: ToolRegistry);
    session.StateChanged += sessionRenders.Request;
}

private Task AskAsync() => session.SubmitAsync("What is the weather like in Lisbon?");

// What the wait is about: the tool being run, else the status the model reported, else the default text.
private static string? WaitLabel(BitAiMessage reply) =>
    reply.PartsOf<BitAiToolCallPart>().LastOrDefault() is { Status: BitAiToolCallStatus.Pending or BitAiToolCallStatus.Running } call
        ? $"Calling the {call.Definition?.DisplayName ?? call.ToolName} tool"
        : reply.StatusText;

public async ValueTask DisposeAsync()
{
    await session.DisposeAsync();
    await sessionRenders.DisposeAsync();
}
"""";

    private readonly string example4RazorCode = """
<style>
    .custom-thinking {
        color: #fff;
        padding: 0.25rem 0.75rem;
        border-radius: 1rem;
        background-color: #0b7a75;
        --bit-Loading-color: #fff;
    }

    .custom-thinking-root {
        letter-spacing: 0.1em;
        text-transform: uppercase;
    }

    .custom-thinking-label {
        color: #b45309;
        font-weight: bold;
    }
</style>

<BitAiThinkingIndicator Style="padding: 0.5rem 1rem; border-radius: 1rem; background-color: #ede9fe" />

<BitAiThinkingIndicator Class="custom-thinking" />

<BitAiThinkingIndicator Styles="@(new() { Loading = new() { Label = "font-style: italic", Child = "background-color: #db2777" } })" />

<BitAiThinkingIndicator Classes="@(new() { Root = "custom-thinking-root", Loading = new() { Label = "custom-thinking-label" } })" />

<div style="--bit-AiThinkingIndicator-color: #0b7a75; --bit-AiThinkingIndicator-font-size: 1.25rem; --bit-Loading-color: #0b7a75;">
    <BitAiThinkingIndicator />
</div>
""";

    private readonly string example5RazorCode = """
<div dir="rtl">
    <BitAiThinkingIndicator Dir="BitDir.Rtl" Label="در حال فکر کردن" />
</div>
""";
}
