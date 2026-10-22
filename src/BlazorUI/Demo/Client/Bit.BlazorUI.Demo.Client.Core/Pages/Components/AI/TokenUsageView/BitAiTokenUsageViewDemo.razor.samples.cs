namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.TokenUsageView;

public partial class BitAiTokenUsageViewDemo
{
    private readonly string example1RazorCode = """
<BitAiTokenUsageView Usage="basicUsage" />
""";
    private readonly string example1CsharpCode = """
private readonly BitAiTokenUsage basicUsage = new() { InputTokens = 1_240, OutputTokens = 386 };
""";

    private readonly string example2RazorCode = """
<BitAiTokenUsageView Usage="centsUsage" />
<BitAiTokenUsageView Usage="dollarsUsage" />
<BitAiTokenUsageView Usage="eurosUsage" />
<BitAiTokenUsageView Usage="yenUsage" />
""";
    private readonly string example2CsharpCode = """
private readonly BitAiTokenUsage centsUsage = new() { InputTokens = 1_240, OutputTokens = 386, Cost = 0.0095m, Currency = "USD" };
private readonly BitAiTokenUsage dollarsUsage = new() { InputTokens = 182_000, OutputTokens = 24_500, Cost = 0.91m };
private readonly BitAiTokenUsage eurosUsage = new() { InputTokens = 250_000, OutputTokens = 61_000, Cost = 1.25m, Currency = "EUR" };
private readonly BitAiTokenUsage yenUsage = new() { InputTokens = 1_250_000, OutputTokens = 92_000, Cost = 420m, Currency = "JPY" };
""";

    private readonly string example3RazorCode = """
<BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" />
<BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" ShowLatency />
<BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" ShowSpeed="false" ShowLatency />
""";
    private readonly string example3CsharpCode = """
// 386 output tokens streamed over 4.8 seconds, after a first token at 400ms.
private static readonly DateTimeOffset requestedAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

private readonly BitAiGenerationMetadata metadata = new()
{
    ModelId = "demo-model-1",
    Usage = new() { InputTokens = 1_240, OutputTokens = 386, Cost = 0.0095m, Currency = "USD" },
    StartedAt = requestedAt,
    TimeToFirstToken = TimeSpan.FromMilliseconds(400),
    CompletedAt = requestedAt.AddSeconds(5.2),
    FinishReason = BitAiFinishReason.Stop,
};
""";

    private readonly string example4RazorCode = """
<BitAiTokenUsageView Usage="detailedUsage" />
<BitAiTokenUsageView Usage="detailedUsage" title="Billed at the batch rate" />
""";
    private readonly string example4CsharpCode = """
private readonly BitAiTokenUsage detailedUsage = new()
{
    InputTokens = 12_400,
    OutputTokens = 1_860,
    CachedInputTokens = 9_800,
    ReasoningTokens = 1_100,
    Cost = 0.0312m,
};
""";

    private readonly string example5RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="SendMetered" Disabled="isSending">Send a message</BitButton>

@if (meteredSession is not null)
{
    @foreach (var message in meteredSession.Conversation.Messages.Where(m => m.Role.IsAssistant && m.Metadata is not null))
    {
        <div>
            <span>@message.Metadata!.ModelId</span>
            <BitAiTokenUsageView Usage="message.Metadata.Usage" Metadata="message.Metadata" ShowLatency />
        </div>
    }

    <div>
        <b>Total</b>
        <BitAiTokenUsageView Usage="meteredSession.Conversation.TotalUsage" />
    </div>
}
""";
    private readonly string example5CsharpCode = """
// AiDemoInteractiveTransports.Metered is a scripted transport of this site that reports usage, cost and context size,
// priced with the rates of the model of AiDemoCatalog:
// https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Services/AI/AiDemoInteractiveTransports.cs
private BitAiChatSession? meteredSession;
private bool isSending;

private async Task SendMetered()
{
    isSending = true;

    meteredSession ??= new BitAiChatSession(AiDemoInteractiveTransports.Metered(AiDemoCatalog.Models[0]));
    await meteredSession.SubmitAsync("How much did that cost?");

    isSending = false;
}

public async ValueTask DisposeAsync()
{
    if (meteredSession is not null) await meteredSession.DisposeAsync();
}
""";

    private readonly string example6RazorCode = """
<BitParams Parameters="usageViewParams">
    <BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" />
    <BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" ShowSpeed="true" />
</BitParams>
""";
    private readonly string example6CsharpCode = """
private readonly BitAiTokenUsageViewParams[] usageViewParams =
[
    new() { ShowLatency = true, ShowSpeed = false, Styles = new() { Cost = "font-weight: bold" } }
];
""";

    private readonly string example7RazorCode = """
<style>
    .custom-class {
        padding: 0.25rem 0.5rem;
        border-radius: 0.25rem;
        color: #e2e8f0;
        background-color: #1e293b;
    }

    .custom-cost {
        color: #fbbf24;
        font-weight: bold;
    }
</style>

<BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" Style="padding: 0.25rem 0.5rem; border: 1px dashed #0ea5e9; border-radius: 1rem" />
<BitAiTokenUsageView Usage="metadata.Usage"
                     Metadata="metadata"
                     ShowLatency
                     Class="custom-class"
                     Classes="@(new() { Cost = "custom-cost" })"
                     Styles="@(new() { Speed = "color: #16a34a", Latency = "color: #d97706" })" />
<BitAiTokenUsageView Usage="metadata.Usage"
                     Metadata="metadata"
                     Style="--bit-AiTokenUsageView-color: #7c3aed; --bit-AiTokenUsageView-font-size: 0.875rem; --bit-AiTokenUsageView-gap: 0.25rem" />
""";

    private readonly string example8RazorCode = """
<BitAiTokenUsageView Dir="BitDir.Rtl" Usage="metadata.Usage" Metadata="metadata" ShowLatency />

<div dir="rtl">
    <BitAiTokenUsageView Usage="metadata.Usage" Metadata="metadata" ShowLatency />
</div>
""";
}
