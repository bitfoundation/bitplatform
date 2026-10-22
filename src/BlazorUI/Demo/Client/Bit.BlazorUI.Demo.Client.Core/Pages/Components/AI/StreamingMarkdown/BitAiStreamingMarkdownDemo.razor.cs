namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.StreamingMarkdown;

public partial class BitAiStreamingMarkdownDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Citations",
            Type = "IReadOnlyList<BitAiCitation>?",
            DefaultValue = "null",
            Description = "The citations that the inline citation markers of the text resolve against: a bracketed number such as [1] is drawn as a BitAiCitationMarker when a citation of that Number is here (and BitAiUiOptions.InlineCitations is on); one that matches none stays the literal text.",
            LinkType = LinkType.Link,
            Href = "/components/ai/citationmarker",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiStreamingMarkdownClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the streaming Markdown.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "IsStreaming",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether more text is expected. While it is true the streaming caret trails the text (unless BitAiUiOptions.ShowStreamingCaret is off), the last block is left open - a code block is not highlighted, a dangling emphasis or code delimiter is hidden - and turning it false settles the document.",
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
            Name = "Styles",
            Type = "BitAiStreamingMarkdownClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the streaming Markdown.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The Markdown received so far. Only the text after the last block boundary already seen is parsed again when it grows; a text that is not a continuation of the previous one (an edit, a new answer) is parsed afresh.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiStreamingMarkdownClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiStreamingMarkdown.",
                },
                new()
                {
                    Name = "Markdown",
                    Type = "BitAiMarkdownViewClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the parts of the BitAiMarkdownView the BitAiStreamingMarkdown renders its text with.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/markdownview/#class-styles",
                },
            ]
        },
    ];



    private const string basicMarkdown = """
        ### Two render modes

        Blazor renders on the **server**, over a circuit, or in the **browser**, on WebAssembly. Pick one per page with
        `@rendermode`, or let `InteractiveAuto` start on the server and move to the browser once it has downloaded.
        """;

    private const string streamingAnswer = """
        ## Comparing render modes

        The **short answer** is that it depends on *where* your users are.

        | Mode | Runs on | First paint |
        | --- | --- | :---: |
        | Server | The server, over a circuit | Fast |
        | WebAssembly | The browser | Slower |

        ```csharp
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
        ```

        - [x] Measure the first paint
        - [ ] Measure the cost of every update

        > Rendering more often than the eye can follow is not smoother. It is just louder.
        """;

    private string streamedText = string.Empty;
    private bool isStreaming;
    private bool slowMotion;
    private CancellationTokenSource? streamCts;
    private BitAiRenderScheduler? scheduler;

    private async Task StreamAsync()
    {
        streamCts = new CancellationTokenSource();
        scheduler ??= new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

        streamedText = string.Empty;
        isStreaming = true;

        try
        {
            // A few characters at a time, the way a model streams its tokens.
            for (var i = 0; i < streamingAnswer.Length; i += 4)
            {
                await Task.Delay(slowMotion ? 150 : 20, streamCts.Token);

                streamedText += streamingAnswer.Substring(i, Math.Min(4, streamingAnswer.Length - i));
                scheduler.Request();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
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

    private const string citedMarkdown = """
        Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit [1].
        If that circuit drops, the state goes with it unless you have persisted it yourself [2]. A number no
        citation has, such as [3], stays the text it is.
        """;

    private readonly BitAiCitation[] citations =
    [
        new("c1", new BitAiCitationSource("s1", "Blazor render modes")
        {
            Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
            SiteName = "Microsoft Learn",
        }) { Number = 1 },
        new("c2", new BitAiCitationSource("s2", "SignalR circuit lifetime")
        {
            Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
            SiteName = "Microsoft Learn",
        }) { Number = 2 },
    ];

    private string? clickedSource;

    private const string lineBreakMarkdown = """
        Server rendering is fast to first paint.
        WebAssembly works offline.
        Auto starts on one and moves to the other.
        """;

    private bool keepLineBreaks;
    private readonly BitAiUiOptions softBreakOptions = BitAiUiOptions.Default;
    private readonly BitAiUiOptions hardBreakOptions = BitAiUiOptions.Default with
    {
        Markdown = BitAiMarkdownOptions.Default with { SoftBreaksAsHardBreaks = true },
    };

    private const string styleMarkdown = """
        #### A styled answer

        - a first point
        - a second point
        """;

    private const string rtlMarkdown = """
        #### دو حالت رندر

        بلیزر روی **سرور** یا در **مرورگر** رندر می‌کند و پاسخ هنوز در حال نوشته شدن است
        """;
}
