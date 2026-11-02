using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.TokenUsageView;

public partial class BitAiTokenUsageViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiTokenUsageViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the token usage view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Metadata",
            Type = "BitAiGenerationMetadata?",
            DefaultValue = "null",
            Description = "The metadata of the generation, which supplies the speed (TokensPerSecond) and the latency (TimeToFirstToken). Without it the readout shows the tokens and the cost alone.",
            LinkType = LinkType.Link,
            Href = "#generation-metadata",
        },
        new()
        {
            Name = "ShowLatency",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the time to the first token, when the Metadata carries it.",
        },
        new()
        {
            Name = "ShowSpeed",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the throughput in tokens per second, when the Metadata carries what it is computed from (the output tokens, the start and the end of the generation).",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiTokenUsageViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the token usage view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Usage",
            Type = "BitAiTokenUsage",
            DefaultValue = "",
            Description = "The usage to show (required): its total of tokens (shortened, 1.6k), its cost in its currency, and - as the native tooltip - a line for each kind of token it reports. A figure it does not carry is left out rather than shown empty.",
            LinkType = LinkType.Link,
            Href = "#token-usage",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiTokenUsageViewClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiTokenUsageView." },
                new() { Name = "Tokens", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the token count of the BitAiTokenUsageView." },
                new() { Name = "Cost", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the cost of the BitAiTokenUsageView." },
                new() { Name = "Speed", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the throughput of the BitAiTokenUsageView." },
                new() { Name = "Latency", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the time to the first token of the BitAiTokenUsageView." },
            ]
        },
        new()
        {
            Id = "token-usage",
            Title = "BitAiTokenUsage",
            Description = "The token accounting of one or more generations, a readonly record struct: new BitAiTokenUsage { InputTokens = ..., OutputTokens = ... }. Every field is optional, because providers report different subsets. Usages add up field by field (usage + other), which is how a message totals the requests of a tool loop and a run the requests made within it; a field missing on both sides stays missing, so a total never claims a zero the provider did not report.",
            Parameters =
            [
                new() { Name = "InputTokens", Type = "long?", DefaultValue = "null", Description = "The tokens consumed by the prompt." },
                new() { Name = "OutputTokens", Type = "long?", DefaultValue = "null", Description = "The tokens produced by the model." },
                new() { Name = "CachedInputTokens", Type = "long?", DefaultValue = "null", Description = "The input tokens served from a provider-side prompt cache." },
                new() { Name = "CacheWriteTokens", Type = "long?", DefaultValue = "null", Description = "The input tokens written to a provider-side prompt cache." },
                new() { Name = "ReasoningTokens", Type = "long?", DefaultValue = "null", Description = "The tokens spent on hidden reasoning, where the provider reports it separately." },
                new() { Name = "Cost", Type = "decimal?", DefaultValue = "null", Description = "The cost in the currency named by Currency, when the host can compute it (BitAiModel.EstimateCost prices a usage with the rates of a model)." },
                new() { Name = "Currency", Type = "string?", DefaultValue = "null", Description = "The ISO 4217 code of the Cost. USD when a cost is present without one." },
                new() { Name = "TotalTokens", Type = "long?", DefaultValue = "null", Description = "The input and the output tokens summed, when either is present." },
                new() { Name = "IsEmpty", Type = "bool", DefaultValue = "true", Description = "Whether no field carries a value." },
                new() { Name = "operator +", Type = "BitAiTokenUsage", DefaultValue = "", Description = "usage + other (or BitAiTokenUsage.Add(usage, other)): adds two usages field by field, keeping a field absent on both sides absent." },
                new() { Name = "Merge", Type = "BitAiTokenUsage", DefaultValue = "", Description = "BitAiTokenUsage.Merge(existing, update): overlays a newer snapshot on an older one field by field - a field the update reports replaces the old value, one it omits keeps it. How a message accumulates usage from a provider that reports the input tokens first and the output tokens at the end." },
            ]
        },
        new()
        {
            Id = "generation-metadata",
            Title = "BitAiGenerationMetadata",
            Description = "Everything known about how a message was produced, separate from what it says (BitAiMessage.Metadata) - held apart from the message body so that the streaming updates never touch it. A transport reports it as BitAiMetadataEvent and BitAiUsageUpdatedEvent; the session adds the timings. A sealed record.",
            Parameters =
            [
                new() { Name = "ModelId", Type = "string?", DefaultValue = "null", Description = "The identifier of the model that produced the message." },
                new() { Name = "ProviderId", Type = "string?", DefaultValue = "null", Description = "The identifier of the provider that served the request." },
                new()
                {
                    Name = "Usage",
                    Type = "BitAiTokenUsage",
                    DefaultValue = "",
                    Description = "The token accounting of the generation.",
                    LinkType = LinkType.Link,
                    Href = "#token-usage",
                },
                new()
                {
                    Name = "FinishReason",
                    Type = "BitAiFinishReason",
                    DefaultValue = "BitAiFinishReason.Unknown",
                    Description = "Why the generation stopped.",
                    LinkType = LinkType.Link,
                    Href = "#finish-reason-enum",
                },
                new() { Name = "StartedAt", Type = "DateTimeOffset?", DefaultValue = "null", Description = "When the request was sent." },
                new() { Name = "CompletedAt", Type = "DateTimeOffset?", DefaultValue = "null", Description = "When the last event arrived." },
                new() { Name = "TimeToFirstToken", Type = "TimeSpan?", DefaultValue = "null", Description = "The time from the request to the first streamed token - the latency the readout shows with ShowLatency." },
                new() { Name = "Duration", Type = "TimeSpan?", DefaultValue = "null", Description = "The total wall-clock duration of the generation, when both StartedAt and CompletedAt are known." },
                new() { Name = "TokensPerSecond", Type = "double?", DefaultValue = "null", Description = "The output tokens per second over the streaming window (the duration less the time to the first token), or null when it cannot be computed from the reported values - the speed the readout shows." },
                new() { Name = "Temperature", Type = "double?", DefaultValue = "null", Description = "The sampling temperature used, when reported." },
                new() { Name = "ContextWindow", Type = "long?", DefaultValue = "null", Description = "The size of the context window of the model in tokens, which a BitAiContextMeter reads off a conversation." },
                new() { Name = "Extra", Type = "IReadOnlyDictionary<string, object?>?", DefaultValue = "null", Description = "Provider-specific values that do not map onto the fields above." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "finish-reason-enum",
            Name = "BitAiFinishReason",
            Description = "The reason a generation stopped (BitAiGenerationMetadata.FinishReason).",
            Items =
            [
                new() { Name = "Unknown", Description = "The reason was not reported by the provider.", Value = "0" },
                new() { Name = "Stop", Description = "The model reached a natural end.", Value = "1" },
                new() { Name = "Length", Description = "The output hit the token limit.", Value = "2" },
                new() { Name = "ToolCalls", Description = "The model stopped to call one or more tools.", Value = "3" },
                new() { Name = "ContentFilter", Description = "The provider's safety system stopped the generation.", Value = "4" },
                new() { Name = "Cancelled", Description = "The caller cancelled the generation.", Value = "5" },
                new() { Name = "Error", Description = "The generation failed.", Value = "6" },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiTokenUsageView-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text of the readout." },
        new() { Name = "--bit-AiTokenUsageView-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Size of the readout." },
        new() { Name = "--bit-AiTokenUsageView-gap", DefaultValue = "spacing(1.5)", Description = "Room between two figures." },
    ];



    // Basic.
    private readonly BitAiTokenUsage basicUsage = new() { InputTokens = 1_240, OutputTokens = 386 };



    // Cost.
    private readonly BitAiTokenUsage centsUsage = new() { InputTokens = 1_240, OutputTokens = 386, Cost = 0.0095m, Currency = "USD" };
    private readonly BitAiTokenUsage dollarsUsage = new() { InputTokens = 182_000, OutputTokens = 24_500, Cost = 0.91m };
    private readonly BitAiTokenUsage eurosUsage = new() { InputTokens = 250_000, OutputTokens = 61_000, Cost = 1.25m, Currency = "EUR" };
    private readonly BitAiTokenUsage yenUsage = new() { InputTokens = 1_250_000, OutputTokens = 92_000, Cost = 420m, Currency = "JPY" };



    // Speed & latency: 386 output tokens streamed over 4.8 seconds, after a first token at 400ms.
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



    // Breakdown.
    private readonly BitAiTokenUsage detailedUsage = new()
    {
        InputTokens = 12_400,
        OutputTokens = 1_860,
        CachedInputTokens = 9_800,
        ReasoningTokens = 1_100,
        Cost = 0.0312m,
    };



    // A live conversation: usage, cost and timing reported by a scripted transport of this site.
    private BitAiChatSession? meteredSession;
    private bool isSending;

    private async Task SendMetered()
    {
        isSending = true;

        meteredSession ??= new BitAiChatSession(AiDemoInteractiveTransports.Metered(AiDemoCatalog.Models[0]));
        await meteredSession.SubmitAsync("How much did that cost?");

        isSending = false;
    }



    // Cascading parameters.
    private readonly BitAiTokenUsageViewParams[] usageViewParams =
    [
        new() { ShowLatency = true, ShowSpeed = false, Styles = new() { Cost = "font-weight: bold" } }
    ];



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (meteredSession is not null) await meteredSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
