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
        if (AutoScroll.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoScroll)))
        {
            bitScrollablePane.AutoScroll = AutoScroll.Value;
        }

        if (AutoScrollThreshold.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoScrollThreshold)))
        {
            bitScrollablePane.AutoScrollThreshold = AutoScrollThreshold.Value;
        }

        if (AutoHeight.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoHeight)))
        {
            bitScrollablePane.AutoHeight = AutoHeight.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (AutoSize.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoSize)))
        {
            bitScrollablePane.AutoSize = AutoSize.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (AutoWidth.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoWidth)))
        {
            bitScrollablePane.AutoWidth = AutoWidth.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (AutoHideScrollbar.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoHideScrollbar)))
        {
            bitScrollablePane.AutoHideScrollbar = AutoHideScrollbar.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (AutoHideDelay.HasValue && bitScrollablePane.HasNotBeenSet(nameof(AutoHideDelay)))
        {
            bitScrollablePane.AutoHideDelay = AutoHideDelay.Value;
        }

        if (DragScroll.HasValue && bitScrollablePane.HasNotBeenSet(nameof(DragScroll)))
        {
            bitScrollablePane.DragScroll = DragScroll.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (DragMomentum.HasValue && bitScrollablePane.HasNotBeenSet(nameof(DragMomentum)))
        {
            bitScrollablePane.DragMomentum = DragMomentum.Value;
        }

        if (ExpandOnPrint.HasValue && bitScrollablePane.HasNotBeenSet(nameof(ExpandOnPrint)))
        {
            bitScrollablePane.ExpandOnPrint = ExpandOnPrint.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (Fade.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Fade)))
        {
            bitScrollablePane.Fade = Fade.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (FadeSize.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(FadeSize)))
        {
            bitScrollablePane.FadeSize = FadeSize;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (FitHeight.HasValue && bitScrollablePane.HasNotBeenSet(nameof(FitHeight)))
        {
            bitScrollablePane.FitHeight = FitHeight.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (FitSize.HasValue && bitScrollablePane.HasNotBeenSet(nameof(FitSize)))
        {
            bitScrollablePane.FitSize = FitSize.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (FitWidth.HasValue && bitScrollablePane.HasNotBeenSet(nameof(FitWidth)))
        {
            bitScrollablePane.FitWidth = FitWidth.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Focusable.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Focusable)))
        {
            bitScrollablePane.Focusable = Focusable.Value;
        }

        if (FullHeight.HasValue && bitScrollablePane.HasNotBeenSet(nameof(FullHeight)))
        {
            bitScrollablePane.FullHeight = FullHeight.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (FullSize.HasValue && bitScrollablePane.HasNotBeenSet(nameof(FullSize)))
        {
            bitScrollablePane.FullSize = FullSize.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (FullWidth.HasValue && bitScrollablePane.HasNotBeenSet(nameof(FullWidth)))
        {
            bitScrollablePane.FullWidth = FullWidth.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Gutter.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Gutter)))
        {
            bitScrollablePane.Gutter = Gutter.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Height.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(Height)))
        {
            bitScrollablePane.Height = Height;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Horizontal.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Horizontal)))
        {
            bitScrollablePane.Horizontal = Horizontal.Value;

            bitScrollablePane.ClassBuilder.Reset();
            bitScrollablePane.StyleBuilder.Reset();
        }

        if (HorizontalWheel.HasValue && bitScrollablePane.HasNotBeenSet(nameof(HorizontalWheel)))
        {
            bitScrollablePane.HorizontalWheel = HorizontalWheel.Value;
        }

        if (MaxHeight.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitScrollablePane.MaxHeight = MaxHeight;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (MaxWidth.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitScrollablePane.MaxWidth = MaxWidth;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (MinHeight.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(MinHeight)))
        {
            bitScrollablePane.MinHeight = MinHeight;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (MinWidth.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(MinWidth)))
        {
            bitScrollablePane.MinWidth = MinWidth;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Modern.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Modern)))
        {
            bitScrollablePane.Modern = Modern.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (NoScroll.HasValue && bitScrollablePane.HasNotBeenSet(nameof(NoScroll)))
        {
            bitScrollablePane.NoScroll = NoScroll.Value;

            bitScrollablePane.ClassBuilder.Reset();
            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Overflow.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Overflow)))
        {
            bitScrollablePane.Overflow = Overflow.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (OverflowX.HasValue && bitScrollablePane.HasNotBeenSet(nameof(OverflowX)))
        {
            bitScrollablePane.OverflowX = OverflowX.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (OverflowY.HasValue && bitScrollablePane.HasNotBeenSet(nameof(OverflowY)))
        {
            bitScrollablePane.OverflowY = OverflowY.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (Overscroll.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Overscroll)))
        {
            bitScrollablePane.Overscroll = Overscroll.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (OverscrollX.HasValue && bitScrollablePane.HasNotBeenSet(nameof(OverscrollX)))
        {
            bitScrollablePane.OverscrollX = OverscrollX.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (OverscrollY.HasValue && bitScrollablePane.HasNotBeenSet(nameof(OverscrollY)))
        {
            bitScrollablePane.OverscrollY = OverscrollY.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (PreserveScroll.HasValue && bitScrollablePane.HasNotBeenSet(nameof(PreserveScroll)))
        {
            bitScrollablePane.PreserveScroll = PreserveScroll.Value;
        }

        if (ReachOffset.HasValue && bitScrollablePane.HasNotBeenSet(nameof(ReachOffset)))
        {
            bitScrollablePane.ReachOffset = ReachOffset.Value;
        }

        if (Role.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(Role)))
        {
            bitScrollablePane.Role = Role;
        }

        if (ScrollbarColor.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(ScrollbarColor)))
        {
            bitScrollablePane.ScrollbarColor = ScrollbarColor;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (ScrollbarWidth.HasValue && bitScrollablePane.HasNotBeenSet(nameof(ScrollbarWidth)))
        {
            bitScrollablePane.ScrollbarWidth = ScrollbarWidth.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (ScrollPadding.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(ScrollPadding)))
        {
            bitScrollablePane.ScrollPadding = ScrollPadding;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (ScrollThrottle.HasValue && bitScrollablePane.HasNotBeenSet(nameof(ScrollThrottle)))
        {
            bitScrollablePane.ScrollThrottle = ScrollThrottle.Value;
        }

        if (Smooth.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Smooth)))
        {
            bitScrollablePane.Smooth = Smooth.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (Snap.HasValue && bitScrollablePane.HasNotBeenSet(nameof(Snap)))
        {
            bitScrollablePane.Snap = Snap.Value;

            bitScrollablePane.StyleBuilder.Reset();
        }

        if (SnapAlign.HasValue && bitScrollablePane.HasNotBeenSet(nameof(SnapAlign)))
        {
            bitScrollablePane.SnapAlign = SnapAlign.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (SnapStop.HasValue && bitScrollablePane.HasNotBeenSet(nameof(SnapStop)))
        {
            bitScrollablePane.SnapStop = SnapStop.Value;

            bitScrollablePane.ClassBuilder.Reset();
        }

        if (Width.HasValue() && bitScrollablePane.HasNotBeenSet(nameof(Width)))
        {
            bitScrollablePane.Width = Width;

            bitScrollablePane.StyleBuilder.Reset();
        }
    }
}
