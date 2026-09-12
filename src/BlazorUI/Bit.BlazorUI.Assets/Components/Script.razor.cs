namespace Bit.BlazorUI;

public partial class Script
{
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string? Src { get; set; }

    /// <summary>
    /// Appends the content hash of the file to the Src, so that a changed file is never served from the browser cache.
    /// The hash is read from the web root of the ASP.NET Core host, so it is only computed where that host is part of the
    /// app: server rendering, including the prerendering pass of an interactive component. The render that follows
    /// prerendering - on WebAssembly, in Blazor Hybrid, or on a server circuit - reuses the value prerendering produced,
    /// and a render with nothing to reuse and no web root renders the Src unchanged.
    /// </summary>
    [Parameter] public bool AppendVersion { get; set; } = true;

    [Parameter] public RenderFragment? ChildContent { get; set; }



    [Inject] private IServiceProvider serviceProvider { get; set; } = default!;



    private string? src;



    // Whether this component renders again after prerendering, which is when what this render computed is
    // carried over. Where the framework does not say (net8.0), every component carries its value over: one
    // entry per page, read by a circuit or a WebAssembly client and otherwise left where the page's state goes.
    private bool Interactive =>
#if NET9_0_OR_GREATER
        AssignedRenderMode is not null;
#else
        true;
#endif


    protected override void OnInitialized()
    {
        base.OnInitialized();

        src = BitAssetVersion.Resolve(serviceProvider, Src, AppendVersion, Interactive);
    }
}
