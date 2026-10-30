namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ContextMeter;

public partial class BitAiContextMeterDemo
{
    private readonly string example1RazorCode = """
<BitAiContextMeter Used="42_000" Window="128_000" />
""";

    private readonly string example2RazorCode = """
<BitSlider Label="Tokens used" Min="0" Max="140000" Step="1000" @bind-Value="sliderUsed" />

<BitAiContextMeter Used="(long)sliderUsed" Window="128_000" />

<BitAiContextMeter Used="(long)sliderUsed" Window="128_000" Warn="0.5" Critical="0.75" />
""";
    private readonly string example2CsharpCode = """
private double sliderUsed = 96_000;
""";

    private readonly string example3RazorCode = """
<BitTextField Label="Prompt" Multiline Immediate @bind-Value="prompt" />

<BitToggle Label="Attach the 20-page report (about 1,500 tokens)" @bind-Value="attachReport" />

<BitAiContextMeter Used="6_375" Window="8_000" PendingTokens="PendingPromptTokens" />
""";
    private readonly string example3CsharpCode = """
// A rough estimate of four characters a token.
private string? prompt = "Compare the two proposals and tell me which one we should ship first";
private bool attachReport;

private long PendingPromptTokens => (prompt?.Length ?? 0) / 4 + (attachReport ? 1_500 : 0);
""";

    private readonly string example4RazorCode = """
@foreach (var model in AiDemoCatalog.Models)
{
    <div>
        <span>@model.Name</span>
        <BitAiContextMeter Used="30_000" Model="model" />
    </div>
}
""";
    private readonly string example4CsharpCode = """
// AiDemoCatalog.Models are the models of this site, with context windows of 200,000, 128,000 and 32,000 tokens:
// https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Services/AI/AiDemoCatalog.cs
""";

    private readonly string example5RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="SendToTinyModel" Disabled="isSending">Send a message</BitButton>
<BitButton Variant="BitVariant.Outline" OnClick="StartOver" Disabled="isSending">Start over</BitButton>

<BitAiContextMeter Conversation="tinySession.Conversation" Model="tinyModel" />

<div>Messages: <b>@tinySession.Conversation.Messages.Count</b></div>
""";
    private readonly string example5CsharpCode = """
// AiDemoInteractiveTransports.Metered is a scripted transport of this site that reports usage and the context window:
// https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Services/AI/AiDemoInteractiveTransports.cs
private static readonly BitAiModel tinyModel = new("demo-tiny", "Demo Tiny") { ContextWindow = 340 };
private BitAiChatSession tinySession = new(AiDemoInteractiveTransports.Metered(tinyModel));
private bool isSending;

private async Task SendToTinyModel()
{
    isSending = true;

    await tinySession.SubmitAsync("Summarize the design review notes, then list the three decisions we still owe the platform team " +
                                  "and who is waiting on each one, so I can chase them before Friday and close the review.");

    isSending = false;
}

private async Task StartOver()
{
    await tinySession.DisposeAsync();
    tinySession = new(AiDemoInteractiveTransports.Metered(tinyModel));
}

public ValueTask DisposeAsync() => tinySession.DisposeAsync();
""";

    private readonly string example6RazorCode = """
<BitAiContextMeter Disabled Used="42_000" Window="128_000" />

<BitAiContextMeter Disabled Used="124_000" Window="128_000" />
""";

    private readonly string example7RazorCode = """
<BitParams Parameters="meterParams">
    <BitAiContextMeter Used="64_000" Window="128_000" />

    <BitAiContextMeter Used="90_000" Window="128_000" />

    <BitAiContextMeter Used="90_000" Window="128_000" Critical="0.95" />
</BitParams>
""";
    private readonly string example7CsharpCode = """
private readonly BitAiContextMeterParams[] meterParams =
[
    new() { Warn = 0.5, Critical = 0.6, Styles = new() { Label = "font-weight: bold" } }
];
""";

    private readonly string example8RazorCode = """
<style>
    .custom-class {
        padding: 0.5rem 0.75rem;
        border-radius: 1rem;
        background-color: #e0f2fe;
    }

    .custom-label {
        color: #0369a1;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }
</style>

<BitAiContextMeter Used="42_000" Window="128_000" Style="padding: 0.5rem; border: 1px dashed #0ea5e9; border-radius: 0.5rem" />

<BitAiContextMeter Used="42_000"
                   Window="128_000"
                   Class="custom-class"
                   Classes="@(new() { Label = "custom-label" })"
                   Styles="@(new() { Percent = "font-weight: bold; color: #0369a1" })" />

<BitAiContextMeter Used="110_000"
                   Window="128_000"
                   Style="--bit-AiContextMeter-font-size: 0.875rem;
                          --bit-AiContextMeter-gap: 0.5rem;
                          --bit-AiContextMeter-meter-max-width: 100%;
                          --bit-AiContextMeter-warning-color: #c2410c" />
""";

    private readonly string example9RazorCode = """
<BitAiContextMeter Dir="BitDir.Rtl" Used="42_000" Window="128_000" />

<div dir="rtl">
    <BitAiContextMeter Used="110_000" Window="128_000" />
</div>
""";
}
