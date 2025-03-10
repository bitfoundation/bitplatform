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



    /// <summary>
    /// The Markdown string value to render as an html element.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnMarkdownSet))]
    public string? Markdown { get; set; }



    protected override string RootElementClass => "bit-mdv";

    protected override async Task OnInitializedAsync()
    {
        if (_js.IsRuntimeInvalid()) // prerendering
        {
            try
            {
                await Task.Run(async () =>
                {
                    _html = await BitMarked.Parse(Markdown, _cts.Token);
                    await InvokeAsync(StateHasChanged);
                }, _cts.Token);
            }
            catch (FileNotFoundException ex) when (ex.FileName?.StartsWith("Jint") is true)
            {
                Console.Error.WriteLine("Please install `Jint` nuget package on the server project.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
        else
        {
            var scriptPath = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";
            if ((await _js.BitMarkdownViewerCheckScriptLoaded(scriptPath)) is false)
            {
                await _js.BitExtrasInitScripts([scriptPath]);
            }

            await ParseAndRender();
        }

        await base.OnInitializedAsync();
    }



    private async Task OnMarkdownSet()
    {
        if (IsRendered is false) return;

        await ParseAndRender();
    }

    private async Task ParseAndRender()
    {
        if (Markdown.HasNoValue()) return;

        _html = await _js.BitMarkdownViewerParse(Markdown!);

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
