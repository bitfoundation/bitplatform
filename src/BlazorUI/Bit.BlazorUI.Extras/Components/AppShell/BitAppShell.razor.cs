using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// BitAppShell is an advanced container to handle the nuances of a cross-platform layout.
/// </summary>
public partial class BitAppShell : BitComponentBase, IDisposable
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
        if (AutoGoToTop)
        {
            _navManager.LocationChanged += LocationChanged;
        }

        base.OnInitialized();
    }



    private void LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (AutoGoToTop)
        {
            _ = GoToTop(BitScrollBehavior.Instant);
        }
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        _navManager.LocationChanged -= LocationChanged;

        _disposed = true;
    }
}
