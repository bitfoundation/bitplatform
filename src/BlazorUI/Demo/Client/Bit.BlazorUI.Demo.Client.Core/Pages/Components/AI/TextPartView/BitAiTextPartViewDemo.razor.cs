using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.TextPartView;

public partial class BitAiTextPartViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiTextPartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the text part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Message",
            Type = "BitAiMessage",
            DefaultValue = "",
            Description = "The message the part belongs to (required), whose citations the markers of the text resolve against. Only the citations anchored to this part (their PartId, or none) and whose span the text already reaches are woven in.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chatmessage",
        },
        new()
        {
            Name = "OnCitationClick",
            Type = "EventCallback<BitAiCitation>",
            DefaultValue = "",
            Description = "The callback for when the user activates an inline citation marker, with the citation it stands for.",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiTextPart",
            DefaultValue = "",
            Description = "The part to render (required). Its text is rendered as Markdown, with a caret and its last block left open while the part is streaming; the citations are woven in again only when the version of the part or the number of citations of the message changed.",
            LinkType = LinkType.Link,
            Href = "#text-part",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiTextPartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the text part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiTextPartViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiTextPartView.",
                },
                new()
                {
                    Name = "Markdown",
                    Type = "BitAiStreamingMarkdownClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiStreamingMarkdown the BitAiTextPartView renders its text with (and, through its own Markdown, for every part of the Markdown view inside it).",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/streamingmarkdown/#class-styles",
                },
            ]
        },
        new()
        {
            Id = "text-part",
            Title = "BitAiTextPart",
            Description = "A run of text that grows as the model writes it - the body of an assistant message. new BitAiTextPart() creates an empty part that expects more text (streaming), new BitAiTextPart(text) a finished one, and new BitAiTextPart(text, partId) a part with an identifier a transport addresses it by (null text: streaming). Appending is amortised constant time, and it is safe for one writer and any number of readers. BitAiMarkdownPart is the same part marked as Markdown, for hosts that render plain text parts verbatim.",
            Parameters =
            [
                new()
                {
                    Name = "Text",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The text accumulated so far.",
                },
                new()
                {
                    Name = "Length",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "How many characters have arrived, without materialising the string - the cheap read for hot paths such as auto-scroll checks.",
                },
                new()
                {
                    Name = "IsEmpty",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether no text has arrived yet.",
                },
                new()
                {
                    Name = "IsStreaming",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether more text is expected: true for a part created without text until Complete is called.",
                },
                new()
                {
                    Name = "Append(string? delta)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Appends a delta and notifies the observers of the part (an empty delta is ignored).",
                },
                new()
                {
                    Name = "SetText(string text)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Replaces the whole text, for edits and regenerations.",
                },
                new()
                {
                    Name = "Complete()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Marks the part finished, which lets the renderers do the work they deferred while it was growing (highlighting a code block, dropping the caret).",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "\"text\"",
                    Description = "The part kind (BitAiMessagePartKinds.Text; \"markdown\" for a BitAiMarkdownPart), which picks the renderer of the part in a message.",
                },
                new()
                {
                    Name = "PartId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the part, given to the constructor or generated. An anchored citation names it in its PartId.",
                },
                new()
                {
                    Name = "Version",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Increments on every change, so a renderer compares two integers rather than two texts.",
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitAiMessagePart>?",
                    DefaultValue = "",
                    Description = "Raised after every change, on whichever thread made it; the message the part belongs to raises its own PartChanged too.",
                },
            ]
        },
    ];



    private const string answerText = """
        Blazor can render a component in **two places**:

        - on the server, which streams UI diffs over a circuit;
        - in the browser, on WebAssembly.

        Pick per page with `@rendermode`.
        """;

    private readonly BitAiMessage answer = new(BitAiRole.Assistant);
    private BitAiTextPart answerPart = default!;

    private BitAiMessage? streamedMessage;
    private BitAiTextPart? streamedPart;
    private bool isStreaming;
    private CancellationTokenSource? streamCts;
    private BitAiRenderScheduler? scheduler;

    private const string citedText =
        "Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit. " +
        "Persist what has to survive a dropped circuit with `PersistentComponentState`.";

    private readonly BitAiMessage citedMessage = new(BitAiRole.Assistant);
    private BitAiTextPart citedPart = default!;
    private string? clickedSource;

    private const string styleText = "A [link](https://learn.microsoft.com/aspnet/core/blazor), some `inline code` and **bold** text.";

    private readonly BitAiMessage styleMessage = new(BitAiRole.Assistant);
    private BitAiTextPart stylePart = default!;

    private const string rtlText = """
        بلیزر یک کامپوننت را در **دو جا** رندر می‌کند:

        - روی سرور، که تغییرات را از راه یک اتصال می‌فرستد؛
        - در مرورگر، روی WebAssembly.
        """;

    private readonly BitAiMessage rtlMessage = new(BitAiRole.Assistant);
    private BitAiTextPart rtlPart = default!;

    protected override void OnInitialized()
    {
        answerPart = answer.AddPart(new BitAiTextPart(answerText));

        citedPart = citedMessage.AddPart(new BitAiTextPart(citedText));
        citedMessage.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Blazor render modes")
        {
            Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
            SiteName = "Microsoft Learn",
        })
        {
            PartId = citedPart.PartId,
            StartIndex = 0,
            EndIndex = citedText.IndexOf(" Persist", StringComparison.Ordinal),
        });
        // This span ends inside the code span, where a marker would be rendered literally: it gets none.
        citedMessage.AddCitation(new BitAiCitation("c2", new BitAiCitationSource("s2", "Prerender state persistence")
        {
            Url = "https://learn.microsoft.com/aspnet/core/blazor/state-management",
            SiteName = "Microsoft Learn",
        })
        {
            PartId = citedPart.PartId,
            StartIndex = citedText.IndexOf("Persist", StringComparison.Ordinal),
            EndIndex = citedText.IndexOf("ComponentState", StringComparison.Ordinal),
        });

        stylePart = styleMessage.AddPart(new BitAiTextPart(styleText));

        rtlPart = rtlMessage.AddPart(new BitAiTextPart(rtlText));
    }

    private async Task StreamAsync()
    {
        streamCts = new CancellationTokenSource();
        scheduler ??= new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

        streamedMessage = new BitAiMessage(BitAiRole.Assistant);
        streamedPart = streamedMessage.AddPart(new BitAiTextPart());
        isStreaming = true;

        try
        {
            var request = new BitAiRequest([]);

            // AiDemoTransports.Markdown() is the scripted IBitAiTransport of this site: use your own.
            await foreach (var evt in AiDemoTransports.Markdown().StreamAsync(request, streamCts.Token))
            {
                if (evt is BitAiTextDeltaEvent delta)
                {
                    streamedPart.Append(delta.Delta);
                    scheduler.Request();
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            streamedPart.Complete();
            isStreaming = false;
            await scheduler.FlushAsync();
        }
    }

    private void StopStreaming() => streamCts?.Cancel();

    public async ValueTask DisposeAsync()
    {
        streamCts?.Cancel();
        if (scheduler is not null) await scheduler.DisposeAsync();
    }
}
