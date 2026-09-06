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
    /// Takes the height of the on-screen keyboard off the scrolling area of the app shell while it is
    /// open, so the content is laid out in the room that is actually left rather than behind it.
    /// </summary>
    /// <remarks>
    /// Opening the keyboard leaves the layout viewport - and therefore every percentage height on the
    /// page - exactly as it was on the platforms that need this, so a shell that is not told about it
    /// goes on believing it owns a screen whose bottom is now covered. With this on, the shell publishes
    /// the covered height on its root as the <c>--bit-ash-keyboard-inset</c> CSS variable and takes it off
    /// the height of the middle, which is what brings a bottom bar declared inside the shell back above
    /// the keyboard. It reports 0 - and so does nothing - wherever the browser shrinks the layout viewport
    /// itself, which is every desktop browser and any page asking for
    /// <c>interactive-widget=resizes-content</c>.
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
    /// Callback for when the main container of the app shell reaches the bottom of its content.
    /// </summary>
    /// <remarks>
    /// It is raised once per arrival rather than on every frame that stays there, and how near the bottom
    /// counts as having reached it is <see cref="ReachOffset"/>.
    /// </remarks>
    [Parameter] public EventCallback OnReachedBottom { get; set; }

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
    /// The shortest interval (in milliseconds) between two <see cref="OnScroll"/> reports.
    /// The default of 0 reports once per animation frame.
    /// </summary>
    [Parameter] public int ScrollThrottle { get; set; }

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
    public async Task ScrollToElement(string elementId,
                                      double offset = 0,
                                      bool smooth = true,
                                      BitScrollAlignment alignment = BitScrollAlignment.Start)
    {
        if (_containerRef.HasValue is false || elementId.HasNoValue()) return;

        await InvokeJs(() => _js.BitScrollablePaneScrollToElement(_containerRef!.Value,
                                                                  elementId,
                                                                  offset,
                                                                  smooth,
                                                                  alignment.ToString().ToLowerInvariant()));
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
    /// to keep the next one from being put back where the previous one was.
    /// </remarks>
    public async Task ClearPersistedScroll()
    {
        await InvokeJs(() => _js.BitAppShellClearScrolls());
    }



    protected override string RootElementClass => "bit-ash";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // The insets are the four bars around the main container, so the flag that removes them belongs
        // on the root all four of them are sized from.
        ClassBuilder.Register(() => NoInsets ? "bit-ash-nin" : string.Empty);
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
            var overscroll = Overscroll switch
            {
                BitOverscroll.Auto => "overscroll-behavior:auto",
                BitOverscroll.Contain => "overscroll-behavior:contain",
                BitOverscroll.None => "overscroll-behavior:none",
                _ => null
            };

            if (Styles?.Main.HasValue() is not true) return overscroll;

            if (overscroll is null) return Styles.Main;

            // Two declarations landing in the same style attribute are only two declarations while a
            // semicolon stands between them.
            return Styles.Main!.TrimEnd().EndsWith(';') ? $"{Styles.Main}{overscroll}" : $"{Styles.Main};{overscroll}";
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

        await InvokeJs(() => AvoidKeyboard
            ? _js.BitAppShellSetupKeyboard(UniqueId, RootElement)
            : _js.BitAppShellDisposeKeyboard(UniqueId));
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

        if (scroll is false && scrollStart is false && scrollEnd is false &&
            top is false && bottom is false && NoScroll is false) return null;

        return new()
        {
            Scroll = scroll,
            ScrollStart = scrollStart,
            ScrollEnd = scrollEnd,
            Top = top,
            Bottom = bottom,
            Offset = ReachOffset,
            Throttle = ScrollThrottle,
            NoScroll = NoScroll,
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
            _ => default
        };

        if (callback.HasDelegate is false) return;

        await callback.InvokeAsync();
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
