namespace Bit.BlazorUI;

public partial class BitScrollablePane
{
    private BitScrollbarGutter scrollbarGutter = BitScrollbarGutter.Auto;
    private BitScrollbarVisibility scrollbarVisibility = BitScrollbarVisibility.Auto;

    /// <summary>
    /// The content of the ScrollablePane, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The height of the ScrollablePane.
    /// </summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// Callback for when the ScrollablePane scrolled.
    /// </summary>
    [Parameter] public EventCallback OnScroll { get; set; }

    /// <summary>
    /// Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.
    /// </summary>
    [Parameter] 
    public BitScrollbarGutter ScrollbarGutter
    {
        get => scrollbarGutter;
        set
        {
            if (scrollbarGutter == value) return;

            scrollbarGutter = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Controls the visibility of scrollbars in the ScrollablePane.
    /// </summary>
    [Parameter] 
    public BitScrollbarVisibility ScrollbarVisibility
    {
        get => scrollbarVisibility;
        set
        {
            if (scrollbarVisibility == value) return;

            scrollbarVisibility = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The width of the ScrollablePane.
    /// </summary>
    [Parameter] public string? Width { get; set; }



    protected override string RootElementClass => "bit-scp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ScrollbarVisibility switch
        {
            BitScrollbarVisibility.Hidden => $"{RootElementClass}-hdn",
            BitScrollbarVisibility.Scroll => $"{RootElementClass}-scr",
            _ => $"{RootElementClass}-aut"
        });

        ClassBuilder.Register(() => ScrollbarGutter switch
        {
            BitScrollbarGutter.Stable => $"{RootElementClass}-gst",
            BitScrollbarGutter.BothEdges => $"{RootElementClass}-gbe",
            _ => $"{RootElementClass}-gat"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);
        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);
    }
}
