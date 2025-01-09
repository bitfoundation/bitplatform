namespace Bit.BlazorUI;

/// <summary>
/// BitAppShell is an advanced container to handle the nuances of a cross-platform layout.
/// </summary>
public partial class BitAppShell : BitComponentBase
{
    private ElementReference _containerRef = default!;


    [Inject] private IJSRuntime _js { get; set; } = default!;



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



    public async Task GoToTop()
    {
        await _js.BitExtrasGoToTop(_containerRef);
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
}
