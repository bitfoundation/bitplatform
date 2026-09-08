using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// BitAppShell is an advanced container to handle the nuances of a cross-platform layout.
/// </summary>
/// <remarks>
/// It is the outermost element of an application: it insets the four edges of the screen by the safe
/// areas the device reports, owns the one region the application scrolls in - which is what lets a Modal,
/// a Panel or an Overlay inside it hold the right scroller - and can keep the reader's place in the pages
/// they navigate between.
/// <br />
/// The safe area insets are read from <c>env(safe-area-inset-*)</c>, which the browser only reports as
/// anything other than zero on a page whose viewport meta tag carries <c>viewport-fit=cover</c>.
/// <br />
/// What it measures is published on its root as CSS variables, so a page can lay its own chrome out
/// against the same numbers: <c>--bit-ash-inset-top</c>, <c>--bit-ash-inset-bottom</c>,
/// <c>--bit-ash-inset-start</c> and <c>--bit-ash-inset-end</c> for the four safe areas, and
/// <c>--bit-ash-keyboard-inset</c> for how much of the shell the on-screen keyboard is covering.
/// </remarks>
[SuppressMessage("Trimming", "IL2110:Field with 'DynamicallyAccessedMembersAttribute' is accessed via reflection. Trimmer can't guarantee availability of the requirements of the field.", Justification = "<Pending>")]
public partial class BitAppShell : BitComponentBase
{
    /// <summary>
    /// The name the app shell cascades the element of its main (scrolling) container under, so that the
    /// components which have to hold a scroller - Modal, Panel, Dialog, Overlay - can find the one the
    /// application actually scrolls in without being handed it.
    /// </summary>
    public const string Container = "BitAppShell.Container";

    /// <summary>
    /// The id the main container of the app shell carries.
    /// </summary>
    /// <remarks>
    /// It is well known rather than unique because it is read before Blazor has started, by the script
    /// that keeps the scrolling the reader did on the server-rendered shell. Only one element of a page
    /// can carry an id, so the rare page that renders more than one app shell gives the extra ones an
    /// <c>Id</c> of their own, off which their container ids are derived.
    /// </remarks>
    public const string ContainerId = "BitAppShell-container";



    private bool _subscribed;
    private bool _scrollInit;
    private bool _paneSetup;
    private bool _keyboardSetup;
    private bool _locationChanged;
    private string? _lastLocation;
    private ElementReference? _containerRef;
    private BitScrollablePaneOptions? _paneOptions;
    private DotNetObjectReference<BitAppShell>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private NavigationManager _navManager { get; set; } = default!;



    /// <summary>
    /// Enables auto-scroll to the top of the main container on navigation.
    /// </summary>
    /// <remarks>
    /// A navigation that only changes the fragment of the url - an in-page anchor - is left alone, since
    /// scrolling to the top is the opposite of what following an anchor asks for.
    /// <see cref="PersistScroll"/> takes precedence over this.
    /// </remarks>
    [Parameter] public bool AutoGoToTop { get; set; }

    /// <summary>
    /// Keeps the main container of the app shell pinned to the end of its content as the content grows.
    /// </summary>
    /// <remarks>
    /// This is what the chat, the log or the console of a shell wants: the newest content is at the end and
    /// the shell stays there on its own. It pins the container as soon as it is turned on, and after that
    /// only while the reader left it standing at the end - a reader who scrolled up to read something is
    /// not dragged back down by the next arrival, and scrolling back to the end takes the pinning up again.
    /// <see cref="AutoScrollThreshold"/> decides how near the end still counts as being at it, and
    /// <see cref="ScrollBehavior"/> how the moves it makes are animated.
    /// </remarks>
    [Parameter] public bool AutoScroll { get; set; }

    /// <summary>
    /// How near the end of the content (in pixels) the main container has to have been left for
    /// <see cref="AutoScroll"/> to keep pinning it there.
    /// </summary>
    /// <remarks>
    /// The default of 0 asks the reader to be at the very end, which is the strictest reading and the one a
    /// chat usually wants. A larger value keeps the pinning going while they are within that many pixels of
    /// it, so a line or two of slack does not count as having scrolled away.
    /// </remarks>
    [Parameter] public int AutoScrollThreshold { get; set; }

