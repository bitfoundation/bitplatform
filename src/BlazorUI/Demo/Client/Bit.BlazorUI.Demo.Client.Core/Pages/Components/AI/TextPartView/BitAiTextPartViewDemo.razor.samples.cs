namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.TextPartView;

public partial class BitAiTextPartViewDemo
{
    private readonly string example1RazorCode = @"
<BitAiTextPartView Part=""answerPart"" Message=""answer"" />";
    private readonly string example1CsharpCode = @"
private const string answerText = """"""
    Blazor can render a component in **two places**:

    - on the server, which streams UI diffs over a circuit;
    - in the browser, on WebAssembly.

    Pick per page with `@rendermode`.
    """""";

private readonly BitAiMessage answer = new(BitAiRole.Assistant);
private BitAiTextPart answerPart = default!;

protected override void OnInitialized()
{
    answerPart = answer.AddPart(new BitAiTextPart(answerText));
}";

    private readonly string example2RazorCode = @"
@implements IAsyncDisposable

<BitButton OnClick=""StreamAsync"" Disabled=""isStreaming"" IconName=""@BitIconName.Play"">Stream an answer</BitButton>
<BitButton OnClick=""StopStreaming"" Disabled=""isStreaming is false"" IconName=""@BitIconName.Stop"" Variant=""BitVariant.Outline"">Stop</BitButton>

@if (streamedPart is not null)
{
    <BitAiTextPartView Part=""streamedPart"" Message=""streamedMessage"" />
}";
    private readonly string example2CsharpCode = @"
private BitAiMessage? streamedMessage;
private BitAiTextPart? streamedPart;
private bool isStreaming;
private CancellationTokenSource? streamCts;
private BitAiRenderScheduler? scheduler;

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
}";

    private readonly string example3RazorCode = @"
<BitAiTextPartView Part=""citedPart"" Message=""citedMessage"" OnCitationClick=""(BitAiCitation citation) => clickedSource = citation.Source.Title"" />

<div>Activated: <b>@(clickedSource ?? ""none yet"")</b></div>";
    private readonly string example3CsharpCode = @"
private const string citedText =
    ""Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit. "" +
    ""Persist what has to survive a dropped circuit with `PersistentComponentState`."";

private readonly BitAiMessage citedMessage = new(BitAiRole.Assistant);
private BitAiTextPart citedPart = default!;
private string? clickedSource;

protected override void OnInitialized()
{
    citedPart = citedMessage.AddPart(new BitAiTextPart(citedText));
    citedMessage.AddCitation(new BitAiCitation(""c1"", new BitAiCitationSource(""s1"", ""Blazor render modes"")
    {
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/components/render-modes"",
        SiteName = ""Microsoft Learn"",
    })
    {
        PartId = citedPart.PartId,
        StartIndex = 0,
        EndIndex = citedText.IndexOf("" Persist"", StringComparison.Ordinal),
    });
    // This span ends inside the code span, where a marker would be rendered literally: it gets none.
    citedMessage.AddCitation(new BitAiCitation(""c2"", new BitAiCitationSource(""s2"", ""Prerender state persistence"")
    {
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/state-management"",
        SiteName = ""Microsoft Learn"",
    })
    {
        PartId = citedPart.PartId,
        StartIndex = citedText.IndexOf(""Persist"", StringComparison.Ordinal),
        EndIndex = citedText.IndexOf(""ComponentState"", StringComparison.Ordinal),
    });
}";

    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem 1rem;
        border-radius: 1rem;
        background-color: #ecfeff;
        color: #164e63;
    }

    .custom-root {
        border-inline-start: 4px solid rebeccapurple;
        padding-inline-start: 0.75rem;
    }

    .custom-paragraph {
        font-style: italic;
    }
</style>

<BitAiTextPartView Part=""stylePart"" Message=""styleMessage"" Style=""padding: 0.5rem; border: 1px dashed dodgerblue;"" />

<BitAiTextPartView Part=""stylePart"" Message=""styleMessage"" Class=""custom-class"" />

<BitAiTextPartView Part=""stylePart"" Message=""styleMessage""
                   Styles=""@(new() { Markdown = new() { Markdown = new() { InlineCode = ""color: tomato;"", Link = ""color: mediumseagreen;"" } } })"" />

<BitAiTextPartView Part=""stylePart"" Message=""styleMessage""
                   Classes=""@(new() { Root = ""custom-root"", Markdown = new() { Markdown = new() { Paragraph = ""custom-paragraph"" } } })"" />

<div style=""--bit-AiMarkdownView-link-color: hotpink; --bit-AiMarkdownView-code-background: #fde68a; --bit-AiMarkdownView-code-color: #78350f;"">
    <BitAiTextPartView Part=""stylePart"" Message=""styleMessage"" />
</div>";
    private readonly string example4CsharpCode = @"
private const string styleText = ""A [link](https://learn.microsoft.com/aspnet/core/blazor), some `inline code` and **bold** text."";

private readonly BitAiMessage styleMessage = new(BitAiRole.Assistant);
private BitAiTextPart stylePart = default!;

protected override void OnInitialized()
{
    stylePart = styleMessage.AddPart(new BitAiTextPart(styleText));
}";

    private readonly string example5RazorCode = @"
<BitAiTextPartView Dir=""BitDir.Rtl"" Part=""rtlPart"" Message=""rtlMessage"" />";
    private readonly string example5CsharpCode = @"
private const string rtlText = """"""
    بلیزر یک کامپوننت را در **دو جا** رندر می‌کند:

    - روی سرور، که تغییرات را از راه یک اتصال می‌فرستد؛
    - در مرورگر، روی WebAssembly.
    """""";

private readonly BitAiMessage rtlMessage = new(BitAiRole.Assistant);
private BitAiTextPart rtlPart = default!;

protected override void OnInitialized()
{
    rtlPart = rtlMessage.AddPart(new BitAiTextPart(rtlText));
}";
}
