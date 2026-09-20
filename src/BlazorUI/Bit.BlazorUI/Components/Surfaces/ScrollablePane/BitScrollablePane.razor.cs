namespace Bit.BlazorUI;

/// <summary>
/// A ScrollablePane is a box that scrolls whatever is put in it, for content that does not fit in the room
/// the page has for it.
/// </summary>
/// <remarks>
/// The scrolling is the browser's own, so the pane keeps the momentum, the keyboard handling and the
/// assistive technology support of the platform it runs on and adds nothing between the reader and the
/// content. What it adds is everything around that: the size the box takes, which axes it scrolls on,
/// whether the scroll carries on into the page behind it, how the scrollbars look, a fade over the edges
/// that still have content beyond them, snap positions to come to rest on, a drag and a wheel that scroll
/// a strip sideways, a position reported back as it changes, a callback as each edge is reached, and a
/// scrolling API - <see cref="ScrollToEnd"/>, <see cref="ScrollToStart"/>, <see cref="ScrollTo"/>,
/// <see cref="ScrollBy"/>, <see cref="ScrollToElement"/> - that a page drives it with.
/// <br />
/// Nothing on the browser side is set up for a pane that asked for none of it: a pane that only sizes a box
/// and styles its scrollbars is a single div with a style attribute, and the scroll listener, the observers
/// and the round trips only appear once <see cref="AutoScroll"/>, <see cref="Fade"/>,
/// <see cref="DragScroll"/>, <see cref="HorizontalWheel"/>, <see cref="PreserveScroll"/>,
/// <see cref="OnScroll"/>, <see cref="OnScrollStart"/>, <see cref="OnScrollEnd"/> or one of the four edge
/// callbacks is used.
/// </remarks>
public partial class BitScrollablePane : BitComponentBase
{
    private bool _autoScrolled;
    private bool _jsSetupDone;
    private BitScrollablePaneOptions? _jsOptions;
    private DotNetObjectReference<BitScrollablePane>? _dotnetObj;

    // The key the browser side was registered under, kept rather than recomputed on every call. Id is a
    // parameter, so _Id is not the same string for the life of the component: a consumer that changes it
    // would otherwise leave every later call - and the teardown most of all - addressed to a key nothing
    // is registered under, silently doing nothing while the instance it meant to reach lived on.
    private string? _jsId;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    // A tabindex of the consumer's own also makes the pane focusable, so it is honored ahead of the
    // parameter that only asks for the default one. A disabled pane is taken back out of the tab order
    // rather than left in it as something that cannot be scrolled.
    private string? _tabIndex => (TabIndex ?? (Focusable ? "0" : null)) is string tabIndex
        ? (IsEnabled ? tabIndex : "-1")
        : null;

    // Whether anything the browser side does is asked for. Everything else this component offers is CSS,
    // so a pane that wants none of these never sets up a listener, an observer or a .NET object reference.
    //
    // What is worth WATCHING once there is an instance - the content, the pane, both - is deliberately not
    // decided here as well: the browser side works it out from the options it is handed, so there is one
    // definition of it rather than two that a new content-dependent feature could be added to only one of.
    private bool _needsJs => IsEnabled && (AutoScroll
                                           || DragScroll
                                           || HorizontalWheel
                                           || PreserveScroll
                                           || Fade
                                           || _autoHides
                                           || OnScroll.HasDelegate
                                           || OnScrollStart.HasDelegate
                                           || OnScrollEnd.HasDelegate
                                           || OnReachedTop.HasDelegate
                                           || OnReachedBottom.HasDelegate
                                           || OnReachedLeft.HasDelegate
                                           || OnReachedRight.HasDelegate);

    // Whether the browser side has a scrollbar to take out of sight. Only the Modern one is ever hidden,
    // since it is the only one the library draws, so the flag on its own is nothing for JavaScript to do.
    private bool _autoHides => Modern && AutoHideScrollbar;



    /// <summary>
    /// Keeps the pane pinned to the end of its content as the content grows.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is what a chat, a log or a console wants: the newest content is at the end and the pane stays
    /// there on its own. It pins the pane once as soon as it is turned on, and after that only while the
    /// reader left it standing at the end - a reader who scrolled up to look at something is not dragged
    /// back down by the next arrival, and scrolling back to the end takes the pinning up again.
    /// <see cref="AutoScrollThreshold"/> decides how near the end still counts as being at it.
    /// <br />
    /// Both axes are pinned, each one only while it has somewhere to go, and <see cref="Smooth"/> animates
    /// the moves it makes.
    /// </remarks>
    [Parameter] public bool AutoScroll { get; set; }

    /// <summary>
    /// How near the end of the content (in pixels) the pane has to have been left for <see cref="AutoScroll"/>
    /// to keep pinning it there.
    /// <br />
    /// The default value is <strong>0</strong>.
    /// </summary>
    /// <remarks>
    /// The default asks the reader to be at the very end, which is the strictest reading and the one a chat
    /// usually wants. A larger value keeps the pinning going while the reader is within that many pixels of
    /// the end, which is worth having where a line or two of slack should not count as having scrolled away.
    /// <br />
    /// A pane is always given a pixel of slack on top of this, since a scroll offset is fractional at a
    /// fractional zoom level and an exact comparison would fail there.
    /// </remarks>
    [Parameter] public int AutoScrollThreshold { get; set; }

    /// <summary>
    /// Makes the height of the pane auto.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoHeight { get; set; }

    /// <summary>
    /// Makes both height and width of the pane auto.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoSize { get; set; }

    /// <summary>
    /// Makes the width of the pane auto.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool AutoWidth { get; set; }