    /// <summary>
    /// Takes the height of the on-screen keyboard off the scrolling area of the app shell while it is
    /// open, so the content is laid out in the room that is actually left rather than behind it.
    /// </summary>
    /// <remarks>
    /// Opening the keyboard leaves the layout viewport - and therefore every percentage height on the
    /// page - exactly as it was on the platforms that need this, so a shell that is not told about it
    /// goes on believing it owns a screen whose bottom is now covered. With this on, the shell publishes
    /// the covered height on its root as the <c>--bit-ash-keyboard-inset</c> CSS variable and takes it off
    /// the height of the middle, which is what brings a bottom bar declared inside the shell back above
    /// the keyboard. The bottom safe area inset is given up for as long as the keyboard is open, since what
    /// that inset keeps clear is covered by the keyboard anyway and a band of background above it would be
    /// room taken from the content for nothing.
    /// <br />
    /// The root is also marked with the <c>data-bit-ash-keyboard</c> attribute while the keyboard is up -
    /// which is what the chrome that hides itself while the reader is typing can be styled against - and
    /// the same measurement is reported to <see cref="OnKeyboardInsetChanged"/> for whatever cannot be
    /// placed in CSS alone.
    /// <br />
    /// It reports 0 - and so does nothing - wherever the browser shrinks the layout viewport itself, which
    /// is every desktop browser and any page asking for <c>interactive-widget=resizes-content</c>, and it
    /// leaves the measurement alone while the page is pinch-zoomed, since a zoomed page shrinks its visual
    /// viewport in exactly the way an open keyboard does.
    /// </remarks>
    [Parameter] public bool AvoidKeyboard { get; set; }

    /// <summary>
    /// The content of the app shell.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the app shell.
    /// </summary>
    [Parameter] public BitAppShellClassStyles? Classes { get; set; }

    /// <summary>
    /// Pins the app shell to the four edges of the screen, so that it fills the window whatever height the
    /// page around it has.
    /// </summary>
    /// <remarks>
    /// The app shell otherwise fills the room it is given, which means the <c>html</c> and the <c>body</c>
    /// of the host page have to be given a height of their own for it to have any - the commonest reason a
    /// shell is reported as having no height at all. This takes the page out of the question: the shell is
    /// positioned against the viewport instead, which is what the shell of an application wants anyway,
    /// since nothing is meant to be laid out around it or scrolled past it.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool FullScreen { get; set; }

    /// <summary>
    /// Reserves the room the scrollbar of the main container takes, whether or not there is anything left
    /// to scroll.
    /// </summary>
    /// <remarks>
    /// This is the CSS <c>scrollbar-gutter</c> property, and the one scroller of an application is where it
    /// earns its keep: without it, every navigation between a page long enough to scroll and a page that is
    /// not moves the whole layout sideways by the width of a scrollbar. It costs nothing at all where the
    /// platform draws its scrollbars over the content, which is every mobile browser.
    /// </remarks>
    [Parameter] public BitScrollbarGutter? Gutter { get; set; }

    /// <summary>
    /// Removes the bottom safe area inset of the app shell, leaving the other three where they are.
    /// </summary>
    /// <remarks>
    /// For the application that insets that one edge itself - a bottom bar padding itself by the inset so
    /// that its background paints behind the home indicator, in a shell that still keeps the status bar and
    /// the rounded sides clear. <see cref="NoInsets"/> is the same thing for all four edges at once.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool NoBottomInset { get; set; }

    /// <summary>
    /// Removes the trailing side safe area inset of the app shell - the one on the right of a left-to-right
    /// shell and on the left of a right-to-left one - leaving the other three where they are.
    /// See <see cref="NoBottomInset"/>.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool NoEndInset { get; set; }

    /// <summary>
    /// Removes the safe area insets, so the four edges of the app shell are not inset at all and the
    /// content fills the whole screen.
    /// </summary>
    /// <remarks>
    /// For an application that insets its own edges - a header that pads itself by the top inset so that
    /// its background paints behind the status bar - which is what an edge-to-edge layout asks for.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool NoInsets { get; set; }

    /// <summary>
    /// Prevents the reader from scrolling the main container of the app shell at all.
    /// </summary>
    /// <remarks>
    /// The content that overflows is clipped rather than reachable. The scrolling API of this component
    /// still moves the container, since <c>overflow: hidden</c> only stops the reader's own gestures.
    /// </remarks>
    [Parameter] public bool NoScroll { get; set; }

    /// <summary>
    /// Removes the leading side safe area inset of the app shell - the one on the left of a left-to-right
    /// shell and on the right of a right-to-left one - leaving the other three where they are.
    /// See <see cref="NoBottomInset"/>.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool NoStartInset { get; set; }

    /// <summary>
    /// Removes the top safe area inset of the app shell, leaving the other three where they are.
    /// </summary>
    /// <remarks>
    /// For the application that insets that one edge itself - a header padding itself by the inset so that
    /// its background paints behind the status bar, in a shell that still keeps the home indicator clear,
    /// which is the commonest half of an edge-to-edge layout. See <see cref="NoBottomInset"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool NoTopInset { get; set; }

    /// <summary>
    /// Callback for how much of the app shell the on-screen keyboard covers, in pixels, raised as that
    /// changes and with 0 as the keyboard closes.
    /// </summary>
    /// <remarks>
    /// It is what <see cref="AvoidKeyboard"/> publishes as the <c>--bit-ash-keyboard-inset</c> CSS
    /// variable, handed to the page as a number as well so the chrome that cannot be placed in CSS alone -
    /// a map to re-center, a list to keep the selected row of in view - can be moved with it. Nothing is
    /// measured, and so nothing is reported, on a shell that has not asked to avoid the keyboard.
    /// </remarks>
    [Parameter] public EventCallback<double> OnKeyboardInsetChanged { get; set; }

