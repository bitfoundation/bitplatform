namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitScrollablePane"/> component.
/// </summary>
public class BitScrollablePaneParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitScrollablePane"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitScrollablePane value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitScrollablePane)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps the pane pinned to the end of its content as the content grows.
    /// </summary>
    public bool? AutoScroll { get; set; }

    /// <summary>
    /// How near the end of the content (in pixels) the pane has to have been left for AutoScroll to keep pinning it there.
    /// </summary>
    public int? AutoScrollThreshold { get; set; }

    /// <summary>
    /// Makes the height of the pane auto.
    /// </summary>
    public bool? AutoHeight { get; set; }

    /// <summary>
    /// Makes both height and width of the pane auto.
    /// </summary>
    public bool? AutoSize { get; set; }

    /// <summary>
    /// Makes the width of the pane auto.
    /// </summary>
    public bool? AutoWidth { get; set; }

    /// <summary>
    /// Keeps the Modern scrollbar of the pane out of sight until the pane is pointed at, focused or scrolled.
    /// </summary>
    public bool? AutoHideScrollbar { get; set; }

    /// <summary>
    /// How long (in milliseconds) the AutoHideScrollbar scrollbar stays on the screen after the pane was last pointed at, focused or scrolled.
    /// </summary>
    public int? AutoHideDelay { get; set; }

    /// <summary>
    /// Lets the pane be scrolled by dragging its content with a pointer.
    /// </summary>
    public bool? DragScroll { get; set; }

    /// <summary>
    /// Lets a released drag carry on at the speed it was let go at and slow to a stop.
    /// </summary>
    public bool? DragMomentum { get; set; }

    /// <summary>
    /// Prints the whole of the content instead of the part the pane happens to be showing.
    /// </summary>
    public bool? ExpandOnPrint { get; set; }

    /// <summary>
    /// Fades out each edge of the pane that still has content beyond it.
    /// </summary>
    public bool? Fade { get; set; }

    /// <summary>
    /// How far the fade reaches into the pane, as any CSS length.
    /// </summary>
    public string? FadeSize { get; set; }

    /// <summary>
    /// Makes the height of the pane fit-content.
    /// </summary>
    public bool? FitHeight { get; set; }

    /// <summary>
    /// Makes both height and width of the pane fit-content.
    /// </summary>
    public bool? FitSize { get; set; }

    /// <summary>
    /// Makes the width of the pane fit-content.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Puts the pane itself in the tab order, so it can be scrolled with the keyboard.
    /// </summary>
    public bool? Focusable { get; set; }

    /// <summary>
    /// Makes the height of the pane 100%.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes both height and width of the pane 100%.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Makes the width of the pane 100%.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Reserves space for the scrollbar of the pane.
    /// </summary>
    public BitScrollbarGutter? Gutter { get; set; }

    /// <summary>
    /// The height of the pane.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Lays the content out on a single line that scrolls sideways instead of wrapping.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Turns a vertical wheel over a pane that only scrolls sideways into a sideways scroll.
    /// </summary>
    public bool? HorizontalWheel { get; set; }

    /// <summary>
    /// The maximum height of the pane.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The maximum width of the pane.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The minimum height of the pane.
    /// </summary>
    public string? MinHeight { get; set; }

    /// <summary>
    /// The minimum width of the pane.
    /// </summary>
    public string? MinWidth { get; set; }

    /// <summary>
    /// Draws the scrollbar of the pane in the colors of the theme.
    /// </summary>
    public bool? Modern { get; set; }

    /// <summary>
    /// Turns the scrolling of the pane off, without taking away anything else it does.
    /// </summary>
    public bool? NoScroll { get; set; }

    /// <summary>
    /// Controls the visibility of scrollbars in the pane.
    /// </summary>
    public BitOverflow? Overflow { get; set; }

    /// <summary>
    /// Controls the visibility of X-axis scrollbar in the pane.
    /// </summary>
    public BitOverflow? OverflowX { get; set; }

    /// <summary>
    /// Controls the visibility of Y-axis scrollbar in the pane.
    /// </summary>
    public BitOverflow? OverflowY { get; set; }

    /// <summary>
    /// What the browser does with a scroll that has already reached the edge of the pane.
    /// </summary>
    public BitOverscroll? Overscroll { get; set; }

    /// <summary>
    /// What the browser does with a horizontal scroll that has already reached the edge of the pane.
    /// </summary>
    public BitOverscroll? OverscrollX { get; set; }

    /// <summary>
    /// What the browser does with a vertical scroll that has already reached the edge of the pane.
    /// </summary>
    public BitOverscroll? OverscrollY { get; set; }

    /// <summary>
    /// Keeps the reader's place when content is added above what they are looking at.
    /// </summary>
    public bool? PreserveScroll { get; set; }

    /// <summary>
    /// How near an edge (in pixels) counts as having reached it, for the four edge callbacks.
    /// </summary>
    public int? ReachOffset { get; set; }

    /// <summary>
    /// The ARIA role of the pane.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Sets the color of the scrollbar thumb and track, as the standard CSS scrollbar-color property.
    /// </summary>
    public string? ScrollbarColor { get; set; }

    /// <summary>
    /// Sets the desired thickness of scrollbars when they are shown.
    /// </summary>
    public BitScrollbarWidth? ScrollbarWidth { get; set; }

    /// <summary>
    /// The inset the pane keeps between its edges and anything scrolled into view inside it, as any CSS length.
    /// </summary>
    public string? ScrollPadding { get; set; }

    /// <summary>
    /// The shortest interval (in milliseconds) between two OnScroll reports.
    /// </summary>
    public int? ScrollThrottle { get; set; }

    /// <summary>
    /// Animates the scrolling of the pane instead of jumping to the new position.
    /// </summary>
    public bool? Smooth { get; set; }

    /// <summary>
    /// Makes the pane come to rest on the snap positions of its content instead of anywhere between them.
    /// </summary>
    public BitScrollSnap? Snap { get; set; }

    /// <summary>
    /// Where the direct children of the pane come to rest in it while Snap is on.
    /// </summary>
    public BitScrollSnapAlign? SnapAlign { get; set; }

    /// <summary>
    /// Keeps a fast scroll from passing over the snap positions it goes by.
    /// </summary>
    public bool? SnapStop { get; set; }

    /// <summary>
    /// The width of the pane.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitScrollablePane"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitScrollablePane"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value and have not been set on the <see cref="BitScrollablePane"/> are updated.
    /// </remarks>
    /// <param name="bitScrollablePane">
    /// The <see cref="BitScrollablePane"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitScrollablePane bitScrollablePane)
    {
        if (bitScrollablePane is null) return;

        UpdateBaseParameters(bitScrollablePane);

        // The values that only the browser side reads (the JS options) need no builder reset: the options are
        // rebuilt from the properties after every render and compared with the ones last sent.
        if (AutoScroll.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoScroll), AutoScroll.Value, static s => s.AutoScroll, static (s, v) => s.AutoScroll = v);
        }

        if (AutoScrollThreshold.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoScrollThreshold), AutoScrollThreshold.Value, static s => s.AutoScrollThreshold, static (s, v) => s.AutoScrollThreshold = v);
        }

        if (AutoHeight.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoHeight), AutoHeight.Value, static s => s.AutoHeight, static (s, v) => s.AutoHeight = v);
        }

        if (AutoSize.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoSize), AutoSize.Value, static s => s.AutoSize, static (s, v) => s.AutoSize = v);
        }

        if (AutoWidth.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoWidth), AutoWidth.Value, static s => s.AutoWidth, static (s, v) => s.AutoWidth = v);
        }

        if (AutoHideScrollbar.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoHideScrollbar), AutoHideScrollbar.Value, static s => s.AutoHideScrollbar, static (s, v) => s.AutoHideScrollbar = v);
        }

        if (AutoHideDelay.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(AutoHideDelay), AutoHideDelay.Value, static s => s.AutoHideDelay, static (s, v) => s.AutoHideDelay = v);
        }

        if (DragScroll.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(DragScroll), DragScroll.Value, static s => s.DragScroll, static (s, v) => s.DragScroll = v);
        }

        if (DragMomentum.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(DragMomentum), DragMomentum.Value, static s => s.DragMomentum, static (s, v) => s.DragMomentum = v);
        }

        if (ExpandOnPrint.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(ExpandOnPrint), ExpandOnPrint.Value, static s => s.ExpandOnPrint, static (s, v) => s.ExpandOnPrint = v);
        }

        if (Fade.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Fade), Fade.Value, static s => s.Fade, static (s, v) => s.Fade = v);
        }

        if (FadeSize.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(FadeSize), FadeSize, static s => s.FadeSize, static (s, v) => s.FadeSize = v);
        }

        if (FitHeight.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(FitHeight), FitHeight.Value, static s => s.FitHeight, static (s, v) => s.FitHeight = v);
        }

        if (FitSize.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(FitSize), FitSize.Value, static s => s.FitSize, static (s, v) => s.FitSize = v);
        }

        if (FitWidth.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static s => s.FitWidth, static (s, v) => s.FitWidth = v);
        }

        if (Focusable.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Focusable), Focusable.Value, static s => s.Focusable, static (s, v) => s.Focusable = v);
        }

        if (FullHeight.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(FullHeight), FullHeight.Value, static s => s.FullHeight, static (s, v) => s.FullHeight = v);
        }

        if (FullSize.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(FullSize), FullSize.Value, static s => s.FullSize, static (s, v) => s.FullSize = v);
        }

        if (FullWidth.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static s => s.FullWidth, static (s, v) => s.FullWidth = v);
        }

        if (Gutter.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Gutter), Gutter.Value, static s => s.Gutter, static (s, v) => s.Gutter = v);
        }

        if (Height.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(Height), Height, static s => s.Height, static (s, v) => s.Height = v);
        }

        if (Horizontal.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Horizontal), Horizontal.Value, static s => s.Horizontal, static (s, v) => s.Horizontal = v);
        }

        if (HorizontalWheel.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(HorizontalWheel), HorizontalWheel.Value, static s => s.HorizontalWheel, static (s, v) => s.HorizontalWheel = v);
        }

        if (MaxHeight.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(MaxHeight), MaxHeight, static s => s.MaxHeight, static (s, v) => s.MaxHeight = v);
        }

        if (MaxWidth.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(MaxWidth), MaxWidth, static s => s.MaxWidth, static (s, v) => s.MaxWidth = v);
        }

        if (MinHeight.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(MinHeight), MinHeight, static s => s.MinHeight, static (s, v) => s.MinHeight = v);
        }

        if (MinWidth.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(MinWidth), MinWidth, static s => s.MinWidth, static (s, v) => s.MinWidth = v);
        }

        if (Modern.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Modern), Modern.Value, static s => s.Modern, static (s, v) => s.Modern = v);
        }

        if (NoScroll.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(NoScroll), NoScroll.Value, static s => s.NoScroll, static (s, v) => s.NoScroll = v);
        }

        if (Overflow.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Overflow), Overflow.Value, static s => s.Overflow, static (s, v) => s.Overflow = v);
        }

        if (OverflowX.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(OverflowX), OverflowX.Value, static s => s.OverflowX, static (s, v) => s.OverflowX = v);
        }

        if (OverflowY.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(OverflowY), OverflowY.Value, static s => s.OverflowY, static (s, v) => s.OverflowY = v);
        }

        if (Overscroll.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Overscroll), Overscroll.Value, static s => s.Overscroll, static (s, v) => s.Overscroll = v);
        }

        if (OverscrollX.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(OverscrollX), OverscrollX.Value, static s => s.OverscrollX, static (s, v) => s.OverscrollX = v);
        }

        if (OverscrollY.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(OverscrollY), OverscrollY.Value, static s => s.OverscrollY, static (s, v) => s.OverscrollY = v);
        }

        if (PreserveScroll.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(PreserveScroll), PreserveScroll.Value, static s => s.PreserveScroll, static (s, v) => s.PreserveScroll = v);
        }

        if (ReachOffset.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(ReachOffset), ReachOffset.Value, static s => s.ReachOffset, static (s, v) => s.ReachOffset = v);
        }

        if (Role.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(Role), Role, static s => s.Role, static (s, v) => s.Role = v);
        }

        if (ScrollbarColor.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(ScrollbarColor), ScrollbarColor, static s => s.ScrollbarColor, static (s, v) => s.ScrollbarColor = v);
        }

        if (ScrollbarWidth.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(ScrollbarWidth), ScrollbarWidth.Value, static s => s.ScrollbarWidth, static (s, v) => s.ScrollbarWidth = v);
        }

        if (ScrollPadding.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(ScrollPadding), ScrollPadding, static s => s.ScrollPadding, static (s, v) => s.ScrollPadding = v);
        }

        if (ScrollThrottle.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(ScrollThrottle), ScrollThrottle.Value, static s => s.ScrollThrottle, static (s, v) => s.ScrollThrottle = v);
        }

        if (Smooth.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Smooth), Smooth.Value, static s => s.Smooth, static (s, v) => s.Smooth = v);
        }

        if (Snap.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(Snap), Snap.Value, static s => s.Snap, static (s, v) => s.Snap = v);
        }

        if (SnapAlign.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(SnapAlign), SnapAlign.Value, static s => s.SnapAlign, static (s, v) => s.SnapAlign = v);
        }

        if (SnapStop.HasValue)
        {
            bitScrollablePane.TakeFromCascade(nameof(SnapStop), SnapStop.Value, static s => s.SnapStop, static (s, v) => s.SnapStop = v);
        }

        if (Width.HasValue())
        {
            bitScrollablePane.TakeFromCascade(nameof(Width), Width, static s => s.Width, static (s, v) => s.Width = v);
        }
    }
}
