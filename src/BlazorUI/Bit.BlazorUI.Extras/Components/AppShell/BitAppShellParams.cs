namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAppShell"/> component.
/// </summary>
/// <remarks>
/// What belongs here is how the shell of an app behaves - its insets, its scroller, its navigation features -
/// which is what a BitParams around the layout, or a set of layouts, agrees on. The content, the cascaded
/// values and the callbacks are deliberately not here: they are what makes one shell the one it is. Nor are
/// <see cref="BitAppShell.PersistScroll"/> and <see cref="BitAppShell.ScrollRestoration"/>: the positions they
/// keep are one store of the page's, which only the one app shell of an application is meant to own, so they
/// are set on that shell rather than handed to every shell under a BitParams.
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
    /// Gets or sets whether the root is marked with whether, and which way, the main container is being scrolled.
    /// </summary>
    public bool? TrackScrollState { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAppShell"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAppShell"/> itself.
    /// </summary>
    /// <remarks>
    /// A property this object leaves unset puts the app shell's own default back, unless the app shell set it itself: a
    /// value an earlier update wrote is otherwise left behind on the app shell once the BitParams stops cascading it.
    /// </remarks>
    /// <param name="bitAppShell">
    /// The <see cref="BitAppShell"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAppShell bitAppShell)
    {
        if (bitAppShell is null) return;

        UpdateBaseParameters(bitAppShell);

        if (bitAppShell.HasNotBeenSet(nameof(AutoGoToTop)))
        {
            bitAppShell.AutoGoToTop = AutoGoToTop ?? false;
        }

        if (bitAppShell.HasNotBeenSet(nameof(AutoScroll)))
        {
            bitAppShell.AutoScroll = AutoScroll ?? false;
        }

        if (bitAppShell.HasNotBeenSet(nameof(AutoScrollThreshold)))
        {
            bitAppShell.AutoScrollThreshold = AutoScrollThreshold ?? 0;
        }

        if (bitAppShell.HasNotBeenSet(nameof(AvoidKeyboard)))
        {
            bitAppShell.AvoidKeyboard = AvoidKeyboard ?? false;
        }

        if (bitAppShell.HasNotBeenSet(nameof(Classes)))
        {
            var classes = Classes;

            if (bitAppShell.Classes != classes)
            {
                bitAppShell.Classes = classes;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(FullScreen)))
        {
            var fullScreen = FullScreen ?? false;

            if (bitAppShell.FullScreen != fullScreen)
            {
                bitAppShell.FullScreen = fullScreen;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(Gutter)))
        {
            bitAppShell.Gutter = Gutter;
        }

        if (bitAppShell.HasNotBeenSet(nameof(NoBottomInset)))
        {
            var noBottomInset = NoBottomInset ?? false;

            if (bitAppShell.NoBottomInset != noBottomInset)
            {
                bitAppShell.NoBottomInset = noBottomInset;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(NoEndInset)))
        {
            var noEndInset = NoEndInset ?? false;

            if (bitAppShell.NoEndInset != noEndInset)
            {
                bitAppShell.NoEndInset = noEndInset;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(NoInsets)))
        {
            var noInsets = NoInsets ?? false;

            if (bitAppShell.NoInsets != noInsets)
            {
                bitAppShell.NoInsets = noInsets;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(NoScroll)))
        {
            bitAppShell.NoScroll = NoScroll ?? false;
        }

        if (bitAppShell.HasNotBeenSet(nameof(NoStartInset)))
        {
            var noStartInset = NoStartInset ?? false;

            if (bitAppShell.NoStartInset != noStartInset)
            {
                bitAppShell.NoStartInset = noStartInset;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(NoTopInset)))
        {
            var noTopInset = NoTopInset ?? false;

            if (bitAppShell.NoTopInset != noTopInset)
            {
                bitAppShell.NoTopInset = noTopInset;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(OverflowX)))
        {
            bitAppShell.OverflowX = OverflowX;
        }

        if (bitAppShell.HasNotBeenSet(nameof(OverflowY)))
        {
            bitAppShell.OverflowY = OverflowY;
        }

        if (bitAppShell.HasNotBeenSet(nameof(Overscroll)))
        {
            bitAppShell.Overscroll = Overscroll;
        }

        if (bitAppShell.HasNotBeenSet(nameof(PreserveScroll)))
        {
            bitAppShell.PreserveScroll = PreserveScroll ?? false;
        }

        if (bitAppShell.HasNotBeenSet(nameof(ReachOffset)))
        {
            bitAppShell.ReachOffset = ReachOffset ?? 0;
        }

        if (bitAppShell.HasNotBeenSet(nameof(ScrollBehavior)))
        {
            bitAppShell.ScrollBehavior = ScrollBehavior;
        }

        if (bitAppShell.HasNotBeenSet(nameof(ScrollPadding)))
        {
            bitAppShell.ScrollPadding = ScrollPadding.HasValue() ? ScrollPadding : null;
        }

        if (bitAppShell.HasNotBeenSet(nameof(ScrollThrottle)))
        {
            bitAppShell.ScrollThrottle = ScrollThrottle ?? 0;
        }

        if (bitAppShell.HasNotBeenSet(nameof(StableInsets)))
        {
            var stableInsets = StableInsets ?? false;

            if (bitAppShell.StableInsets != stableInsets)
            {
                bitAppShell.StableInsets = stableInsets;

                bitAppShell.ClassBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(Styles)))
        {
            var styles = Styles;

            if (bitAppShell.Styles != styles)
            {
                bitAppShell.Styles = styles;

                bitAppShell.StyleBuilder.Reset();
            }
        }

        if (bitAppShell.HasNotBeenSet(nameof(TrackScrollState)))
        {
            bitAppShell.TrackScrollState = TrackScrollState ?? false;
        }
    }
}