    /// <summary>
    /// Callback for when the main container of the app shell reaches the bottom of its content.
    /// </summary>
    /// <remarks>
    /// It is raised once per arrival rather than on every frame that stays there, and how near the bottom
    /// counts as having reached it is <see cref="ReachOffset"/>.
    /// </remarks>
    [Parameter] public EventCallback OnReachedBottom { get; set; }
    /// <summary>
    /// Callback for when the main container of the app shell reaches the visual left edge of its content.
    /// See <see cref="OnReachedBottom"/>.
    /// </summary>
    /// <remarks>
    /// The edge is the one on the screen rather than the one in reading order, so it is the same edge in a
    /// right-to-left shell.
    /// </remarks>
    [Parameter] public EventCallback OnReachedLeft { get; set; }

    /// <summary>
    /// Callback for when the main container of the app shell reaches the visual right edge of its content.
    /// See <see cref="OnReachedLeft"/>.
    /// </summary>
    [Parameter] public EventCallback OnReachedRight { get; set; }


    /// <summary>
    /// Callback for when the main container of the app shell reaches the top of its content.
    /// See <see cref="OnReachedBottom"/>.
    /// </summary>
    [Parameter] public EventCallback OnReachedTop { get; set; }

    /// <summary>
    /// Callback for the scroll position of the main container of the app shell, raised as it is scrolled.
    /// </summary>
    /// <remarks>
    /// Reporting costs a listener and a measurement per frame of every scroll, so nothing is reported at
    /// all until one of the scroll callbacks is given a handler. Use <see cref="ScrollThrottle"/> to
    /// report less often than once per frame.
    /// </remarks>
    [Parameter] public EventCallback<BitScrollOffset> OnScroll { get; set; }

    /// <summary>
    /// Callback for when a scroll of the main container of the app shell comes to a stop.
    /// </summary>
    [Parameter] public EventCallback<BitScrollOffset> OnScrollEnd { get; set; }

    /// <summary>
    /// Callback for when a scroll of the main container of the app shell begins.
    /// </summary>
    [Parameter] public EventCallback<BitScrollOffset> OnScrollStart { get; set; }

    /// <summary>
    /// What the main container of the app shell does with content that overflows it sideways.
    /// </summary>
    /// <remarks>
    /// The main container scrolls along both axes, which is what an application laying a page out wider
    /// than the screen needs. Set this to <see cref="BitOverflow.Hidden"/> to clip that overflow instead,
    /// so the one element a few pixels too wide cannot leave the whole application scrollable sideways -
    /// the commonest layout bug of a mobile web app. <see cref="NoScroll"/> takes both axes away at once
    /// and wins over this.
    /// </remarks>
    [Parameter] public BitOverflow? OverflowX { get; set; }

    /// <summary>
    /// What the main container of the app shell does with content that overflows it downwards.
    /// See <see cref="OverflowX"/>.
    /// </summary>
    [Parameter] public BitOverflow? OverflowY { get; set; }

    /// <summary>
    /// Determines what happens when the main container of the app shell is scrolled past its edge.
    /// </summary>
    /// <remarks>
    /// Left unset, the app shell behaves as <see cref="BitOverscroll.None"/>, which is what an
    /// application-like layout wants: no scroll chaining out of the shell, and no pull-to-refresh or
    /// rubber-banding of the page behind it. Set it to <see cref="BitOverscroll.Auto"/> to give the
    /// platform's own overscroll affordances back to a shell that is really a web page.
    /// </remarks>
    [Parameter] public BitOverscroll? Overscroll { get; set; }

    /// <summary>
    /// Persists scroll position of the main container and restores it on navigation.
    /// </summary>
    /// <remarks>
    /// The positions are kept per url in session storage, so they survive a reload and are gone when the
    /// tab is - <see cref="ClearPersistedScroll"/> forgets them sooner. A navigation that only changes the
    /// fragment of the url is left alone, so an in-page anchor still works. It takes precedence over
    /// <see cref="AutoGoToTop"/>.
    /// <br />
    /// The store belongs to the page rather than to this component, so it is the one app shell of an
    /// application that is meant to ask for it: a second shell doing so on the same page would take the
    /// store over from the first.
    /// </remarks>
    [Parameter] public bool PersistScroll { get; set; }
    /// <summary>
    /// Keeps the reader's place when content is added above what they are looking at.
    /// </summary>
    /// <remarks>
    /// This is the other half of an endless list: a page of older messages arriving at the top of a
    /// conversation is as tall as the messages in it, and without this it pushes what the reader was
    /// reading that far down the screen. With it the container is moved down by exactly what arrived, so
    /// what they were looking at does not move at all.
    /// <br />
    /// Every engine but WebKit already does this on its own (it is the CSS <c>overflow-anchor</c>
    /// behavior), so this changes nothing where the browser is anchoring the container and brings the rest
    /// - Safari, most of all - up to the same behavior. It is worth pairing with <see cref="OnReachedTop"/>
    /// and a <see cref="ReachOffset"/> of about a screenful, so the fetch starts before the reader is at
    /// the top rather than once they are.
    /// </remarks>
    [Parameter] public bool PreserveScroll { get; set; }


