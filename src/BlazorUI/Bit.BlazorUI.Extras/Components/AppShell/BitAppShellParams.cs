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
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAppShell"/> will be updated.
    /// A value it wrote goes back to the app shell's own once this object stops supplying it.
    /// </remarks>
    /// <param name="bitAppShell">
    /// The <see cref="BitAppShell"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAppShell bitAppShell)
    {
        if (bitAppShell is null) return;

        UpdateBaseParameters(bitAppShell);

        if (AutoGoToTop.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(AutoGoToTop), AutoGoToTop.Value, static a => a.AutoGoToTop, static (a, v) => a.AutoGoToTop = v);
        }

        if (AutoScroll.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(AutoScroll), AutoScroll.Value, static a => a.AutoScroll, static (a, v) => a.AutoScroll = v);
        }

        if (AutoScrollThreshold.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(AutoScrollThreshold), AutoScrollThreshold.Value, static a => a.AutoScrollThreshold, static (a, v) => a.AutoScrollThreshold = v);
        }

        if (AvoidKeyboard.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(AvoidKeyboard), AvoidKeyboard.Value, static a => a.AvoidKeyboard, static (a, v) => a.AvoidKeyboard = v);
        }

        if (Classes is not null)
        {
            bitAppShell.TakeFromCascade(nameof(Classes), Classes, static a => a.Classes, static (a, v) => a.Classes = v);
        }

        if (FullScreen.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(FullScreen), FullScreen.Value, static a => a.FullScreen, static (a, v) => a.FullScreen = v);
        }

        if (Gutter.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(Gutter), Gutter, static a => a.Gutter, static (a, v) => a.Gutter = v);
        }

        if (NoBottomInset.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(NoBottomInset), NoBottomInset.Value, static a => a.NoBottomInset, static (a, v) => a.NoBottomInset = v);
        }

        if (NoEndInset.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(NoEndInset), NoEndInset.Value, static a => a.NoEndInset, static (a, v) => a.NoEndInset = v);
        }

        if (NoInsets.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(NoInsets), NoInsets.Value, static a => a.NoInsets, static (a, v) => a.NoInsets = v);
        }

        if (NoScroll.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(NoScroll), NoScroll.Value, static a => a.NoScroll, static (a, v) => a.NoScroll = v);
        }

        if (NoStartInset.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(NoStartInset), NoStartInset.Value, static a => a.NoStartInset, static (a, v) => a.NoStartInset = v);
        }

        if (NoTopInset.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(NoTopInset), NoTopInset.Value, static a => a.NoTopInset, static (a, v) => a.NoTopInset = v);
        }

        if (OverflowX.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(OverflowX), OverflowX, static a => a.OverflowX, static (a, v) => a.OverflowX = v);
        }

        if (OverflowY.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(OverflowY), OverflowY, static a => a.OverflowY, static (a, v) => a.OverflowY = v);
        }

        if (Overscroll.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(Overscroll), Overscroll, static a => a.Overscroll, static (a, v) => a.Overscroll = v);
        }

        if (PreserveScroll.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(PreserveScroll), PreserveScroll.Value, static a => a.PreserveScroll, static (a, v) => a.PreserveScroll = v);
        }

        if (ReachOffset.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(ReachOffset), ReachOffset.Value, static a => a.ReachOffset, static (a, v) => a.ReachOffset = v);
        }

        if (ScrollBehavior.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(ScrollBehavior), ScrollBehavior, static a => a.ScrollBehavior, static (a, v) => a.ScrollBehavior = v);
        }

        if (ScrollPadding.HasValue())
        {
            bitAppShell.TakeFromCascade(nameof(ScrollPadding), ScrollPadding, static a => a.ScrollPadding, static (a, v) => a.ScrollPadding = v);
        }

        if (ScrollThrottle.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(ScrollThrottle), ScrollThrottle.Value, static a => a.ScrollThrottle, static (a, v) => a.ScrollThrottle = v);
        }

        if (StableInsets.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(StableInsets), StableInsets.Value, static a => a.StableInsets, static (a, v) => a.StableInsets = v);
        }

        if (Styles is not null)
        {
            bitAppShell.TakeFromCascade(nameof(Styles), Styles, static a => a.Styles, static (a, v) => a.Styles = v);
        }

        if (TrackScrollState.HasValue)
        {
            bitAppShell.TakeFromCascade(nameof(TrackScrollState), TrackScrollState.Value, static a => a.TrackScrollState, static (a, v) => a.TrackScrollState = v);
        }
    }
}
