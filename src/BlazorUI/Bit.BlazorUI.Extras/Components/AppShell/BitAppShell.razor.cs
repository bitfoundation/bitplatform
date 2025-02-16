using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// BitAppShell is an advanced container to handle the nuances of a cross-platform layout.
/// </summary>
[SuppressMessage("Trimming", "IL2110:Field with 'DynamicallyAccessedMembersAttribute' is accessed via reflection. Trimmer can't guarantee availability of the requirements of the field.", Justification = "<Pending>")]
public partial class BitAppShell : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;
    private ElementReference _containerRef = default!;


    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private NavigationManager _navManager { get; set; } = default!;



    /// <summary>
    /// Enables auto-scroll to the top of the main container on navigation.
    /// </summary>
    [Parameter] public bool AutoGoToTop { get; set; }

    /// <summary>
    /// The cascading values to be provided for the children of the layout.
    /// </summary>
    [Parameter] public IEnumerable<BitCascadingValue>? CascadingValues { get; set; }

    /// <summary>
    /// The content of the layout.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the layout.
    /// </summary>
    [Parameter] public BitAppShellClassStyles? Classes { get; set; }

    /// <summary>
    /// Persists scroll position of the main container and restores it on navigation.
    /// </summary>
    [Parameter] public bool PersistScroll { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the layout.
    /// </summary>
    [Parameter] public BitAppShellClassStyles? Styles { get; set; }



    /// <summary>
    /// Scrolls the main container to top.
    /// </summary>
    public async Task GoToTop(BitScrollBehavior? behavior = null)
    {
        await _js.BitExtrasGoToTop(_containerRef, behavior);
    }



    protected override string RootElementClass => "bit-ash";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        if (AutoGoToTop || PersistScroll)
        {
            _navManager.LocationChanged += LocationChanged;
        }

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && PersistScroll)
        {
            await _js.BitAppShellInitScroll(_containerRef, _navManager.Uri);
        }
    }



    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (PersistScroll)
        {
            _ = _js.BitAppShellNavigateScroll(_navManager.Uri);
        }
        else if (AutoGoToTop)
        {
            _ = GoToTop(BitScrollBehavior.Instant);
        }
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing is false || _disposed) return;

        _navManager.LocationChanged -= LocationChanged;

        await _js.BitAppShellDisposeScroll();

        _disposed = true;
    }
}