    /// <summary>
    /// Keeps the <see cref="Modern"/> scrollbar of the pane out of sight until the pointer is over it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the overlay behavior of a modern operating system, brought to a pane whose scrollbar the
    /// theme draws: the bar is there the moment it is wanted and takes no attention while it is not. It
    /// only applies to the scrollbar <see cref="Modern"/> draws, and the bar comes back for a pane that
    /// holds the focus as well as for one under the pointer, so it is never hidden from a keyboard reader.
    /// <br />
    /// A scrollbar that is not on the screen is not saying that there is more content, so it is worth
    /// pairing with <see cref="Fade"/>.
    /// <br />
    /// Which of the two the bar is showing for is decided in the browser rather than in the stylesheet,
    /// because Chromium does not repaint a custom scrollbar when the state of its element changes - so
    /// this needs the JavaScript of the library, and a pane rendered without it keeps its bar on screen.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool AutoHideScrollbar { get; set; }

    /// <summary>
    /// Alias for the ChildContent parameter.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The content of the pane, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Lets the pane be scrolled by dragging its content with a pointer.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is what a strip of cards, a board or a wide diagram wants: a mouse has no obvious way of
    /// scrolling sideways, and dragging the content is the way every map and every canvas on the web has
    /// answered that. A touch already drags, so this is only ever applied to a mouse or a pen.
    /// <br />
    /// The drag only starts once the pointer has moved a few pixels, so a click on something inside the
    /// pane is still a click, and the click that ends a real drag is swallowed rather than delivered to
    /// whatever happened to be under the pointer. Drags that start on a form control, a link or a button
    /// are left alone, as is anything marked <c>data-bit-scp-nodrag</c>, so selecting text in a field
    /// inside the pane still works.
    /// <br />
    /// <see cref="DragMomentum"/> is what makes a released drag carry on and slow to a stop instead of
    /// stopping dead with the button.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool DragScroll { get; set; }

    /// <summary>
    /// Lets a released drag carry on at the speed it was let go at and slow to a stop.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// It is what every scrolling surface of every platform does with a flick, and what a strip of cards
    /// dragged with a mouse is otherwise missing: without it a drag stops dead the moment the button comes
    /// up, so crossing a long strip is a row of separate drags rather than one throw of it.
    /// <br />
    /// It only applies to <see cref="DragScroll"/>, and only to a release that was actually moving: a
    /// pointer held still before it was lifted was not a flick, however fast the drag that led up to it
    /// was. Anything the reader does next - a new drag, a wheel that reaches an end - takes the pane back,
    /// and a pane whose <see cref="Snap"/> is <see cref="BitScrollSnap.Mandatory"/> is left to come to
    /// rest on an item its own way rather than being glided onto one.
    /// </remarks>
    [Parameter] public bool DragMomentum { get; set; }

    /// <summary>
    /// Fades out each edge of the pane that still has content beyond it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The fade is the one affordance a native scroll container does not give of its own accord: an
    /// overlay scrollbar disappears when it is not being used, which leaves a pane that is scrolled to the
    /// middle of its content looking exactly like one whose content ends at its edge. A faded edge says
    /// there is more that way, and it goes away as soon as that edge is reached.
    /// <br />
    /// All four edges are faded, each one only while the pane can still be scrolled towards it, so a pane
    /// that only scrolls up and down never fades its sides. <see cref="FadeSize"/> sets how far the fade
    /// reaches. It is drawn with a mask, so it works over any background and needs no element of its own
    /// inside a pane whose content the consumer owns - and browsers that cannot composite masks simply do
    /// not draw it.
    /// <br />
    /// A mask is drawn over the whole of the pane, which includes a classic scrollbar and anything held in
    /// place inside it with <c>position: sticky</c>: the ends of the bar and a sticky header sitting in a
    /// faded band are faded with the content behind them. That is what makes this worth pairing with a
    /// scrollbar that is not on the screen anyway - <see cref="ScrollbarWidth"/> of
    /// <see cref="BitScrollbarWidth.None"/>, or <see cref="Modern"/> with
    /// <see cref="AutoHideScrollbar"/> - which is the pairing the fade is for in the first place.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Fade { get; set; }

    /// <summary>
    /// How far the <see cref="Fade"/> reaches into the pane, as any CSS length.
    /// </summary>
    /// <remarks>
    /// The default is 2rem, which is roughly a line and a half of body text. It applies to all four edges.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? FadeSize { get; set; }

    /// <summary>
    /// Makes the height of the pane fit-content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitHeight { get; set; }

    /// <summary>
    /// Makes both height and width of the pane fit-content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitSize { get; set; }

    /// <summary>
    /// Makes the width of the pane fit-content.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// Puts the pane itself in the tab order, so it can be scrolled with the keyboard.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A scrolling box that holds nothing focusable is unreachable by keyboard: there is nothing inside it
    /// to tab to, and the arrow keys only scroll what has the focus. This puts the box itself in the tab
    /// order, which is what WCAG 2.1.1 asks of a scrolling region and what the "scrollable region must have
    /// keyboard access" rule checks for.
    /// <br />
    /// Turn it on for a pane of plain content - text, a table, an image - and leave it off for one that
    /// already holds links, buttons or fields, where it would only add a tab stop in front of them. A pane
    /// that is put in the tab order is worth naming with <see cref="BitComponentBase.AriaLabel"/>, and
    /// worth marking with a <see cref="Role"/> of <c>region</c> or <c>group</c> so what took the focus is
    /// announced as something rather than as an unnamed stop. Setting
    /// <see cref="BitComponentBase.TabIndex"/> makes the pane focusable on its own, without this.
    /// </remarks>
    [Parameter] public bool Focusable { get; set; }

