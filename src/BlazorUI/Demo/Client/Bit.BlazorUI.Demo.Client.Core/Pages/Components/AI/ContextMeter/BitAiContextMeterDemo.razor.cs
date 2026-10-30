using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ContextMeter;

public partial class BitAiContextMeterDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiContextMeterClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the context meter.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation?",
            DefaultValue = "null",
            Description = "Reads the used tokens and the window from a conversation (see Measure) instead of taking them directly: the used tokens it reports win over Used, while an explicit Window wins over the window it reports.",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist#conversation",
        },
        new()
        {
            Name = "Critical",
            Type = "double",
            DefaultValue = "0.95",
            Description = "The fraction of the window at which the meter turns to its critical (error) styling and announces BitAiStrings.ContextCritical once.",
        },
        new()
        {
            Name = "Model",
            Type = "BitAiModel?",
            DefaultValue = "null",
            Description = "Supplies the window (its ContextWindow) when neither Window nor the Conversation carries one.",
            LinkType = LinkType.Link,
            Href = "/components/ai/modelselector#model",
        },
        new()
        {
            Name = "PendingTokens",
            Type = "long",
            DefaultValue = "0",
            Description = "An estimate of the tokens about to be added - the prompt being typed, a file about to be attached - counted on top of what the conversation has used, so the meter warns before the request that would overflow the window.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiContextMeterClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the context meter.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Used",
            Type = "long",
            DefaultValue = "0",
            Description = "The tokens used so far. Overridden by the Conversation when it carries usage.",
        },
        new()
        {
            Name = "Warn",
            Type = "double",
            DefaultValue = "0.8",
            Description = "The fraction of the window at which the meter turns to its warning styling and announces BitAiStrings.ContextWarning once.",
        },
        new()
        {
            Name = "Window",
            Type = "long?",
            DefaultValue = "null",
            Description = "The size of the context window in tokens. When null, the window the Conversation reports is used, else the ContextWindow of the Model; nothing is rendered while none is known.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Measure",
            Type = "(long? Used, long? Window)",
            DefaultValue = "",
            Description = "BitAiContextMeter.Measure(BitAiConversation conversation), static: works out how full the context is from a conversation - the tokens the next request will carry (the prompt plus the output of its latest assistant message that reports usage) and the window (the newest ContextWindow its messages report), either of which is null when the conversation does not report it. Summing every turn's usage instead, as a total-cost readout does, counts the early turns once per later turn and runs the meter to the top long before the model forgets anything.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiContextMeterClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiContextMeter." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label (\"Context\") of the BitAiContextMeter." },
                new()
                {
                    Name = "Meter",
                    Type = "BitProgressClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitProgress meter of the BitAiContextMeter.",
                    LinkType = LinkType.Link,
                    Href = "/components/progress#progressBar-class-styles",
                },
                new() { Name = "Percent", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the percentage readout of the BitAiContextMeter." },
                new() { Name = "Status", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the visually hidden live region of the BitAiContextMeter." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiContextMeter-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The label and the percentage." },
        new() { Name = "--bit-AiContextMeter-warning-color", DefaultValue = "var(--bit-clr-wrn-fg)", Description = "The label and the percentage past the Warn mark. The bar is drawn in the warning role there, which the state of the reading says rather than a choice of the app's." },
        new() { Name = "--bit-AiContextMeter-critical-color", DefaultValue = "var(--bit-clr-err-fg)", Description = "The label and the percentage past the Critical mark, where the bar is drawn in the error role." },
        new() { Name = "--bit-AiContextMeter-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Size of the label and the percentage." },
        new() { Name = "--bit-AiContextMeter-gap", DefaultValue = "spacing(1.5)", Description = "Room between the label, the bar and the percentage." },
        new() { Name = "--bit-AiContextMeter-meter-max-width", DefaultValue = "spacing(24)", Description = "Width the bar grows to where there is room, and asks for where the meter is sized to its content (in a flex row, say)." },
        new() { Name = "--bit-AiContextMeter-track-color", DefaultValue = "var(--bit-Progress-track-color, var(--bit-clr-brd-sec))", Description = "The empty part of the bar. The secondary border color by default rather than the background a BitProgress draws its track in, so it stands off the surface the meter sits on." },
    ];



    // Warn & Critical.
    private double sliderUsed = 96_000;



    // Pending tokens: a rough estimate of four characters a token.
    private string? prompt = "Compare the two proposals and tell me which one we should ship first";
    private bool attachReport;

    private long PendingPromptTokens => (prompt?.Length ?? 0) / 4 + (attachReport ? 1_500 : 0);



    // Conversation: a scripted transport of this site answering on a model with a tiny window.
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



    // Cascading parameters.
    private readonly BitAiContextMeterParams[] meterParams =
    [
        new() { Warn = 0.5, Critical = 0.6, Styles = new() { Label = "font-weight: bold" } }
    ];



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await tinySession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
