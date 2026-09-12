namespace Bit.BlazorUI;

public partial class Script : IDisposable
{
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = default!;

    [Parameter] public string? Src { get; set; }

    /// <summary>
    /// Appends the content hash of the file to the Src, so that a changed file is never served from the browser cache.
    /// The hash is read from the web root of the ASP.NET Core host, so it is only appended where that host is part of the
    /// app: server rendering, including the prerendering pass of an interactive component. A render on WebAssembly or in
    /// Blazor Hybrid reuses the value that prerendering produced, and renders the Src unchanged when the component was
    /// not prerendered.
    /// </summary>
    [Parameter] public bool AppendVersion { get; set; } = true;

    [Parameter] public RenderFragment? ChildContent { get; set; }



    [Inject] private IServiceProvider serviceProvider { get; set; } = default!;



    private string? src;
    private PersistingComponentStateSubscription stateSubscription;



    // Only a component that is itself interactive renders again on the client, where the version cannot be
    // computed, so only that one has anything to carry over. Where the framework does not say (net8.0),
    // every component carries its value over rather than any of them losing it.
    private bool CarryOverToClient =>
#if NET9_0_OR_GREATER
        AssignedRenderMode is not null;
#else
        true;
#endif


    protected override void OnInitialized()
    {
        base.OnInitialized();

        (src, stateSubscription) = BitAssetVersion.Resolve(serviceProvider, nameof(Script), Src, AppendVersion, CarryOverToClient);
    }

    public void Dispose()
    {
        stateSubscription.Dispose();

        GC.SuppressFinalize(this);
    }
}