    /// <summary>
    /// How near an edge (in pixels) counts as having reached it, for <see cref="OnReachedTop"/> and
    /// <see cref="OnReachedBottom"/>.
    /// </summary>
    [Parameter] public int ReachOffset { get; set; }

    /// <summary>
    /// The scroll behavior of the main container of the app shell.
    /// </summary>
    /// <remarks>
    /// It decides how every move the container is not dragged to by the reader is made: the scrolling API
    /// of this component, a fragment navigation into it, and the browser bringing a focused element into
    /// view. The default is <see cref="BitScrollBehavior.Smooth"/>, which is taken back off on its own
    /// under the reduced motion preference unless <c>ForceAnimation</c> is set.
    /// </remarks>
    [Parameter] public BitScrollBehavior? ScrollBehavior { get; set; }
    /// <summary>
    /// The room the main container of the app shell keeps between its edges and anything scrolled into
    /// view inside it, as any CSS length.
    /// </summary>
    /// <remarks>
    /// This is the CSS <c>scroll-padding</c> property, and it is what keeps a header stuck to the top of
    /// the shell from covering whatever was just scrolled to - by a fragment navigation, by the browser
    /// bringing a focused field into view, or by <see cref="ScrollToElement"/>, which reads it as well so
    /// that the moves this component makes leave the same room the browser's own do. The other scrolling
    /// methods take an absolute position or a distance and are left alone by it.
    /// <br />
    /// A shell whose header is the height of the top safe area plus a bar of its own can say so:
    /// <c>ScrollPadding="calc(var(--bit-ash-inset-top) + 3rem) 0 0 0"</c>.
    /// </remarks>
    [Parameter] public string? ScrollPadding { get; set; }


    /// <summary>
    /// The shortest interval (in milliseconds) between two <see cref="OnScroll"/> reports.
    /// The default of 0 reports once per animation frame.
    /// </summary>
    [Parameter] public int ScrollThrottle { get; set; }

    /// <summary>
    /// Sizes the four inset bars from the largest safe areas the device can ask for rather than from the
    /// ones it is asking for right now, so the layout is not relaid out as the browser's own chrome slides
    /// in and out.
    /// </summary>
    /// <remarks>
    /// The insets a browser reports are not constants: an edge-to-edge Chrome on Android retracts its
    /// bottom bar as the reader scrolls down and brings it back on the way up, and the bottom inset follows
    /// it the whole way - so the bar sized from it, and everything laid out against it, is moved on every
    /// frame of that slide. With this on the shell is sized from the static maximums instead
    /// (<c>env(safe-area-max-inset-*)</c>): the layout is laid out once, for the room left when nothing is
    /// retracted, and the browser slides its own chrome over the background of an inset bar rather than
    /// over the content. It costs that much room on the screen for as long as the chrome is retracted,
    /// which is the trade being made.
    /// <br />
    /// A browser that does not report the maximums - which is every one but Chromium 135 and later, and
    /// every platform whose insets do not move in the first place - is left reading the insets it does
    /// report, so this changes nothing there.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool StableInsets { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the app shell.
    /// </summary>
    [Parameter] public BitAppShellClassStyles? Styles { get; set; }

    /// <summary>
    /// The cascading value list to be provided for the children of the app shell.
    /// </summary>
    [Parameter] public BitCascadingValueList? ValueList { get; set; }

    /// <summary>
    /// The cascading values to be provided for the children of the app shell.
    /// </summary>
    [Parameter] public IEnumerable<BitCascadingValue>? Values { get; set; }



    /// <summary>
    /// The element reference to the main container of the app shell.
    /// </summary>
    public ElementReference? ContainerRef => _containerRef;

    /// <summary>
    /// The id of the main container element of this app shell.
    /// </summary>
    /// <remarks>
    /// It is <see cref="ContainerId"/> unless an <c>Id</c> was given to the app shell, in which case it
    /// is that id with <c>-container</c> after it - which is how a page that renders more than one app
    /// shell keeps each container id to itself.
    /// </remarks>
    public string MainContainerId => Id.HasValue() ? $"{Id}-container" : ContainerId;



    /// <summary>
    /// Scrolls the main container to top.
    /// </summary>
    /// <param name="behavior">
    /// How the move is made. When it is not given, the <see cref="ScrollBehavior"/> of the app shell
    /// decides, which honors the reduced motion preference.
    /// </param>
    public async Task GoToTop(BitScrollBehavior? behavior = null)
    {
        if (_containerRef.HasValue is false) return;

        await InvokeJs(() => _js.BitExtrasGoToTop(_containerRef!.Value, behavior));
    }

