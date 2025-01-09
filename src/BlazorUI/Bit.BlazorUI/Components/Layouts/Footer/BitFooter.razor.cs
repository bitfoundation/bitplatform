namespace Bit.BlazorUI;

/// <summary>
/// The BitFooter component is used to display a colored bar (with text and possibly other components) at the bottom of a site or application.
/// </summary>
public partial class BitFooter : BitComponentBase
{
    /// <summary>
    /// Gets or sets the content to be rendered inside the BitFooter.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the height of the BitFooter (in pixels).
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? Height { get; set; }

    /// <summary>
    /// Renders the footer with a fixed position at the bottom of the page.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Fixed { get; set; }



    protected override string RootElementClass => "bit-ftr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Fixed ? "bit-ftr-fix" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Height.HasValue ? $"height:{Height}px" : string.Empty);
    }
}