    /// <summary>
    /// Makes the height of the pane 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes both height and width of the pane 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Makes the width of the pane 100%.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FullWidth { get; set; }

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
    /// Lays the content out on a single line that scrolls sideways instead of wrapping.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// It keeps the content from wrapping - the pane sets <c>white-space:nowrap</c>, which the content
    /// inherits - and turns the vertical scrolling off, which is what a strip of chips, a row of cards or a
    /// wide table wants. The layout of the content itself is left alone, so block level children still
    /// stack: lay those out with a flex or grid container of your own inside the pane.
    /// <br />
    /// <see cref="Overflow"/>, <see cref="OverflowX"/> and <see cref="OverflowY"/> still have the last
    /// word on either axis, so a horizontal pane that should also scroll down can say so.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// Turns a vertical wheel over a pane that only scrolls sideways into a sideways scroll.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A wheel mouse has no horizontal axis, so a strip that only scrolls sideways cannot be scrolled with
    /// it at all unless the vertical wheel is taken to mean the only axis the pane has. That is the whole
    /// of what this does, and it is deliberately narrow: it acts only while the pane has something to
    /// scroll sideways and nothing to scroll up and down, it leaves a wheel that already carries a
    /// horizontal delta - a trackpad, a tilt wheel - alone, and it hands the scroll back to the page as
    /// soon as the pane reaches the end it is being pushed towards, so a page is never left unscrollable
    /// under the pointer.
    /// </remarks>
    [Parameter] public bool HorizontalWheel { get; set; }

    /// <summary>
    /// Where the pane stands the first time it is rendered, measured from the visual left edge of the
    /// content in pixels.
    /// </summary>
    /// <remarks>
    /// This is what a list that is navigated back to, or a document a reader left in the middle of, opens
    /// at: hand back the <see cref="BitScrollOffset.OffsetLeft"/> that was saved and the pane starts
    /// there instead of at the start of its content. It is applied once, on the first render, so a later
    /// change to it does not move a pane the reader has since scrolled; <see cref="ScrollTo"/> is what
    /// moves a pane that is already on the screen.
    /// <br />
    /// It is not animated, whatever <see cref="Smooth"/> says - a pane that slid into place from the top
    /// as it appeared would be an animation nobody asked for - and it is left to
    /// <see cref="AutoScroll"/> where both are used, since a pane pinned to the end of its content has
    /// already been told where to open.
    /// </remarks>
    [Parameter] public double? InitialScrollLeft { get; set; }

    /// <summary>
    /// Where the pane stands the first time it is rendered, measured from the top of the content in
    /// pixels. See <see cref="InitialScrollLeft"/>.
    /// </summary>
    [Parameter] public double? InitialScrollTop { get; set; }

    /// <summary>
    /// The maximum height of the pane.
    /// </summary>
    /// <remarks>
    /// This is what makes a pane grow with its content and only start scrolling once there is too much of
    /// it, which is what a message list, a set of results or a menu of unknown length wants. Leave
    /// <see cref="Height"/> unset and give this one a value.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The maximum width of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MaxWidth { get; set; }

    /// <summary>
    /// The minimum height of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MinHeight { get; set; }

    /// <summary>
    /// The minimum width of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MinWidth { get; set; }

    /// <summary>
    /// Enables a modern style for the scrollbar of the pane.
    /// </summary>
    /// <remarks>
    /// It draws a thin rounded thumb over a transparent track, with no arrow buttons and no corner, in the
    /// colors of the theme - so the scrollbar of the pane belongs to the design system rather than to the
    /// operating system, and re-skins with it.
    /// <br />
    /// The thickness comes from the <c>--bit-scp-sbs</c> custom property and the three states of the thumb
    /// from <c>--bit-scp-sbc</c>, <c>--bit-scp-sbch</c> and <c>--bit-scp-sbca</c>, all of which can be
    /// retuned per pane through <see cref="BitComponentBase.Style"/>. Note that <see cref="ScrollbarWidth"/> and
    /// <see cref="ScrollbarColor"/> are the standard CSS properties, which Chromium lets win over the
    /// custom rendering this draws: use one or the other on a given pane, not both.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Modern { get; set; }

    /// <summary>
    /// Turns the scrolling of the pane off, without taking away anything else it does.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The content that does not fit is clipped and neither the wheel, a drag nor the keyboard moves it,
    /// while the content itself stays interactive and the scrolling API of the component keeps working -
    /// which is what a pane whose position is driven entirely from code wants. To take the interaction
    /// away as well, disable the pane with <see cref="BitComponentBase.IsEnabled"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool NoScroll { get; set; }

    /// <summary>
    /// Callback for when the pane has been scrolled to the bottom of its content.
    /// </summary>
    /// <remarks>
    /// It is called as the edge is reached rather than for as long as the pane stands at it, and it is
    /// armed again once the pane has left it, so coming to rest at the bottom is one call rather than one
    /// per frame of getting there. <see cref="ReachOffset"/> decides how near the edge counts as having
    /// reached it, which is what turns this into the "load the next page" trigger of an endless list.
    /// <br />
    /// Two things are never reported: the edges a pane is already standing at when it is first measured -
    /// every pane starts at the top, and a page told about that would fetch what comes before its first
    /// item before anything had been scrolled - and either edge of an axis with nothing to scroll, which
    /// stands at both of them at once.
    /// </remarks>
    [Parameter] public EventCallback OnReachedBottom { get; set; }

    /// <summary>
    /// Callback for when the pane has been scrolled to the visual left edge of its content.
    /// </summary>
    /// <remarks>
    /// The edges of the horizontal axis are the ones on the screen rather than the ones in reading order,
    /// so this is the left hand edge of a right-to-left pane as well. See <see cref="OnReachedBottom"/>
    /// for when the callbacks are made.
    /// </remarks>
    [Parameter] public EventCallback OnReachedLeft { get; set; }

    /// <summary>
    /// Callback for when the pane has been scrolled to the visual right edge of its content.
    /// </summary>
    /// <remarks>
    /// The edges of the horizontal axis are the ones on the screen rather than the ones in reading order,
    /// so this is the right hand edge of a right-to-left pane as well. See <see cref="OnReachedBottom"/>
    /// for when the callbacks are made.
    /// </remarks>
    [Parameter] public EventCallback OnReachedRight { get; set; }