    /// <summary>
    /// Scrolls the main container to the bottom of its content.
    /// </summary>
    /// <param name="behavior">See <see cref="GoToTop"/>.</param>
    public async Task GoToBottom(BitScrollBehavior? behavior = null)
    {
        if (_containerRef.HasValue is false) return;

        await InvokeJs(() => _js.BitExtrasGoToBottom(_containerRef!.Value, behavior));
    }

    /// <summary>
    /// Scrolls the main container to a position.
    /// </summary>
    /// <param name="left">The horizontal position, or null to leave that axis where it stands.</param>
    /// <param name="top">The vertical position, or null to leave that axis where it stands.</param>
    /// <param name="behavior">See <see cref="GoToTop"/>.</param>
    public async Task ScrollTo(double? left, double? top, BitScrollBehavior? behavior = null)
    {
        if (_containerRef.HasValue is false) return;

        await InvokeJs(() => _js.BitExtrasScrollTo(_containerRef!.Value, left, top, behavior));
    }

    /// <summary>
    /// Scrolls the main container by an amount, from wherever it currently stands.
    /// </summary>
    /// <param name="x">How far to move sideways, in pixels.</param>
    /// <param name="y">How far to move up or down, in pixels.</param>
    /// <param name="behavior">See <see cref="GoToTop"/>.</param>
    public async Task ScrollBy(double x, double y, BitScrollBehavior? behavior = null)
    {
        if (_containerRef.HasValue is false) return;

        await InvokeJs(() => _js.BitExtrasScrollBy(_containerRef!.Value, (decimal)x, (decimal)y, behavior));
    }

    /// <summary>
    /// Brings an element inside the main container into view by scrolling the container itself.
    /// </summary>
    /// <param name="elementId">The id of the element to scroll to.</param>
    /// <param name="offset">How much room (in pixels) to leave above it, for a sticky header of the page.</param>
    /// <param name="smooth">Whether the move is animated. Honors the reduced motion preference.</param>
    /// <param name="alignment">Where in the container the element comes to rest.</param>
    /// <param name="behavior">
    /// How the move is made, which wins over <paramref name="smooth"/> where both are given. When it is
    /// not given either, <paramref name="smooth"/> decides.
    /// </param>
    public async Task ScrollToElement(string elementId,
                                      double offset = 0,
                                      bool smooth = true,
                                      BitScrollAlignment alignment = BitScrollAlignment.Start,
                                      BitScrollBehavior? behavior = null)
    {
        if (_containerRef.HasValue is false || elementId.HasNoValue()) return;

        // The behavior is the same argument every other move of this component takes, and it wins over the
        // older flag beside it where both are given. Left out, the flag decides, so a call written before
        // there was a behavior to pass still means what it did.
        var animated = behavior switch
        {
            BitScrollBehavior.Smooth => true,
            BitScrollBehavior.Instant => false,
            BitScrollBehavior.Auto => ScrollBehavior is null or BitScrollBehavior.Smooth,
            _ => smooth
        };

        await InvokeJs(() => _js.BitScrollablePaneScrollToElement(_containerRef!.Value,
                                                                  elementId,
                                                                  offset,
                                                                  animated,
                                                                  alignment.ToString().ToLowerInvariant()));
    }

    /// <summary>
    /// Re-measures the main container of the app shell and reports whatever has changed since it was last
    /// measured.
    /// </summary>
    /// <remarks>
    /// The container watches both its own size and its content on its own, so this is only for the changes
    /// neither of those can see - a web font that finished loading, an image that settled at a size the
    /// markup never named - after which the edge callbacks and the pinning of <see cref="AutoScroll"/> are
    /// brought back up to date. It does nothing on a shell that asked for none of them, since such a shell
    /// has no browser side to bring up to date.
    /// </remarks>
    public async Task Refresh()
    {
        if (_paneSetup is false) return;

        await InvokeJs(() => _js.BitScrollablePaneRefresh(UniqueId));
    }

    /// <summary>
    /// Reads where the main container currently stands, measured in the browser.
    /// </summary>
    /// <remarks>
    /// It is a read of the element at the moment it is asked for, so it needs none of the scroll
    /// callbacks to have been given a handler.
    /// </remarks>
    public async Task<BitScrollOffset?> GetScrollOffset()
    {
        if (_containerRef.HasValue is false) return null;

        try
        {
            return await _js.BitScrollablePaneGetOffset(_containerRef.Value);
        }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
    }

