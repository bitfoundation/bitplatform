namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ReasoningPartView;

public partial class BitAiReasoningPartViewDemo
{
    private readonly string example1RazorCode = @"
<BitAiReasoningPartView Part=""finishedReasoning"" />";
    private readonly string example1CsharpCode = @"
private readonly BitAiReasoningPart finishedReasoning = new(""""""
    The user asks about the **cost of a round trip**. Two costs are worth separating:

    1. the fixed cost of the connection - one handshake, one request;
    2. the cost per token, which scales with the length of the answer.

    A number for each reads better than an adjective.
    """""");";

    private readonly string example2RazorCode = @"
@implements IAsyncDisposable

<BitButton OnClick=""ThinkAsync"" Disabled=""isThinking"" IconName=""@BitIconName.Play"">Ask a question</BitButton>
<BitButton OnClick=""StopThinking"" Disabled=""isThinking is false"" IconName=""@BitIconName.Stop"" Variant=""BitVariant.Outline"">Stop</BitButton>

@if (streamedReasoning is not null)
{
    <BitAiReasoningPartView Part=""streamedReasoning"" />

    <BitAiTextPartView Part=""streamedAnswer"" Message=""streamedMessage"" />
}";
    private readonly string example2CsharpCode = @"
private BitAiMessage? streamedMessage;
private BitAiReasoningPart? streamedReasoning;
private BitAiTextPart? streamedAnswer;
private bool isThinking;
private CancellationTokenSource? thinkCts;
private BitAiRenderScheduler? scheduler;

private async Task ThinkAsync()
{
    thinkCts = new CancellationTokenSource();
    scheduler ??= new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

    streamedMessage = new BitAiMessage(BitAiRole.Assistant);
    streamedReasoning = streamedMessage.AddPart(new BitAiReasoningPart());
    streamedAnswer = streamedMessage.AddPart(new BitAiTextPart());
    isThinking = true;

    try
    {
        var request = new BitAiRequest([]);

        // AiDemoTransports.Reasoning() is the scripted IBitAiTransport of this site: use your own.
        await foreach (var evt in AiDemoTransports.Reasoning().StreamAsync(request, thinkCts.Token))
        {
            switch (evt)
            {
                case BitAiReasoningDeltaEvent thought:
                    // The panel follows its part by itself: no render of the page is needed.
                    streamedReasoning.Append(thought.Delta);
                    break;

                case BitAiTextDeltaEvent delta:
                    // The answer has begun, so the reasoning is done.
                    streamedReasoning.Complete();
                    streamedAnswer.Append(delta.Delta);
                    scheduler.Request();
                    break;
            }
        }
    }
    catch (OperationCanceledException) { }
    finally
    {
        streamedReasoning.Complete();
        streamedAnswer.Complete();
        isThinking = false;
        await scheduler.FlushAsync();
    }
}

private void StopThinking() => thinkCts?.Cancel();

public async ValueTask DisposeAsync()
{
    thinkCts?.Cancel();
    if (scheduler is not null) await scheduler.DisposeAsync();
}";

    private readonly string example3RazorCode = @"
<BitAiOptionsProvider UiOptions=""openReasoningOptions"">
    <BitAiReasoningPartView Part=""finishedReasoning"" />
</BitAiOptionsProvider>";
    private readonly string example3CsharpCode = @"
private readonly BitAiUiOptions openReasoningOptions = BitAiUiOptions.Default with { CollapseReasoning = false };";

    private readonly string example4RazorCode = @"
<BitParams Parameters=""reasoningPartViewParams"">
    <BitAiReasoningPartView Part=""finishedReasoning"" />

    <BitAiReasoningPartView Part=""finishedReasoning"" Styles=""@(new() { Title = ""font-weight: bold;"" })"" />
</BitParams>";
    private readonly string example4CsharpCode = @"
// finishedReasoning is the one of the Basic example.
private readonly BitAiReasoningPartViewParams[] reasoningPartViewParams =
[
    new()
    {
        Style = ""max-width: 32rem;"",
        Styles = new() { Icon = ""color: goldenrod;"", Title = ""font-style: italic;"" },
    },
];";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        max-width: 32rem;
        border-radius: 1rem;
        outline: 2px dashed tomato;
    }

    .custom-root {
        max-width: 32rem;
    }

    .custom-title {
        color: rebeccapurple;
        text-transform: uppercase;
    }

    .custom-header {
        color: #4c1d95;
        background-color: #ede9fe;
    }

    .custom-icon {
        color: #6d28d9;
    }
</style>

<BitAiReasoningPartView Part=""finishedReasoning"" Style=""max-width: 32rem; box-shadow: 0 0 0.5rem dodgerblue;"" />

<BitAiReasoningPartView Part=""finishedReasoning"" Class=""custom-class"" />

<BitAiReasoningPartView Part=""finishedReasoning""
                        Styles=""@(new() { Icon = ""color: goldenrod;"",
                                          Title = ""color: goldenrod; font-style: italic;"",
                                          Body = ""padding: 0.5rem; border-radius: 0.5rem; background-color: #fef3c7; color: #78350f;"" })"" />

<BitAiReasoningPartView Part=""finishedReasoning""
                        Classes=""@(new() { Root = ""custom-root"", Icon = ""custom-icon"", Title = ""custom-title"", Collapsible = new() { Accordion = new() { Header = ""custom-header"" } } })"" />

<div style=""--bit-AiReasoningPartView-color: #8b5cf6; --bit-AiReasoningPartView-font-size: 1rem; --bit-AiReasoningPartView-max-height: 4rem;"">
    <BitAiReasoningPartView Part=""finishedReasoning"" />
</div>";

    private readonly string example6RazorCode = @"
<BitAiOptionsProvider UiOptions=""persianOptions"">
    <BitAiReasoningPartView Dir=""BitDir.Rtl"" Part=""rtlReasoning"" />
</BitAiOptionsProvider>";
    private readonly string example6CsharpCode = @"
private readonly BitAiReasoningPart rtlReasoning = new(""""""
    کاربر درباره‌ی **هزینه‌ی یک رفت‌وبرگشت** می‌پرسد. دو هزینه را باید از هم جدا کرد:

    1. هزینه‌ی ثابت اتصال؛
    2. هزینه‌ی هر توکن، که با طول پاسخ بیشتر می‌شود.
    """""");

private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with
{
    CollapseReasoning = false,
    Strings = new PersianStrings(),
};

private sealed class PersianStrings : BitAiStrings
{
    public override string Reasoning => ""استدلال"";

    public override string ReasonedFor(TimeSpan duration) => $""{Math.Ceiling(duration.TotalSeconds)} ثانیه فکر کرد"";
}";
}
