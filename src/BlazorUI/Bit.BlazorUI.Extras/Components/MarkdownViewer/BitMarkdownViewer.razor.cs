namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a SEO friendly Blazor wrapper around the famous markedjs library.
/// <see href="https://github.com/markedjs/marked"/>
/// </summary>
public partial class BitMarkdownViewer : BitComponentBase
{
    private string? _html;
    private bool _isRenderingParsedHtml;
    private readonly CancellationTokenSource _cts = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private BitMarkdownService _markdownService { get; set; } = default!;



    /// <summary>
    /// The Markdown string value to render as an html element.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnMarkdownSet))]
    public string? Markdown { get; set; }

    /// <summary>
    /// A callback that is called before starting to parse the markdown.
    /// </summary>
    [Parameter] public EventCallback<string?> OnParsing { get; set; }

    /// <summary>
    /// A callback that is called after parsing the markdown.
    /// </summary>
    [Parameter] public EventCallback<string?> OnParsed { get; set; }

    /// <summary>
    /// A callback that is called after rendering the parsed markdown.
    /// </summary>
    [Parameter] public EventCallback<string?> OnRendered { get; set; }



    protected override string RootElementClass => "bit-mdv";

    protected override async Task OnInitializedAsync()
    {
        await ParseAndRender();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_isRenderingParsedHtml)
        {
            _isRenderingParsedHtml = false;

            _ = OnRendered.InvokeAsync(_html);
        }
    }



    private void OnMarkdownSet()
    {
        if (IsRendered is false) return;

        _ = ParseAndRender();
    }

    private async Task ParseAndRender()
    {
        _ = OnParsing.InvokeAsync(Markdown);

        try
        {
            _html = await _markdownService.Parse(Markdown, _cts.Token);
        }
        catch
        {
            _html = "<b>The BitMarkdownViewer failed to parse the markdown!</b>";
        }

        _ = OnParsed.InvokeAsync(_html);

        _isRenderingParsedHtml = true;

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