    /// <summary>
    /// Forgets every scroll position <see cref="PersistScroll"/> has kept, for the pages of this app
    /// shell and of any other.
    /// </summary>
    /// <remarks>
    /// The positions are kept for the whole session, so an application that signs a user out calls this
    /// to keep the next one from being put back where the previous one was. Given a url, only that one page
    /// is forgotten, which is what a page whose content has been replaced under the reader - a list that was
    /// filtered, a search that was run again - wants, since the position it was left at no longer points at
    /// anything.
    /// </remarks>
    /// <param name="url">
    /// The url to forget, exactly as <c>NavigationManager.Uri</c> reports it. When it is not given, every
    /// position is forgotten.
    /// </param>
    public async Task ClearPersistedScroll(string? url = null)
    {
        await InvokeJs(() => _js.BitAppShellClearScrolls(url));
    }



    protected override string RootElementClass => "bit-ash";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // The insets are the four bars around the main container, so the flag that removes them belongs
        // on the root all four of them are sized from.
        ClassBuilder.Register(() => NoInsets ? "bit-ash-nin" : string.Empty);

        // And each of the four can be taken back to zero on its own, for the application that insets that
        // one edge itself: a header painting behind the status bar over a shell that still keeps the home
        // indicator clear is an edge-to-edge layout of the top edge alone. The two side ones are the
        // LOGICAL edges, so each of them stays with the reading direction the bars are laid out in.
        ClassBuilder.Register(() => FullScreen ? "bit-ash-fsc" : string.Empty);

        // Written before the flags that take an inset away, and the stylesheet keeps them in that order,
        // so a shell asking for both is left with the edge removed rather than with the largest inset the
        // device can ask for.
        ClassBuilder.Register(() => StableInsets ? "bit-ash-sin" : string.Empty);

