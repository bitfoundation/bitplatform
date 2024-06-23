namespace Bit.BlazorUI;

public partial class BitHeader
{
    /// <summary>
    /// Gets or sets the content to be rendered inside the BitHeader.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the height of the BitHeader (in pixels).
    /// </summary>
    [Parameter] public int? Height { get; set; } = 50;

    /// <summary>
    /// Renders the header with a fixed position at the top of the page.
    /// </summary>
    [Parameter] public bool Fixed { get; set; }



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
