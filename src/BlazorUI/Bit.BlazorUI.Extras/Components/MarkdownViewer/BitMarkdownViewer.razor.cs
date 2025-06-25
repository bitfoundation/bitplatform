namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a SEO friendly Blazor wrapper around the famous markedjs library.
/// <see href="https://github.com/markedjs/marked"/>
/// </summary>
public partial class BitMarkdownViewer : BitComponentBase
{
    private string? _html;
    private readonly CancellationTokenSource _cts = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private BitMarkdownService _markdownService { get; set; } = default!;



    /// <summary>
    /// The Markdown string value to render as an html element.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnMarkdownSet))]
    public string? Markdown { get; set; }



    protected override string RootElementClass => "bit-mdv";

    protected override async Task OnInitializedAsync()
    {
        await ParseAndRender();

        await base.OnInitializedAsync();
    }



    private void OnMarkdownSet()
    {
        if (IsRendered is false) return;

        _ = ParseAndRender();
    }

    private async Task ParseAndRender()
    {
        try
        {
            _html = await _markdownService.Parse(Markdown, _cts.Token);
        }
        catch
        {
            _html = "<b>Failed to parse the markdown!</b>";
        }

        StateHasChanged();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _cts.Cancel();
        _cts.Dispose();

        await base.DisposeAsync(disposing);
    }
}
