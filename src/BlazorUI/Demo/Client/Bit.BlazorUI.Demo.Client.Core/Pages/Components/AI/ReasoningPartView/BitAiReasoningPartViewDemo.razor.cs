using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ReasoningPartView;

public partial class BitAiReasoningPartViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiReasoningPartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the reasoning panel.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiReasoningPart",
            DefaultValue = "",
            Description = "The reasoning to render (required). The panel subscribes to the part and re-renders itself as it grows, at most once per BitAiUiOptions.RenderInterval; it is open while the part streams and collapses when it completes (when BitAiUiOptions.CollapseReasoning is on), unless the user toggled it. A new part starts the panel over.",
            LinkType = LinkType.Link,
            Href = "#reasoning-part",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiReasoningPartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the reasoning panel.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiReasoningPartViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiReasoningPartView.",
                },
                new()
                {
                    Name = "Collapsible",
                    Type = "BitAiCollapsibleClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiCollapsible that is the panel of the BitAiReasoningPartView (and, through its Accordion, for the parts of the accordion that draws it).",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/collapsible/#class-styles",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the header of the BitAiReasoningPartView: its icon, its title and its loading dots.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon in the header of the BitAiReasoningPartView.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title in the header of the BitAiReasoningPartView.",
                },
                new()
                {
                    Name = "Loading",
                    Type = "BitLoadingClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the loading dots (a BitBouncingDotsLoading) the header of the BitAiReasoningPartView shows while the model is still reasoning.",
                    LinkType = LinkType.Link,
                    Href = "/components/loading/#class-styles",
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the scrolling body of the BitAiReasoningPartView, which holds the reasoning.",
                },
                new()
                {
                    Name = "Markdown",
                    Type = "BitAiStreamingMarkdownClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiStreamingMarkdown the BitAiReasoningPartView renders the reasoning with.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/streamingmarkdown/#class-styles",
                },
            ]
        },
        new()
        {
            Id = "reasoning-part",
            Title = "BitAiReasoningPart",
            Description = "The reasoning of a model, kept apart from the answer so it can be collapsed, left out of copy-to-clipboard and kept out of the plain text that goes back to the provider on the next turn. new BitAiReasoningPart() creates an empty part that expects more reasoning (streaming), new BitAiReasoningPart(text) a finished one (with no Duration), and new BitAiReasoningPart(text, partId) a part with an identifier a transport addresses it by. It is a BitAiTextPart, so Text, IsStreaming, Append, Complete and the rest are the same members (see AiTextPartView).",
            Parameters =
            [
                new()
                {
                    Name = "StartedAt",
                    Type = "DateTimeOffset",
                    DefaultValue = "",
                    Description = "When the reasoning began: the moment the part was created.",
                },
                new()
                {
                    Name = "CompletedAt",
                    Type = "DateTimeOffset?",
                    DefaultValue = "null",
                    Description = "When the reasoning finished: the moment Complete was called on a streaming part.",
                },
                new()
                {
                    Name = "Duration",
                    Type = "TimeSpan?",
                    DefaultValue = "null",
                    Description = "How long the model spent reasoning, once finished; the panel's title says it (BitAiStrings.ReasonedFor), and shows BitAiStrings.Reasoning while it is null.",
                },
                new()
                {
                    Name = "Signature",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An opaque signature some providers require when reasoning is replayed on a later turn.",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "\"reasoning\"",
                    Description = "The part kind (BitAiMessagePartKinds.Reasoning), which picks the renderer of the part in a message.",
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiReasoningPartView-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the reasoning." },
        new() { Name = "--bit-AiReasoningPartView-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Text size of the reasoning." },
        new() { Name = "--bit-AiReasoningPartView-max-height", DefaultValue = "spacing(44)", Description = "Height past which the reasoning scrolls inside the panel." },
    ];



    private readonly BitAiReasoningPart finishedReasoning = new("""
        The user asks about the **cost of a round trip**. Two costs are worth separating:

        1. the fixed cost of the connection - one handshake, one request;
        2. the cost per token, which scales with the length of the answer.

        A number for each reads better than an adjective.
        """);

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
    }

    private readonly BitAiUiOptions openReasoningOptions = BitAiUiOptions.Default with { CollapseReasoning = false };

    private readonly BitAiReasoningPartViewParams[] reasoningPartViewParams =
    [
        new()
        {
            Style = "max-width: 32rem;",
            Styles = new() { Icon = "color: goldenrod;", Title = "font-style: italic;" },
        },
    ];

    private readonly BitAiReasoningPart rtlReasoning = new("""
        کاربر درباره‌ی **هزینه‌ی یک رفت‌وبرگشت** می‌پرسد. دو هزینه را باید از هم جدا کرد:

        1. هزینه‌ی ثابت اتصال؛
        2. هزینه‌ی هر توکن، که با طول پاسخ بیشتر می‌شود.
        """);

    private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with
    {
        CollapseReasoning = false,
        Strings = new PersianStrings(),
    };

    private sealed class PersianStrings : BitAiStrings
    {
        public override string Reasoning => "استدلال";

        public override string ReasonedFor(TimeSpan duration) => $"{Math.Ceiling(duration.TotalSeconds)} ثانیه فکر کرد";
    }
}
