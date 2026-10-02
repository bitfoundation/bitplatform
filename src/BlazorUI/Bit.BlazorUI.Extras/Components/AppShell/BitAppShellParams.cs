namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAppShell"/> component.
/// </summary>
/// <remarks>
/// What belongs here is how the shell of an app behaves - its insets, its scroller, its navigation features -
/// which is what a BitParams around the layout, or a set of layouts, agrees on. The content, the cascaded
/// values and the callbacks are deliberately not here: they are what makes one shell the one it is.
/// </remarks>
public class BitAppShellParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAppShell"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAppShell value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAppShell)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets whether the main container scrolls to the top on navigation.
    /// </summary>
    public bool? AutoGoToTop { get; set; }

    /// <summary>
    /// Gets or sets whether the main container stays pinned to the end of its content as the content grows.
    /// </summary>
    public bool? AutoScroll { get; set; }

    /// <summary>
    /// Gets or sets how near the end (in pixels) still counts as being at it for AutoScroll.
    /// </summary>
    public int? AutoScrollThreshold { get; set; }

    /// <summary>
    /// Gets or sets whether the height of the on-screen keyboard is taken off the scrolling area while it is open.
    /// </summary>
    public bool? AvoidKeyboard { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS classes for the different parts of the app shell.
    /// </summary>
    public BitAppShellClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets whether the app shell is pinned to the four edges of the screen.
    /// </summary>
    public bool? FullScreen { get; set; }

    /// <summary>
    /// Gets or sets the room the main container reserves for its scrollbar.
    /// </summary>
    public BitScrollbarGutter? Gutter { get; set; }

    /// <summary>
    /// Gets or sets whether the bottom safe area inset is removed.
    /// </summary>
    public bool? NoBottomInset { get; set; }

    /// <summary>
    /// Gets or sets whether the trailing side safe area inset is removed.
    /// </summary>
    public bool? NoEndInset { get; set; }

    /// <summary>
    /// Gets or sets whether all four safe area insets are removed.
    /// </summary>
    public bool? NoInsets { get; set; }

    /// <summary>
    /// Gets or sets whether the reader is prevented from scrolling the main container.
    /// </summary>
    public bool? NoScroll { get; set; }

    /// <summary>
    /// Gets or sets whether the leading side safe area inset is removed.
    /// </summary>
    public bool? NoStartInset { get; set; }

    /// <summary>
    /// Gets or sets whether the top safe area inset is removed.
    /// </summary>
    public bool? NoTopInset { get; set; }

    /// <summary>
    /// Gets or sets what the main container does with content that overflows it sideways.
    /// </summary>
    public BitOverflow? OverflowX { get; set; }

    /// <summary>
    /// Gets or sets what the main container does with content that overflows it downwards.
    /// </summary>
    public BitOverflow? OverflowY { get; set; }

    /// <summary>
    /// Gets or sets what happens when the main container is scrolled past its edge.
    /// </summary>
    public BitOverscroll? Overscroll { get; set; }

    /// <summary>
    /// Gets or sets whether the scroll position of the main container is kept per url and restored on navigation.
    /// </summary>
    public bool? PersistScroll { get; set; }

    /// <summary>
    /// Gets or sets whether the reader's place is kept when content is added above what they are looking at.
    /// </summary>
    public bool? PreserveScroll { get; set; }

    /// <summary>
    /// Gets or sets how near an edge (in pixels) counts as having reached it.
    /// </summary>
    public int? ReachOffset { get; set; }

    /// <summary>
    /// Gets or sets the scroll behavior of the main container.
    /// </summary>
    public BitScrollBehavior? ScrollBehavior { get; set; }

    /// <summary>
    /// Gets or sets the room the main container keeps between its edges and anything scrolled into view inside it.
    /// </summary>
    public string? ScrollPadding { get; set; }

    /// <summary>
    /// Gets or sets which navigations PersistScroll puts the reader back where they left a page on.
    /// </summary>
    public BitAppShellScrollRestoration? ScrollRestoration { get; set; }

    /// <summary>
    /// Gets or sets the shortest interval (in milliseconds) between two OnScroll reports.
    /// </summary>
    public int? ScrollThrottle { get; set; }

    /// <summary>
    /// Gets or sets whether the inset bars are sized from the largest safe areas the device can ask for.
    /// </summary>
    public bool? StableInsets { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS styles for the different parts of the app shell.
    /// </summary>
    public BitAppShellClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAppShell"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAppShell"/> itself.
    /// </summary>
    /// <param name="bitAppShell">
    /// The <see cref="BitAppShell"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAppShell bitAppShell)
    {
        if (bitAppShell is null) return;

        UpdateBaseParameters(bitAppShell);

        if (AutoGoToTop.HasValue && bitAppShell.HasNotBeenSet(nameof(AutoGoToTop)))
        {
            bitAppShell.AutoGoToTop = AutoGoToTop.Value;
        }

        if (AutoScroll.HasValue && bitAppShell.HasNotBeenSet(nameof(AutoScroll)))
        {
            bitAppShell.AutoScroll = AutoScroll.Value;
        }

        if (AutoScrollThreshold.HasValue && bitAppShell.HasNotBeenSet(nameof(AutoScrollThreshold)))
        {
            bitAppShell.AutoScrollThreshold = AutoScrollThreshold.Value;
        }

        if (AvoidKeyboard.HasValue && bitAppShell.HasNotBeenSet(nameof(AvoidKeyboard)))
        {
            bitAppShell.AvoidKeyboard = AvoidKeyboard.Value;
        }

        if (Classes is not null && bitAppShell.HasNotBeenSet(nameof(Classes)))
        {
            bitAppShell.Classes = Classes;

            bitAppShell.ClassBuilder.Reset();
        }

        if (FullScreen.HasValue && bitAppShell.HasNotBeenSet(nameof(FullScreen)))
        {
            bitAppShell.FullScreen = FullScreen.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (Gutter.HasValue && bitAppShell.HasNotBeenSet(nameof(Gutter)))
        {
            bitAppShell.Gutter = Gutter.Value;
        }

        if (NoBottomInset.HasValue && bitAppShell.HasNotBeenSet(nameof(NoBottomInset)))
        {
            bitAppShell.NoBottomInset = NoBottomInset.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (NoEndInset.HasValue && bitAppShell.HasNotBeenSet(nameof(NoEndInset)))
        {
            bitAppShell.NoEndInset = NoEndInset.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (NoInsets.HasValue && bitAppShell.HasNotBeenSet(nameof(NoInsets)))
        {
            bitAppShell.NoInsets = NoInsets.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (NoScroll.HasValue && bitAppShell.HasNotBeenSet(nameof(NoScroll)))
        {
            bitAppShell.NoScroll = NoScroll.Value;
        }

        if (NoStartInset.HasValue && bitAppShell.HasNotBeenSet(nameof(NoStartInset)))
        {
            bitAppShell.NoStartInset = NoStartInset.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (NoTopInset.HasValue && bitAppShell.HasNotBeenSet(nameof(NoTopInset)))
        {
            bitAppShell.NoTopInset = NoTopInset.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (OverflowX.HasValue && bitAppShell.HasNotBeenSet(nameof(OverflowX)))
        {
            bitAppShell.OverflowX = OverflowX.Value;
        }

        if (OverflowY.HasValue && bitAppShell.HasNotBeenSet(nameof(OverflowY)))
        {
            bitAppShell.OverflowY = OverflowY.Value;
        }

        if (Overscroll.HasValue && bitAppShell.HasNotBeenSet(nameof(Overscroll)))
        {
            bitAppShell.Overscroll = Overscroll.Value;
        }

        if (PersistScroll.HasValue && bitAppShell.HasNotBeenSet(nameof(PersistScroll)))
        {
            bitAppShell.PersistScroll = PersistScroll.Value;
        }

        if (PreserveScroll.HasValue && bitAppShell.HasNotBeenSet(nameof(PreserveScroll)))
        {
            bitAppShell.PreserveScroll = PreserveScroll.Value;
        }

        if (ReachOffset.HasValue && bitAppShell.HasNotBeenSet(nameof(ReachOffset)))
        {
            bitAppShell.ReachOffset = ReachOffset.Value;
        }

        if (ScrollBehavior.HasValue && bitAppShell.HasNotBeenSet(nameof(ScrollBehavior)))
        {
            bitAppShell.ScrollBehavior = ScrollBehavior.Value;
        }

        if (ScrollPadding.HasValue() && bitAppShell.HasNotBeenSet(nameof(ScrollPadding)))
        {
            bitAppShell.ScrollPadding = ScrollPadding;
        }

        if (ScrollRestoration.HasValue && bitAppShell.HasNotBeenSet(nameof(ScrollRestoration)))
        {
            bitAppShell.ScrollRestoration = ScrollRestoration.Value;
        }

        if (ScrollThrottle.HasValue && bitAppShell.HasNotBeenSet(nameof(ScrollThrottle)))
        {
            bitAppShell.ScrollThrottle = ScrollThrottle.Value;
        }

        if (StableInsets.HasValue && bitAppShell.HasNotBeenSet(nameof(StableInsets)))
        {
            bitAppShell.StableInsets = StableInsets.Value;

            bitAppShell.ClassBuilder.Reset();
        }

        if (Styles is not null && bitAppShell.HasNotBeenSet(nameof(Styles)))
        {
            bitAppShell.Styles = Styles;

            bitAppShell.StyleBuilder.Reset();
        }
    }
}
