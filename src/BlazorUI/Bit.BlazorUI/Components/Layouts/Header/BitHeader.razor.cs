namespace Bit.BlazorUI;

/// <summary>
/// The BitHeader component is used to display a title (and possibly other components) in a colored bar at the top of a site or application.
/// </summary>
public partial class BitHeader : BitComponentBase
{
    /// <summary>
    /// Gets or sets the content to be rendered inside the BitHeader.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the height of the BitHeader (in pixels).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Height { get; set; }

    /// <summary>
    /// Renders the header with a fixed position at the top of the page.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Fixed { get; set; }



    protected override string RootElementClass => "bit-hdr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Fixed ? "bit-hdr-fix" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Height.HasValue ? $"height:{Height}px" : string.Empty);
    }
}