    /// <summary>
    /// Callback for when the pane has been scrolled to the top of its content.
    /// </summary>
    /// <remarks>
    /// This is the other half of an endless list: a chat that loads the messages before the ones it is
    /// showing asks for them here, and <see cref="PreserveScroll"/> is what keeps the reader where they
    /// were once the messages arrive above them. See <see cref="OnReachedBottom"/> for when the callbacks
    /// are made.
    /// </remarks>
    [Parameter] public EventCallback OnReachedTop { get; set; }

    /// <summary>
    /// Callback for when the pane is scrolled, carrying where it now stands.
    /// </summary>
    /// <remarks>
    /// The position is measured in the browser and handed over as a <see cref="BitScrollOffset"/>, which
    /// carries both offsets, the size of the content, the size of the visible area, how far the pane moved
    /// to get here, and the answers derived from them - how far along each axis the pane is, which edges
    /// it is standing at, and which way it was last going.
    /// <br />
    /// It reports a position rather than an event, so it is also called when the position means something
    /// new without anything having been scrolled: the pane or its content changing size moves the end of
    /// the content and the answers derived from it, which is what lets one callback drive a progress bar,
    /// a set of arrows that light up while there is anywhere to go, and a header that folds away on the
    /// way down alike.
    /// <br />
    /// Reports are coalesced to one per animation frame, and <see cref="ScrollThrottle"/> spaces them out
    /// further where each one costs a round trip. A callback that only needs to know that an edge was
    /// reached is better served by the four edge callbacks, which are made once per arrival.
    /// </remarks>
    [Parameter] public EventCallback<BitScrollOffset> OnScroll { get; set; }

    /// <summary>
    /// Callback for when the pane has come to rest after being scrolled, carrying where it stopped.
    /// </summary>
    /// <remarks>
    /// It is the browser's own <c>scrollend</c>: one call once the scroll is over - the finger lifted, the
    /// scrollbar released, the momentum spent, the animated move finished - rather than one per frame of
    /// getting there. That makes it the right place for the work that is too expensive to do while a
    /// scroll is running: saving the reading position, loading what the reader has settled on, or lighting
    /// up the item a carousel came to rest at. Browsers without the event fall back to a short idle, so a
    /// pane is never left without the call.
    /// </remarks>
    [Parameter] public EventCallback<BitScrollOffset> OnScrollEnd { get; set; }

    /// <summary>
    /// Callback for when the pane starts being scrolled, carrying where it stood as it set off.
    /// </summary>
    /// <remarks>
    /// It is called once at the start of a scroll rather than for as long as one is running, which is what
    /// a page that gets out of the way while the reader is moving - a toolbar that folds away, a hover card
    /// that closes - wants. <see cref="OnScrollEnd"/> is the other half of it.
    /// </remarks>
    [Parameter] public EventCallback<BitScrollOffset> OnScrollStart { get; set; }

    /// <summary>
    /// Controls the visibility of scrollbars in the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? Overflow { get; set; }

    /// <summary>
    /// Controls the visibility of X-axis scrollbar in the pane.
    /// </summary>
    /// <remarks>
    /// It has the last word on the horizontal axis, so it overrides both <see cref="Overflow"/> and
    /// <see cref="Horizontal"/> there.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? OverflowX { get; set; }

    /// <summary>
    /// Controls the visibility of Y-axis scrollbar in the pane.
    /// </summary>
    /// <remarks>
    /// It has the last word on the vertical axis, so it overrides both <see cref="Overflow"/> and
    /// <see cref="Horizontal"/> there.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitOverflow? OverflowY { get; set; }

    /// <summary>
    /// What the browser does with a scroll that has already reached the edge of the pane.
    /// </summary>
    /// <remarks>
    /// <see cref="BitOverscroll.Contain"/> is what a pane inside a scrolling page almost always wants: the
    /// wheel stops at the end of the pane instead of carrying on into the page behind it, which is the
    /// difference between reading the pane and losing your place on the page. It is also what keeps a
    /// touch drag inside a dialog or a sheet from pulling the page to refresh underneath it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitOverscroll? Overscroll { get; set; }

    /// <summary>
    /// What the browser does with a horizontal scroll that has already reached the edge of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverscroll? OverscrollX { get; set; }

    /// <summary>
    /// What the browser does with a vertical scroll that has already reached the edge of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitOverscroll? OverscrollY { get; set; }

    /// <summary>
    /// Keeps the reader's place when content is added above what they are looking at.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the other half of an endless list: a page of older messages arriving at the top of a
    /// conversation is as tall as the messages in it, and without this it pushes everything the reader was
    /// reading that far down the screen. With it the pane is moved down by exactly what arrived, so what
    /// they were looking at does not move at all.
    /// <br />
    /// Every engine but WebKit already does this on its own (it is the CSS <c>overflow-anchor</c>
    /// behavior), so this changes nothing where the browser is anchoring the pane and brings the rest -
    /// Safari, most of all - up to the same behavior. A pane that sets <c>overflow-anchor: none</c> on
    /// itself has turned that anchoring off, so this takes it over on every engine. Only content that lands above the visible area counts, so an
    /// arrival at the bottom is left alone, and a pane standing at the very top of its content has no
    /// place to keep and is left alone as well - which is what makes it safe to pair with
    /// <see cref="OnReachedTop"/>, whose <see cref="ReachOffset"/> is worth a screenful so that the fetch
    /// starts before the reader is at the top rather than once they are.
    /// </remarks>
    [Parameter] public bool PreserveScroll { get; set; }

    /// <summary>
    /// How near an edge (in pixels) counts as having reached it, for the four edge callbacks.
    /// <br />
    /// The default value is <strong>0</strong>.
    /// </summary>
    /// <remarks>
    /// The default asks the pane to be at the edge itself. An endless list gives this a screenful or so,
    /// which is what makes the next page arrive before the reader gets to the bottom rather than after.
    /// </remarks>
    [Parameter] public int ReachOffset { get; set; }

