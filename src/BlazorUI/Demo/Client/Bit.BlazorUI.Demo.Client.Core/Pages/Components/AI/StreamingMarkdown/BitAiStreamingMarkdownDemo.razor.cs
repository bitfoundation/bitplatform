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
        new()
        {
            Id = "incremental-parser",
            Title = "BitAiIncrementalMarkdownParser",
            Description = "The parser a BitAiStreamingMarkdown holds for its lifetime, for an app that renders streamed Markdown with a BitAiMarkdownView of its own (ShowCaret while it streams). It keeps the blocks before the last point no later text can change - the start of a line after a blank line that begins a new top-level block - and parses only the tail again on every call, so a long answer costs linear work rather than quadratic. A text with a link reference or footnote definition, which makes any text depend on any other, is parsed whole on every call. An instance belongs to one streaming text and is not thread-safe.",
            Parameters =
            [
                new() { Name = "BitAiIncrementalMarkdownParser(BitAiMarkdownOptions? options = null)", Type = "constructor", DefaultValue = "", Description = "Creates a parser for the given extensions (BitAiMarkdownOptions.Default when null). Make a new one when the options change.", LinkType = LinkType.Link, Href = "/components/ai/markdownview#markdown-options" },
                new() { Name = "Parse(string? source, bool isComplete = false)", Type = "BitAiMarkdownDocument", DefaultValue = "", Description = "Parses the whole text received so far - not just the new part - and returns a new document whose blocks may be shared with the previous one. isComplete false leaves the last block open; a source that does not continue the previous one is parsed afresh.", LinkType = LinkType.Link, Href = "/components/ai/markdownview#markdown-document" },
                new() { Name = "Reset()", Type = "void", DefaultValue = "", Description = "Discards all cached state, so the next parse starts from scratch." },
                new() { Name = "StableLength", Type = "int", DefaultValue = "0", Description = "How many characters of the source have been promoted to the stable prefix that is never parsed again - for diagnostics and tests: the measure of how much work is being reused." },
                new() { Name = "StableBlockCount", Type = "int", DefaultValue = "0", Description = "How many blocks are in the stable prefix." },
            ]
        },
        new()
        {
            Id = "render-scheduler",
            Title = "BitAiRenderScheduler",
            Description = "Coalesces the change notifications of a stream - hundreds of deltas a second - into a bounded number of renders, the way the AI components do with BitAiUiOptions.RenderInterval. Rendering every delta is wasted work in WebAssembly and a flood of diffs down the circuit of Blazor Server; the scheduler makes the frame rate matter rather than the throughput. While a render is in flight, further requests only mark it dirty and one more render follows, so a slow renderer falls behind in render count, never in memory. Dispose it with the page (IAsyncDisposable).",
            Parameters =
            [
                new() { Name = "BitAiRenderScheduler(Func<Task> flush, TimeSpan? interval = null)", Type = "constructor", DefaultValue = "", Description = "Creates a scheduler that calls flush when a render is due - in a component, () => InvokeAsync(StateHasChanged) - at most once per interval (DefaultInterval when null)." },
                new() { Name = "DefaultInterval", Type = "TimeSpan", DefaultValue = "50 ms", Description = "Static: one render every 50 milliseconds, twenty a second - continuous to the eye, and comfortable over a Blazor Server circuit on a poor connection." },
                new() { Name = "Request()", Type = "void", DefaultValue = "", Description = "Asks for a render. Cheap and safe to call from any thread, on every token." },
                new() { Name = "FlushAsync()", Type = "Task", DefaultValue = "", Description = "Renders at once, bypassing the interval: call it when the stream ends, so the final state is never left waiting behind a timer." },
                new() { Name = "RenderCount", Type = "int", DefaultValue = "0", Description = "How many renders the scheduler has performed, for tests and diagnostics." },
                new() { Name = "CoalescedCount", Type = "int", DefaultValue = "0", Description = "How many requests have been coalesced away." },
                new() { Name = "DisposeAsync()", Type = "ValueTask", DefaultValue = "", Description = "Stops the scheduler: a pending render is dropped, and later requests are ignored." },
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

    private const string danglingMarkdown = "The **short answer** is that it depends on **where";

    private bool keepLineBreaks;
    private bool hideDanglingDelimiters = true;
    private BitAiUiOptions markdownOptions = BitAiUiOptions.Default;

    // A new options instance only when a choice changes: one made on every render would rebuild the parser every time.
    private void UpdateMarkdownOptions() => markdownOptions = BitAiUiOptions.Default with
    {
        Markdown = BitAiMarkdownOptions.Default with
        {
            SoftBreaksAsHardBreaks = keepLineBreaks,
            HideDanglingDelimiters = hideDanglingDelimiters,
        },
    };

    private readonly BitAiStreamingMarkdownParams[] streamingParams =
    [
        new() { Styles = new() { Markdown = new() { Heading = "color: var(--bit-clr-pri);" } } }
    ];

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
