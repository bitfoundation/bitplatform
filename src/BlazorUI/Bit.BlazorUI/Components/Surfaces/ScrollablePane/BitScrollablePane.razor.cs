
namespace Bit.BlazorUI;

/// <summary>
/// A ScrollablePane is a component for scrolling through content that doesn't fit entirely on the screen.
/// </summary>
public partial class BitScrollablePane : BitComponentBase
{
    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Automatically scrolls to the end of the pane after each render.
    /// </summary>
    [Parameter] public bool AutoScroll { get; set; }

    /// <summary>
    /// The content of the pane, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitScrollbarGutter? Gutter { get; set; }

    /// <summary>
    /// The height of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Enables a modern style for the scrollbar of the pane.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Modern { get; set; }

    /// <summary>
    /// Callback for when the pane scrolled.
    /// </summary>
    [Parameter] public EventCallback OnScroll { get; set; }

    /// <summary>
    /// Controls the visibility of scrollbars in the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? Overflow { get; set; }

    /// <summary>
    /// Controls the visibility of X-axis scrollbar in the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? OverflowX { get; set; }

    /// <summary>
    /// Controls the visibility of Y-axis scrollbar in the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? OverflowY { get; set; }

    /// <summary>
    ///  Sets the color of the scrollbar track and thumb. For specific colors, it has to contain both colors separated by a space or otherwise it won't work.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? ScrollbarColor { get; set; }

    /// <summary>
    ///  Sets the desired thickness of scrollbars when they are shown.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitScrollbarWidth? ScrollbarWidth { get; set; }

    /// <summary>
    /// The width of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    /// <summary>
    /// Scrolls the pane to the end of its content, both horizontally and vertically.
    /// </summary>
    public ValueTask ScrollToEnd()
    {
        return _js.BitScrollablePaneScrollToEnd(RootElement);
    }



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
        StyleBuilder.Register(() => Gutter switch
        {
            BitScrollbarGutter.Auto => string.Empty,
            BitScrollbarGutter.Stable => "scrollbar-gutter:stable",
            BitScrollbarGutter.BothEdges => "scrollbar-gutter:stable both-edges",
            _ => string.Empty
        });

        // Auto is the default value which is already set on the root element
        StyleBuilder.Register(() => ScrollbarWidth switch
        {
            BitScrollbarWidth.Auto => string.Empty,
            BitScrollbarWidth.Thin => "scrollbar-width:thin",
            BitScrollbarWidth.None => "scrollbar-width:none",
            _ => string.Empty
        });

        StyleBuilder.Register(() => ScrollbarColor.HasValue() ? $"scrollbar-color:{ScrollbarColor}" : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Modern ? "bit-scp-mod" : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (AutoScroll)
        {
            await _js.BitScrollablePaneScrollToEnd(RootElement);
        }
    }



    private static readonly Dictionary<BitOverflow, string> _OverflowMap = new()
    {
        { BitOverflow.Auto, "auto" },
        { BitOverflow.Hidden, "hidden" },
        { BitOverflow.Scroll, "scroll" },
        { BitOverflow.Visible, "visible" },
    };
}