    /// <summary>
    /// The ARIA role of the pane.
    /// </summary>
    /// <remarks>
    /// A pane renders no role of its own: a scrolling box is a piece of layout, and marking every one of
    /// them would only add noise for a screen reader. Set it to <c>region</c> or <c>group</c> - along with
    /// <see cref="BitComponentBase.AriaLabel"/>, which is what names it - where the pane is a part of the
    /// page in its own right, which is worth doing for a pane that <see cref="Focusable"/> puts in the tab
    /// order.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    ///  Sets the color of the scrollbar track and thumb. For specific colors, it has to contain both colors separated by a space or otherwise it won't work.
    /// </summary>
    /// <remarks>
    /// The thumb color comes first and the track second, as in <c>ScrollbarColor="gray transparent"</c>.
    /// This is the standard CSS property, which Chromium lets win over the rendering <see cref="Modern"/>
    /// draws, so the two are not worth combining on one pane.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? ScrollbarColor { get; set; }

    /// <summary>
    ///  Sets the desired thickness of scrollbars when they are shown.
    /// </summary>
    /// <remarks>
    /// This is the standard CSS property, which Chromium lets win over the rendering <see cref="Modern"/>
    /// draws, so the two are not worth combining on one pane.
    /// <br />
    /// <see cref="BitScrollbarWidth.None"/> takes the scrollbar off the screen while leaving the pane
    /// scrollable by every other means, which is worth pairing with <see cref="Fade"/> so there is still
    /// something saying the content carries on.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitScrollbarWidth? ScrollbarWidth { get; set; }

    /// <summary>
    /// The inset the pane keeps between its edges and anything scrolled into view inside it, as any CSS length.
    /// </summary>
    /// <remarks>
    /// This is the CSS <c>scroll-padding</c> property. It is what keeps a sticky header inside the pane from
    /// covering the thing that was just scrolled to - by a fragment navigation, or by the browser bringing a
    /// focused field into view. <see cref="ScrollToElement"/> reads it as well, so the moves this component
    /// makes leave the same room the browser's own do; the other scrolling methods take an absolute
    /// position or a distance and are left alone by it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? ScrollPadding { get; set; }

    /// <summary>
    /// The shortest interval (in milliseconds) between two <see cref="OnScroll"/> reports.
    /// <br />
    /// The default value is <strong>0</strong>.
    /// </summary>
    /// <remarks>
    /// Reports are always coalesced to at most one per animation frame; the default of 0 leaves it at
    /// that. A larger value spaces them out further, which is worth having wherever each report costs a
    /// round trip - a Blazor Server circuit, most of all - or wherever what is done with the position is
    /// more expensive than reading it. The last report of a scroll is always delivered, so the page is
    /// never left holding a position the pane has moved on from.
    /// </remarks>
    [Parameter] public int ScrollThrottle { get; set; }

    /// <summary>
    /// Animates the scrolling of the pane instead of jumping to the new position.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// It applies to every move the pane does not make by hand: the scrolling API of this component, a
    /// fragment navigation into it, the browser bringing a focused element inside it into view, and the
    /// pinning <see cref="AutoScroll"/> does. Dragging the scrollbar and turning the wheel are unaffected -
    /// those follow the pointer, as they should.
    /// <br />
    /// Like every other animation in the library it collapses to an instant move when the operating system
    /// or the browser reports 'prefers-reduced-motion: reduce', unless
    /// <see cref="BitComponentBase.ForceAnimation"/> opts this pane out of that.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Smooth { get; set; }

    /// <summary>
    /// Makes the pane come to rest on the snap positions of its content instead of anywhere between them.
    /// </summary>
    /// <remarks>
    /// This is the CSS <c>scroll-snap-type</c> property, which is how the platform itself builds a
    /// carousel, a set of pages or a strip of cards that always stops on an item: the scrolling stays the
    /// browser's, with all of its momentum and its keyboard handling, and only where it is allowed to come
    /// to rest is decided here.
    /// <br />
    /// Snapping needs the content to say where a snap position is, which is what <see cref="SnapAlign"/>
    /// puts on the direct children of the pane, and <see cref="SnapStop"/> is what keeps a fast scroll
    /// from passing over the positions it goes by. Both axes are snapped, each one only where there is
    /// something to scroll.
    /// <br />
    /// A pane that is also moved from code - by the scrolling API, or by the pinning <see cref="AutoScroll"/>
    /// does - is worth keeping to <see cref="BitScrollSnap.Proximity"/>, since a mandatory snap pulls such a
    /// move onto the nearest snap position rather than leaving it where it was put.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public BitScrollSnap? Snap { get; set; }

    /// <summary>
    /// Where the direct children of the pane come to rest in it while <see cref="Snap"/> is on.
    /// </summary>
    /// <remarks>
    /// It is the CSS <c>scroll-snap-align</c> property, applied to the direct children of the pane. A pane
    /// whose items sit inside a layout container of their own - the flex row a horizontal strip is laid
    /// out with - has that container as its only child, so give the items the property directly there
    /// instead of setting this.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitScrollSnapAlign? SnapAlign { get; set; }

    /// <summary>
    /// Keeps a fast scroll from passing over the snap positions it goes by.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// It is the CSS <c>scroll-snap-stop</c> property, applied to the direct children of the pane the same
    /// way <see cref="SnapAlign"/> is. Without it a flick of a strip comes to rest on whichever item the
    /// momentum happened to run out over, several items along; with it the pane stops at the first one it
    /// reaches, which is what turns a snapping strip into a carousel that moves one card per gesture.
    /// <br />
    /// It only has an effect while <see cref="Snap"/> is on and the children carry a snap position of
    /// their own, and it applies to a flick, a wheel and a page key alike - the browser stops at the next
    /// position however the scroll was started.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool SnapStop { get; set; }

