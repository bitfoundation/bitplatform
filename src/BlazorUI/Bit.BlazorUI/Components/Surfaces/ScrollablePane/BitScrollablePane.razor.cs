namespace Bit.BlazorUI;

/// <summary>
/// A ScrollablePane is a component for scrolling through content that doesn't fit entirely on the screen.
/// </summary>
public partial class BitScrollablePane : BitComponentBase
{
    /// <summary>
    /// The content of the ScrollablePane, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The height of the ScrollablePane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Callback for when the ScrollablePane scrolled.
    /// </summary>
    [Parameter] public EventCallback OnScroll { get; set; }

    /// <summary>
    /// Controls the visibility of scrollbars in the ScrollablePane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? Overflow { get; set; }

    /// <summary>
    /// Controls the visibility of X-axis scrollbar in the ScrollablePane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? OverflowX { get; set; }

    /// <summary>
    /// Controls the visibility of Y-axis scrollbar in the ScrollablePane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? OverflowY { get; set; }

    /// <summary>
    /// Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitScrollbarGutter? Gutter { get; set; }

    /// <summary>
    /// The width of the ScrollablePane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    protected override string RootElementClass => "bit-scp";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);
        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);

        // Auto is the default value which is already set on the root element
        StyleBuilder.Register(register =>
        {
            if (OverflowX is not null && OverflowY is not null)
            {
                return $"overflow:{_OverflowMap[OverflowX.Value]} {_OverflowMap[OverflowY.Value]}";
            }

            if (Overflow is not null && Overflow is not BitOverflow.Auto)
            {
                register($"overflow:{_OverflowMap[Overflow.Value]}");
            }

            if (OverflowX is not null && OverflowX is not BitOverflow.Auto)
            {
                register($"overflow-x:{_OverflowMap[OverflowX.Value]}");
            }

            if (OverflowY is not null && OverflowY is not BitOverflow.Auto)
            {
                register($"overflow-y:{_OverflowMap[OverflowY.Value]}");
            }

            return string.Empty;
        });

        // Auto is the default value which is already set on the root element 
        StyleBuilder.Register(() => Gutter is not null && Gutter is not BitScrollbarGutter.Auto
                ? $"scrollbar-gutter:stable{(Gutter is BitScrollbarGutter.BothEdges ? " both-edges" : "")}"
                : string.Empty);
    }



    private static readonly Dictionary<BitOverflow, string> _OverflowMap = new()
    {
        { BitOverflow.Auto, "auto" },
        { BitOverflow.Hidden, "hidden" },
        { BitOverflow.Scroll, "scroll" },
        { BitOverflow.Visible, "visible" },
    };
}
