using Jint;

namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a Blazor wrapper around the famous markedjs library.
/// <see href="https://github.com/markedjs/marked"/>
/// </summary>
public partial class BitMarkdownViewer : BitComponentBase
{
    private static string _ScriptPath = "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js";



    private string? _html;
    private CancellationTokenSource _cts = new();



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
                await RunJint();
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex.ToString());
            }
        }
        else if (OperatingSystem.IsBrowser())
        {
            if (await _js.BitMarkdownViewerCheckScript(_ScriptPath))
            {
                await ParseAndRender();
            }
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (await _js.BitMarkdownViewerCheckScript(_ScriptPath) is false)
            {
                await _js.BitExtrasInitScripts([_ScriptPath]);
            }

            if (_html.HasNoValue())
            {
                await ParseAndRender();
            }
        }
    }


    private async Task RunJint()
    {
        if (Markdown.HasNoValue()) return;

        await Task.Run(async () =>
        {
            var scriptPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "Bit.BlazorUI.Extras", "wwwroot", "marked", "marked-15.0.7.js");
            var script = await File.ReadAllTextAsync(scriptPath);

            var engine = new Engine(options =>
            {
                options.CancellationToken(_cts.Token);
            }).Execute(script);

            var fn = engine.Evaluate("marked.parse").AsFunctionInstance();

            _html = fn.Call(Markdown).AsString();

            await InvokeAsync(StateHasChanged);
        }, _cts.Token);
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
