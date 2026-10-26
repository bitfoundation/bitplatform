using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ThinkingIndicator;

public partial class BitAiThinkingIndicatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiThinkingIndicatorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the thinking indicator.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text shown and announced beside the dots, for example the status of a running tool. Left null, the Thinking text of the BitAiStrings in force is shown.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiThinkingIndicatorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the thinking indicator.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiThinkingIndicatorClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiThinkingIndicator.",
                },
                new()
                {
                    Name = "Loading",
                    Type = "BitLoadingClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the parts of the BitBouncingDotsLoading the BitAiThinkingIndicator draws: its root (the live region), its dots (Container, Child) and its label.",
                    LinkType = LinkType.Link,
                    Href = "/components/loading#class-styles",
                },
            ],
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiThinkingIndicator-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the label. The dots keep the color of the loader (--bit-Loading-color)." },
        new() { Name = "--bit-AiThinkingIndicator-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the label, which the dots are drawn at too." },
    ];



    [AutoInject] private IBitAiToolRegistry toolRegistry = default!;

    private BitAiRenderScheduler sessionRenders = default!;
    private BitAiChatSession session = default!;

    protected override Task OnInitAsync()
    {
        sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

        // A scripted model that reports a status, calls get_weather and answers once the tool has returned.
        session = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "get_weather",
                """{"city":"Lisbon"}""",
                "It is 17 °C and clear in Lisbon - a good afternoon for a walk."),
            tools: toolRegistry);
        session.StateChanged += sessionRenders.Request;

        return base.OnInitAsync();
    }

    private Task AskAsync() => session.SubmitAsync("What is the weather like in Lisbon?");

    // What the wait is about: the tool being called - from its first argument streaming in until it has run - else the
    // status the model reported, else the default text.
    private static string? WaitLabel(BitAiMessage reply) =>
        reply.PartsOf<BitAiToolCallPart>().LastOrDefault() is { Status: BitAiToolCallStatus.Streaming or BitAiToolCallStatus.Pending or BitAiToolCallStatus.Running } call
            ? $"Calling the {call.Definition?.DisplayName ?? call.ToolName} tool"
            : reply.StatusText;

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (session is not null) await session.DisposeAsync();
        if (sessionRenders is not null) await sessionRenders.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