    /// <summary>
    /// The width of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    /// <summary>
    /// Scrolls the pane to the end of its content, both horizontally and vertically.
    /// </summary>
    /// <remarks>
    /// The end is the end of the content rather than a side of the screen, so on the horizontal axis it is
    /// the visual left edge of a right-to-left pane and the visual right edge of every other one.
    /// </remarks>
    /// <param name="smooth">
    /// Whether the move is animated. Leaving it unset follows the <see cref="Smooth"/> parameter of the pane.
    /// </param>
    public ValueTask ScrollToEnd(bool? smooth = null)
    {
        if (IsRendered is false) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneScrollToEnd(RootElement, smooth ?? Smooth);
    }

    /// <summary>
    /// Scrolls the pane back to the start of its content, both horizontally and vertically.
    /// </summary>
    /// <remarks>
    /// The start is the start of the content rather than a side of the screen, so on the horizontal axis it
    /// is the visual right edge of a right-to-left pane and the visual left edge of every other one.
    /// </remarks>
    /// <param name="smooth">
    /// Whether the move is animated. Leaving it unset follows the <see cref="Smooth"/> parameter of the pane.
    /// </param>
    public ValueTask ScrollToStart(bool? smooth = null)
    {
        if (IsRendered is false) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneScrollToStart(RootElement, smooth ?? Smooth);
    }

    /// <summary>
    /// Scrolls the pane to an absolute position, in pixels.
    /// </summary>
    /// <remarks>
    /// An axis that is left null stays where it is, so one call serves "scroll to the top", "scroll to this
    /// column" and "scroll to both" alike, and positions outside the content are clamped to it.
    /// <br />
    /// The horizontal offset is measured from the visual left edge of the content whichever way the pane
    /// reads, which is the same way <see cref="BitScrollOffset.OffsetLeft"/> reports it: a position read
    /// off the pane can be handed straight back here to return to it.
    /// </remarks>
    /// <param name="left">
    /// How far from the visual left edge of the content, or null to leave that axis alone.
    /// </param>
    /// <param name="top">
    /// How far from the top of the content, or null to leave the vertical axis alone.
    /// </param>
    /// <param name="smooth">
    /// Whether the move is animated. Leaving it unset follows the <see cref="Smooth"/> parameter of the pane.
    /// </param>
    public ValueTask ScrollTo(double? left, double? top, bool? smooth = null)
    {
        if (IsRendered is false) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneScrollTo(RootElement, left, top, smooth ?? Smooth);
    }

    /// <summary>
    /// Scrolls the pane by a distance from wherever it currently stands, in pixels.
    /// </summary>
    /// <remarks>
    /// The horizontal distance is measured on the screen whichever way the pane reads, so a positive
    /// <paramref name="x"/> always moves the pane rightwards - the same way <see cref="ScrollTo"/> counts
    /// its offset. <see cref="ScrollToStart"/> and <see cref="ScrollToEnd"/> are the two calls that follow
    /// the content rather than the screen.
    /// </remarks>
    /// <param name="x">
    /// How far to move the pane rightwards; negative values move it leftwards.
    /// </param>
    /// <param name="y">
    /// How far to move down; negative values move back up.
    /// </param>
    /// <param name="smooth">
    /// Whether the move is animated. Leaving it unset follows the <see cref="Smooth"/> parameter of the pane.
    /// </param>
    public ValueTask ScrollBy(double x, double y, bool? smooth = null)
    {
        if (IsRendered is false) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneScrollBy(RootElement, x, y, smooth ?? Smooth);
    }

    /// <summary>
    /// Brings an element inside the pane into view, by scrolling the pane itself.
    /// </summary>
    /// <remarks>
    /// Only the pane moves: scrolling the element into view the browser's own way would scroll every
    /// scrolling ancestor the pane sits in as well, which moves the page under a reader who only asked the
    /// pane to move. An element that is not inside the pane is left alone.
    /// </remarks>
    /// <param name="elementId">
    /// The <c>id</c> of an element inside the pane.
    /// </param>
    /// <param name="offset">
    /// How much room to leave between the element and the edges of the pane, in pixels. Left at 0 it takes
    /// whatever <see cref="ScrollPadding"/> asks for, so a pane with a sticky header of its own gets the
    /// same clearance here that a fragment navigation into it would have got.
    /// </param>
    /// <param name="smooth">
    /// Whether the move is animated. Leaving it unset follows the <see cref="Smooth"/> parameter of the pane.
    /// </param>
    /// <param name="alignment">
    /// Where in the pane the element is left. The default brings it to the start of the pane, which is what
    /// a fragment navigation does; <see cref="BitScrollAlignment.Nearest"/> is the one that leaves a pane
    /// that is already showing the element alone.
    /// </param>
    public ValueTask ScrollToElement(string elementId,
                                     double offset = 0,
                                     bool? smooth = null,
                                     BitScrollAlignment alignment = BitScrollAlignment.Start)
    {
        if (IsRendered is false || elementId.HasNoValue()) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneScrollToElement(RootElement, elementId, offset, smooth ?? Smooth, _AlignmentMap[alignment]);
    }

    /// <summary>
    /// Gives the focus to the pane itself.
    /// </summary>
    /// <remarks>
    /// A pane only takes the focus while something has put it in the tab order - <see cref="Focusable"/>,
    /// or a <see cref="BitComponentBase.TabIndex"/> of its own - and once it holds the focus the arrow
    /// keys, Page Up and Page Down, Home and End all scroll it.
    /// </remarks>
    public ValueTask FocusAsync()
    {
        // A pane that has not been rendered yet leaves an empty element reference behind, which throws
        // instead of doing nothing when it is focused - where every other call on this component quietly
        // does nothing until there is something to do it to.
        if (IsRendered is false) return ValueTask.CompletedTask;

        return RootElement.FocusAsync();
    }

