namespace Bit.BlazorUI;

public partial class Link
{
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Appends the content hash of the file to the Href, so that a changed file is never served from the browser cache.
    /// The hash is read from the web root of the ASP.NET Core host, so it is only computed where that host is part of the
    /// app: server rendering, including the prerendering pass of an interactive component. The render that follows
    /// prerendering - on WebAssembly, in Blazor Hybrid, or on a server circuit - reuses the value prerendering produced,
    /// and a render with nothing to reuse and no web root renders the Href unchanged.
    /// </summary>
    [Parameter] public bool AppendVersion { get; set; } = true;



    [Inject] private IServiceProvider serviceProvider { get; set; } = default!;



    private string? href;



    // Whether this component renders again after prerendering, which is when what this render computed is
    // carried over.
    private bool Interactive =>
        AssignedRenderMode is not null;


    protected override void OnInitialized()
    {
        base.OnInitialized();

        href = BitAssetVersion.Resolve(serviceProvider, Href, AppendVersion, Interactive);
    }
}