        ClassBuilder.Register(() => NoTopInset ? "bit-ash-nit" : string.Empty);
        ClassBuilder.Register(() => NoBottomInset ? "bit-ash-nib" : string.Empty);
        ClassBuilder.Register(() => NoStartInset ? "bit-ash-nis" : string.Empty);
        ClassBuilder.Register(() => NoEndInset ? "bit-ash-nie" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    // The class and the style of the main container, which is the element that actually scrolls - so
    // everything about the scrolling of the app shell lands here rather than on the root.
    private string _MainClass => string.Join(' ', new[]
    {
        "bit-ash-main",
        // Smooth is the default of the app shell, so the class is on unless another behavior was asked
        // for. The stylesheet takes it back off under the reduced motion preference; a behavior handed to
        // one of the scrolling methods overrides the property either way, which is why the methods that
        // are called without one pass nothing at all and let this property decide.
        ScrollBehavior is null or BitScrollBehavior.Smooth ? "bit-ash-smt" : null,
        NoScroll ? "bit-ash-nsc" : null,
        Classes?.Main
    }.Where(c => c.HasValue()));

    private string? _MainStyle
    {
        get
        {
            // Everything the scrolling container is styled with lands in this one attribute, in the order
            // the declarations override one another in: whatever the page wrote first, then the ones this
            // component derives from its parameters, and NoScroll over all of it - so a shell the reader
            // is not to be able to move is never left movable by an overflow the page happened to write.
            List<string?> declarations =
            [
                Styles?.Main?.TrimEnd().TrimEnd(';'),

                Overscroll switch
                {
                    BitOverscroll.Auto => "overscroll-behavior:auto",
                    BitOverscroll.Contain => "overscroll-behavior:contain",
                    BitOverscroll.None => "overscroll-behavior:none",
                    _ => null
                },

                // Each axis is spelled out as its own longhand rather than folded into the shorthand,
                // which would also reset the axis it was not asked about.
                OverflowX switch
                {
                    BitOverflow.Auto => "overflow-x:auto",
                    BitOverflow.Hidden => "overflow-x:hidden",
                    BitOverflow.Scroll => "overflow-x:scroll",
                    BitOverflow.Visible => "overflow-x:visible",
                    _ => null
                },

                OverflowY switch
                {
                    BitOverflow.Auto => "overflow-y:auto",
                    BitOverflow.Hidden => "overflow-y:hidden",
                    BitOverflow.Scroll => "overflow-y:scroll",
                    BitOverflow.Visible => "overflow-y:visible",
                    _ => null
                },

                // Auto is the initial value, which the container already has.
                Gutter switch
                {
                    BitScrollbarGutter.Stable => "scrollbar-gutter:stable",
                    BitScrollbarGutter.BothEdges => "scrollbar-gutter:stable both-edges",
                    _ => null
                },

                ScrollPadding.HasValue() ? $"scroll-padding:{ScrollPadding}" : null,

                // The class of the container says this as well, but a style attribute wins over a class:
                // an overflow written into Styles.Main, or an axis asked for above, would otherwise leave
                // the container scrollable after all.
                NoScroll ? "overflow:hidden" : null,
            ];

            // Two declarations landing in the same style attribute are only two declarations while a
            // semicolon stands between them.
            var style = string.Join(';', declarations.Where(d => d.HasValue()));

            return style.HasValue() ? style : null;
        }
    }

    protected override void OnParametersSet()
    {
        // The two navigation features are parameters like any other, so turning either of them on after
        // the shell has been rendered has to subscribe it - and turning both of them off, unsubscribe it.
        UpdateSubscription();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // The container element does not exist while the first render is being built, so the cascade of
            // it published nothing on that render. Anything that came looking for the shell's scroller
            // before the shell happened to render again - a Modal opened straight off the landing page -
            // fell back to holding a body that a shell app never scrolls, and the region behind it went on
            // scrolling. One more render is what puts the element into the cascade.
            StateHasChanged();
        }

        await SetupPersistScroll();

        await SetupScrollReporting();

        await SetupKeyboard();

        if (_locationChanged && firstRender is false)
        {
            _locationChanged = false;
            await InvokeJs(() => _js.BitAppShellAfterRenderScroll(_navManager.Uri));
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    // The scroll persistence of the browser side, followed as the parameter flips rather than settled on
    // the first render: turning it on later starts it, and turning it off again stops it rather than
    // leaving a listener behind that goes on writing positions nothing will ever restore.
    private async Task SetupPersistScroll()
    {
        if (PersistScroll == _scrollInit) return;

        if (PersistScroll)
        {
            if (_containerRef.HasValue is false) return;

            _scrollInit = true;

            await InvokeJs(() => _js.BitAppShellInitScroll(_containerRef.Value, _navManager.Uri));
            return;
        }

        _scrollInit = false;
        _locationChanged = false;

        await InvokeJs(() => _js.BitAppShellDisposeScroll());
    }

    // The keyboard tracking, followed as the parameter flips rather than settled on the first render, so
    // that a page which only avoids the keyboard on the screens that have a field on them can turn it on
    // and off. It costs two listeners on the visual viewport, which is why it is not simply always on.
    private async Task SetupKeyboard()
    {
        if (AvoidKeyboard == _keyboardSetup) return;

        _keyboardSetup = AvoidKeyboard;

        if (AvoidKeyboard is false)
        {
            await InvokeJs(() => _js.BitAppShellDisposeKeyboard(UniqueId));
            return;
        }

        // The reference is handed over whether or not anything is listening for the measurement, since a
        // page that starts listening later would otherwise have to make the shell set its tracking up
        // again to be heard; the browser side only calls back when the measurement CHANGES, which is once
        // or twice per keyboard rather than per frame.
        _dotnetObj ??= DotNetObjectReference.Create(this);

        await InvokeJs(() => _js.BitAppShellSetupKeyboard(UniqueId, RootElement, _dotnetObj));
    }

    // The scroll reporting is driven by the very engine BitScrollablePane uses, so there is not a second
    // implementation of measuring, throttling and edge detection in the library. It is only ever set up
    // for a shell that asked for one of the reports, since it costs a listener and a measurement per
    // frame of every scroll.
    private async Task SetupScrollReporting()
    {
        if (_containerRef.HasValue is false) return;

        var options = BuildPaneOptions();

        if (options is null)
        {
            if (_paneSetup is false) return;

            _paneSetup = false;
            await InvokeJs(() => _js.BitScrollablePaneDispose(UniqueId));
            return;
        }

        if (_paneSetup)
        {
            // Nothing is sent for a set of options the browser side already has, so a shell that
            // re-renders on every navigation does not re-configure its scroller on every navigation.
            if (options == _paneOptions) return;

            _paneOptions = options;
            await InvokeJs(() => _js.BitScrollablePaneUpdate(UniqueId, options));
            return;
        }

        _paneSetup = true;
        _paneOptions = options;
        _dotnetObj ??= DotNetObjectReference.Create(this);

        await InvokeJs(() => _js.BitScrollablePaneSetup(UniqueId, _containerRef!.Value, _dotnetObj, options));
    }

    // What the browser side is driven with, or null when nothing has been asked of it at all. NoScroll is
    // in here because `overflow: hidden` only stops the reader's own gestures, and the engine has to know
    // not to move a container whose page alone decides where it stands.
    private BitScrollablePaneOptions? BuildPaneOptions()
    {
        var scroll = OnScroll.HasDelegate;
        var scrollStart = OnScrollStart.HasDelegate;
        var scrollEnd = OnScrollEnd.HasDelegate;
        var top = OnReachedTop.HasDelegate;
        var bottom = OnReachedBottom.HasDelegate;
        var left = OnReachedLeft.HasDelegate;
        var right = OnReachedRight.HasDelegate;

        if (scroll is false && scrollStart is false && scrollEnd is false &&
            top is false && bottom is false && left is false && right is false &&
            NoScroll is false && AutoScroll is false && PreserveScroll is false) return null;

        return new()
        {
            Scroll = scroll,
            ScrollStart = scrollStart,
            ScrollEnd = scrollEnd,
            Top = top,
            Bottom = bottom,
            Left = left,
            Right = right,
            Offset = ReachOffset,
            Throttle = ScrollThrottle,
            NoScroll = NoScroll,
            AutoScroll = AutoScroll,
            AutoScrollThreshold = AutoScrollThreshold,
            Preserve = PreserveScroll,
            Smooth = ScrollBehavior is null or BitScrollBehavior.Smooth,
        };
    }

    private void UpdateSubscription()
    {
        var needed = AutoGoToTop || PersistScroll;

        if (needed == _subscribed) return;

        _subscribed = needed;

        if (needed)
        {
            // Where the application stands as the subscription is taken, so that the FIRST navigation
            // after it can be told apart from an in-page anchor as well as every one after that.
            _lastLocation = _navManager.Uri;

            _navManager.LocationChanged += LocationChanged;
        }
        else
        {
            _navManager.LocationChanged -= LocationChanged;
        }
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        // An in-page anchor is a navigation like any other as far as the NavigationManager is concerned,
        // and both features below would fight what following an anchor is asking for: one sends the
        // reader to the top of the page they just jumped into, the other to whatever position was stored
        // for a url key that has never been scrolled.
        if (TakeLocation(args.Location) is false) return;

        if (PersistScroll)
        {
            if (IsRendered is false) return;

            _locationChanged = true;

            _ = InvokeJs(() => _js.BitAppShellLocationChangedScroll());

            // The restore happens on the next render of this component. In its intended place - the
            // layout - the new page arrives as this component's own ChildContent, so that render is
            // guaranteed; asking for it here is what makes the restore work for a shell rendered anywhere
            // else too, instead of leaving its browser side with the scroll listener detached.
            // Through the dispatcher, since a navigation is not always raised on the renderer's thread -
            // a NavigateTo from a background task is not - and a render queued from another one throws.
            _ = InvokeAsync(StateHasChanged);
        }
        else if (AutoGoToTop)
        {
            _ = GoToTop(ScrollBehavior ?? BitScrollBehavior.Instant);
        }
    }

    // Remembers where the application now stands and answers whether that was a navigation to another
    // PAGE, which is the only kind either feature above acts on. A move that left everything but the
    // fragment of the url the same is an in-page anchor, and both of them would fight what following one
    // is asking for. The url the NavigationManager reports is absolute, so the two are compared with the
    // fragment cut off each of them.
    private bool TakeLocation(string location)
    {
        var previous = _lastLocation;

        _lastLocation = location;

        if (previous is null) return true;

        if (string.Equals(previous, location, StringComparison.Ordinal)) return true;

        return string.Equals(WithoutFragment(previous), WithoutFragment(location), StringComparison.Ordinal) is false;
    }

    private static string WithoutFragment(string url)
    {
        var index = url.IndexOf('#', StringComparison.Ordinal);

        return index < 0 ? url : url[..index];
    }

    // Every call out to the browser goes through here, so that the two failures none of them can do
    // anything about - a circuit that dropped, and a runtime already torn down - are never surfaced as an
    // unhandled exception of an application that did nothing wrong.
    private async Task InvokeJs(Func<ValueTask> action)
    {
        try
        {
            await action();
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }



    [JSInvokable("OnScroll")]
    public async Task _OnScroll(BitScrollOffset offset)
    {
        if (IsDisposed || offset is null) return;

        await OnScroll.InvokeAsync(offset);
    }

    [JSInvokable("OnScrollStart")]
    public async Task _OnScrollStart(BitScrollOffset offset)
    {
        if (IsDisposed || offset is null) return;

        await OnScrollStart.InvokeAsync(offset);
    }

    [JSInvokable("OnScrollEnd")]
    public async Task _OnScrollEnd(BitScrollOffset offset)
    {
        if (IsDisposed || offset is null) return;

        await OnScrollEnd.InvokeAsync(offset);
    }

    [JSInvokable("OnReached")]
    public async Task _OnReached(string edge)
    {
        if (IsDisposed) return;

        var callback = edge switch
        {
            "top" => OnReachedTop,
            "bottom" => OnReachedBottom,
            "left" => OnReachedLeft,
            "right" => OnReachedRight,
            _ => default
        };

        if (callback.HasDelegate is false) return;

        await callback.InvokeAsync();
    }

    [JSInvokable("OnKeyboardInset")]
    public async Task _OnKeyboardInset(double inset)
    {
        if (IsDisposed || OnKeyboardInsetChanged.HasDelegate is false) return;

        await OnKeyboardInsetChanged.InvokeAsync(inset);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _navManager.LocationChanged -= LocationChanged;
        _subscribed = false;

        if (_scrollInit)
        {
            await InvokeJs(() => _js.BitAppShellDisposeScroll());
        }

        if (_paneSetup)
        {
            await InvokeJs(() => _js.BitScrollablePaneDispose(UniqueId));
        }

        if (_keyboardSetup)
        {
            await InvokeJs(() => _js.BitAppShellDisposeKeyboard(UniqueId));
        }

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await base.DisposeAsync(disposing);
    }
}