    /// <summary>
    /// Gives the focus to the pane itself, optionally without scrolling it into view.
    /// </summary>
    /// <param name="preventScroll">
    /// Whether the browser is asked to leave the page where it is instead of bringing the pane into view.
    /// </param>
    public ValueTask FocusAsync(bool preventScroll)
    {
        if (IsRendered is false) return ValueTask.CompletedTask;

        return RootElement.FocusAsync(preventScroll);
    }

    /// <summary>
    /// Reads where the pane currently stands, straight from the browser.
    /// </summary>
    /// <remarks>
    /// This is the same <see cref="BitScrollOffset"/> that <see cref="OnScroll"/> carries, for a page that
    /// wants the position at a moment of its own choosing rather than as it changes. A pane that has not
    /// been rendered yet reports null.
    /// </remarks>
    public ValueTask<BitScrollOffset?> GetScrollOffset()
    {
        if (IsRendered is false) return ValueTask.FromResult<BitScrollOffset?>(null);

        return _js.BitScrollablePaneGetOffset(RootElement);
    }

    /// <summary>
    /// Re-measures the pane and reports whatever has changed since it was last measured.
    /// </summary>
    /// <remarks>
    /// The pane watches both its own size and its content on its own, so this is only for the changes
    /// neither of those can see - a web font that finished loading, an image that settled at a size the
    /// markup never named - after which the fade and the edge callbacks are brought back up to date. It
    /// does nothing on a pane that asked for neither.
    /// </remarks>
    public ValueTask Refresh()
    {
        if (_jsSetupDone is false) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneRefresh(_jsId!);
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



    protected override string RootElementClass => "bit-scp";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);
        StyleBuilder.Register(() => Height.HasValue() ? $"height:{Height}" : string.Empty);

        StyleBuilder.Register(() => AutoWidth ? "width:auto" : string.Empty);
        StyleBuilder.Register(() => AutoHeight ? "height:auto" : string.Empty);
        StyleBuilder.Register(() => AutoSize ? "height:auto;width:auto" : string.Empty);

        StyleBuilder.Register(() => FitWidth ? "width:fit-content" : string.Empty);
        StyleBuilder.Register(() => FitHeight ? "height:fit-content" : string.Empty);
        StyleBuilder.Register(() => FitSize ? "height:fit-content;width:fit-content" : string.Empty);

        StyleBuilder.Register(() => FullWidth ? "width:100%" : string.Empty);
        StyleBuilder.Register(() => FullHeight ? "height:100%" : string.Empty);
        StyleBuilder.Register(() => FullSize ? "height:100%;width:100%" : string.Empty);

        StyleBuilder.Register(() => MinWidth.HasValue() ? $"min-width:{MinWidth}" : string.Empty);
        StyleBuilder.Register(() => MinHeight.HasValue() ? $"min-height:{MinHeight}" : string.Empty);
        StyleBuilder.Register(() => MaxWidth.HasValue() ? $"max-width:{MaxWidth}" : string.Empty);
        StyleBuilder.Register(() => MaxHeight.HasValue() ? $"max-height:{MaxHeight}" : string.Empty);

        // Every overflow declaration is pushed from one registrar so that they land in the style attribute
        // in the order they are written here, which is the order they override one another in: the axes a
        // Horizontal pane picks first, then whatever the three overflow parameters say about either axis,
        // and NoScroll over all of it. Both axes are always spelled out as longhands rather than folded
        // into the shorthand, since the shorthand would also reset the axis it was not asked about.
        StyleBuilder.Register(register =>
        {
            if (Horizontal)
            {
                register("overflow-x:auto");
                register("overflow-y:hidden");
            }

            if (Overflow.HasValue)
            {
                register($"overflow:{_OverflowMap[Overflow.Value]}");
            }

            if (OverflowX.HasValue)
            {
                register($"overflow-x:{_OverflowMap[OverflowX.Value]}");
            }

            if (OverflowY.HasValue)
            {
                register($"overflow-y:{_OverflowMap[OverflowY.Value]}");
            }

            if (NoScroll)
            {
                register("overflow:hidden");
            }

            return string.Empty;
        });

        StyleBuilder.Register(() => Overscroll.HasValue ? $"overscroll-behavior:{_OverscrollMap[Overscroll.Value]}" : string.Empty);
        StyleBuilder.Register(() => OverscrollX.HasValue ? $"overscroll-behavior-x:{_OverscrollMap[OverscrollX.Value]}" : string.Empty);
        StyleBuilder.Register(() => OverscrollY.HasValue ? $"overscroll-behavior-y:{_OverscrollMap[OverscrollY.Value]}" : string.Empty);

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

        StyleBuilder.Register(() => ScrollPadding.HasValue() ? $"scroll-padding:{ScrollPadding}" : string.Empty);

        // Both axes are asked for, each one snapping only where there is anything to scroll, so one pane
        // does not have to say which way it runs twice.
        StyleBuilder.Register(() => Snap switch
        {
            BitScrollSnap.None => "scroll-snap-type:none",
            BitScrollSnap.Proximity => "scroll-snap-type:both proximity",
            BitScrollSnap.Mandatory => "scroll-snap-type:both mandatory",
            _ => string.Empty
        });

        StyleBuilder.Register(() => FadeSize.HasValue() ? $"--bit-scp-fsz:{FadeSize}" : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Modern ? "bit-scp-mod" : string.Empty);

        ClassBuilder.Register(() => AutoHideScrollbar ? "bit-scp-ahs" : string.Empty);

        ClassBuilder.Register(() => Horizontal ? "bit-scp-hor" : string.Empty);

        ClassBuilder.Register(() => Smooth ? "bit-scp-smt" : string.Empty);

        ClassBuilder.Register(() => Fade ? "bit-scp-fad" : string.Empty);

