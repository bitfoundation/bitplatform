namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.StreamingMarkdown;

public partial class BitAiStreamingMarkdownDemo
{
    private readonly string example1RazorCode = @"
<BitAiStreamingMarkdown Text=""@basicMarkdown"" />";
    private readonly string example1CsharpCode = @"
private const string basicMarkdown = """"""
    ### Two render modes

    Blazor renders on the **server**, over a circuit, or in the **browser**, on WebAssembly. Pick one per page with
    `@rendermode`, or let `InteractiveAuto` start on the server and move to the browser once it has downloaded.
    """""";";

    private readonly string example2RazorCode = @"
@implements IAsyncDisposable

<BitStack Horizontal Wrap AutoHeight Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
    <BitButton OnClick=""StreamAsync"" Disabled=""isStreaming"" IconName=""@BitIconName.Play"">Stream the answer</BitButton>
    <BitButton OnClick=""StopStreaming"" Disabled=""isStreaming is false"" IconName=""@BitIconName.Stop"" Variant=""BitVariant.Outline"">Stop</BitButton>
    <BitToggle @bind-Value=""slowMotion"" Text=""Slow motion"" />
</BitStack>

<div class=""stream-grid"">
    <div>
        <div class=""stream-title"">Received so far</div>
        <pre class=""stream-raw"">@streamedText</pre>
    </div>
    <div>
        <div class=""stream-title"">Rendered</div>
        <BitAiStreamingMarkdown Text=""@streamedText"" IsStreaming=""isStreaming"" />
    </div>
</div>";
    private readonly string example2CsharpCode = @"
private const string streamingAnswer = """"""
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
    """""";

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
}";
    private const string example2ScssCode = @"
.stream-grid {
    gap: 1rem;
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr));
}

.stream-grid > div {
    min-width: 0;
}

.stream-title {
    font-weight: 600;
    margin-bottom: 0.5rem;
}

.stream-raw {
    margin: 0;
    padding: 0.75rem;
    overflow: auto;
    min-height: 6rem;
    max-height: 28rem;
    font-size: 0.8125rem;
    white-space: pre-wrap;
    border-radius: 0.5rem;
    background-color: var(--bit-clr-bg-sec);
}";
    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("BitAiStreamingMarkdownDemo.razor.scss", example2ScssCode),
    ];

    private readonly string example3RazorCode = @"
<BitAiStreamingMarkdown Text=""@citedMarkdown""
                        Citations=""citations""
                        OnCitationClick=""(BitAiCitation citation) => clickedSource = citation.Source.Title"" />

<div>Activated: <b>@(clickedSource ?? ""none yet"")</b></div>";
    private readonly string example3CsharpCode = @"
private const string citedMarkdown = """"""
    Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit [1].
    If that circuit drops, the state goes with it unless you have persisted it yourself [2]. A number no
    citation has, such as [3], stays the text it is.
    """""";

private readonly BitAiCitation[] citations =
[
    new(""c1"", new BitAiCitationSource(""s1"", ""Blazor render modes"")
    {
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/components/render-modes"",
        SiteName = ""Microsoft Learn"",
    }) { Number = 1 },
    new(""c2"", new BitAiCitationSource(""s2"", ""SignalR circuit lifetime"")
    {
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr"",
        SiteName = ""Microsoft Learn"",
    }) { Number = 2 },
];

private string? clickedSource;";

    private readonly string example4RazorCode = @"
<BitToggle @bind-Value=""keepLineBreaks"" Text=""SoftBreaksAsHardBreaks"" />

<BitAiOptionsProvider UiOptions=""@(keepLineBreaks ? hardBreakOptions : softBreakOptions)"">
    <BitAiStreamingMarkdown Text=""@lineBreakMarkdown"" />
</BitAiOptionsProvider>";
    private readonly string example4CsharpCode = @"
private const string lineBreakMarkdown = """"""
    Server rendering is fast to first paint.
    WebAssembly works offline.
    Auto starts on one and moves to the other.
    """""";

private bool keepLineBreaks;
private readonly BitAiUiOptions softBreakOptions = BitAiUiOptions.Default;
private readonly BitAiUiOptions hardBreakOptions = BitAiUiOptions.Default with
{
    Markdown = BitAiMarkdownOptions.Default with { SoftBreaksAsHardBreaks = true },
};";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem 1rem;
        border-radius: 1rem;
        background-color: #1e293b;
        color: #e2e8f0;
    }

    .custom-root {
        border-inline-start: 4px solid tomato;
        padding-inline-start: 0.75rem;
    }

    .custom-heading {
        text-transform: uppercase;
        letter-spacing: 0.1em;
    }
</style>

<BitAiStreamingMarkdown Text=""@styleMarkdown"" Style=""padding: 0.5rem; border: 1px dashed dodgerblue;"" />

<BitAiStreamingMarkdown Text=""@styleMarkdown"" Class=""custom-class"" />

<BitAiStreamingMarkdown Text=""@styleMarkdown""
                        Styles=""@(new() { Markdown = new() { Heading = ""color: tomato;"", ListItem = ""font-style: italic;"" } })"" />

<BitAiStreamingMarkdown Text=""@styleMarkdown""
                        Classes=""@(new() { Root = ""custom-root"", Markdown = new() { Heading = ""custom-heading"" } })"" />

<div style=""--bit-AiMarkdownView-heading-color: mediumseagreen; --bit-AiMarkdownView-marker-color: mediumseagreen; --bit-AiMarkdownView-line-height: 2;"">
    <BitAiStreamingMarkdown Text=""@styleMarkdown"" />
</div>";
    private readonly string example5CsharpCode = @"
private const string styleMarkdown = """"""
    #### A styled answer

    - a first point
    - a second point
    """""";";

    private readonly string example6RazorCode = @"
<BitAiStreamingMarkdown Dir=""BitDir.Rtl"" Text=""@rtlMarkdown"" IsStreaming />";
    private readonly string example6CsharpCode = @"
private const string rtlMarkdown = """"""
    #### دو حالت رندر

    بلیزر روی **سرور** یا در **مرورگر** رندر می‌کند و پاسخ هنوز در حال نوشته شدن است
    """""";";
}