        // The grab cursor is the promise that the content can be dragged, so a pane the reader is not to be
        // able to move must not make it, whatever DragScroll says.
        ClassBuilder.Register(() => DragScroll && NoScroll is false ? "bit-scp-drg" : string.Empty);

        ClassBuilder.Register(() => SnapAlign switch
        {
            BitScrollSnapAlign.None => "bit-scp-sna-non",
            BitScrollSnapAlign.Start => "bit-scp-sna-str",
            BitScrollSnapAlign.Center => "bit-scp-sna-cnt",
            BitScrollSnapAlign.End => "bit-scp-sna-end",
            _ => string.Empty
        });

        ClassBuilder.Register(() => SnapStop ? "bit-scp-sns" : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        if (firstRender)
        {
            await ApplyInitialScroll();
        }

        await SetupJs();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false)
        {
            await base.DisposeAsync(disposing);
            return;
        }

        await base.DisposeAsync(disposing);

        if (_jsSetupDone)
        {
            _jsSetupDone = false;

            try
            {
                await _js.BitScrollablePaneDispose(_jsId!);
            }
            catch (JSDisconnectedException) { } // circuit gone; nothing to tear down
            catch (ObjectDisposedException) { } // the component went away mid-call
        }

        _jsId = null;

        _dotnetObj?.Dispose();
        _dotnetObj = null;
    }



    // Where the pane opens, for the one render it can be decided on. It is never animated - a pane that
    // slid into place from the top as it appeared would be an animation nobody asked for - and it is left
    // to AutoScroll where both are used, since a pane pinned to the end has already been told where to
    // open. A pane that asked for neither costs nothing here.
    private async Task ApplyInitialScroll()
    {
        if (AutoScroll) return;
        if (InitialScrollLeft.HasValue is false && InitialScrollTop.HasValue is false) return;

        try
        {
            await _js.BitScrollablePaneScrollTo(RootElement, InitialScrollLeft, InitialScrollTop, false);
        }
        catch (JSDisconnectedException) { } // circuit gone; there is nothing to open
        catch (ObjectDisposedException) { } // the component went away mid-call
    }

    // Everything the browser side needs is decided here rather than at the call sites, so that turning the
    // last of the features off tears the instance back down instead of leaving a listener behind, and so
    // that a render which changed none of them costs a comparison rather than a round trip.
    private async Task SetupJs()
    {
        // The render this follows may have been the last one: everything before it is awaited, so a pane
        // taken off the page while one of those calls was in flight is disposed before this runs. Setting
        // up then would hand the browser a .NET object reference the disposal has already been and gone
        // for, leaving it - and the component it points at - alive for as long as the page is.
        if (IsDisposed) return;

        try
        {
            if (_needsJs is false)
            {
                if (_jsSetupDone is false) return;

                var registered = _jsId!;

                _jsSetupDone = false;
                _autoScrolled = false;
                _jsOptions = null;
                _jsId = null;

                await _js.BitScrollablePaneDispose(registered);

                return;
            }

            var options = BuildOptions();

            if (_jsSetupDone is false)
            {
                _dotnetObj ??= DotNetObjectReference.Create(this);
                _jsOptions = options;
                _jsSetupDone = true;
                _jsId = _Id;

                await _js.BitScrollablePaneSetup(_jsId, RootElement, _dotnetObj, options);
            }
            else if (options != _jsOptions)
            {
                _jsOptions = options;

                await _js.BitScrollablePaneUpdate(_jsId!, options);
            }

            // The first pinning is the one this side has to ask for: it is unconditional - a pane that
            // starts out with content already in it belongs at the end of it - and the browser side has
            // nothing to compare against on its very first measurement. Every pinning after that is its
            // own answer to content it watches for and re-pins off, without a round trip per render.
            if (AutoScroll)
            {
                if (_autoScrolled is false)
                {
                    _autoScrolled = true;

                    await _js.BitScrollablePaneAutoScroll(_jsId!, true);
                }
            }
            else
            {
                _autoScrolled = false;
            }
        }
        catch (JSDisconnectedException) { } // circuit gone; nothing to set up
        catch (ObjectDisposedException) { } // the component went away mid-call
    }

    private BitScrollablePaneOptions BuildOptions() => new()
    {
        Fade = Fade,
        Offset = Math.Max(0, ReachOffset),
        Throttle = Math.Max(0, ScrollThrottle),
        Scroll = OnScroll.HasDelegate,
        ScrollStart = OnScrollStart.HasDelegate,
        ScrollEnd = OnScrollEnd.HasDelegate,
        Top = OnReachedTop.HasDelegate,
        Bottom = OnReachedBottom.HasDelegate,
        Left = OnReachedLeft.HasDelegate,
        Right = OnReachedRight.HasDelegate,
        AutoScroll = AutoScroll,
        AutoScrollThreshold = Math.Max(0, AutoScrollThreshold),
        Smooth = Smooth,
        Drag = DragScroll,
        Momentum = DragMomentum,
        Wheel = HorizontalWheel,
        Preserve = PreserveScroll,
        AutoHide = _autoHides,
        NoScroll = NoScroll,
    };



    private static readonly Dictionary<BitScrollAlignment, string> _AlignmentMap = new()
    {
        { BitScrollAlignment.Start, "start" },
        { BitScrollAlignment.Center, "center" },
        { BitScrollAlignment.End, "end" },
        { BitScrollAlignment.Nearest, "nearest" },
    };

    private static readonly Dictionary<BitOverflow, string> _OverflowMap = new()
    {
        { BitOverflow.Auto, "auto" },
        { BitOverflow.Hidden, "hidden" },
        { BitOverflow.Scroll, "scroll" },
        { BitOverflow.Visible, "visible" },
    };

    private static readonly Dictionary<BitOverscroll, string> _OverscrollMap = new()
    {
        { BitOverscroll.Auto, "auto" },
        { BitOverscroll.Contain, "contain" },
        { BitOverscroll.None, "none" },
    };
}
