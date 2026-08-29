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
/// that still have content beyond them, a position reported back as it changes, a callback as each edge is
/// reached, and a scrolling API - <see cref="ScrollToEnd"/>, <see cref="ScrollToStart"/>,
/// <see cref="ScrollTo"/>, <see cref="ScrollBy"/>, <see cref="ScrollToElement"/> - that a page drives it
/// with.
/// <br />
/// Nothing on the browser side is set up for a pane that asked for none of it: a pane that only sizes a box
/// and styles its scrollbars is a single div with a style attribute, and the scroll listener, the observers
/// and the round trips only appear once <see cref="AutoScroll"/>, <see cref="Fade"/>,
/// <see cref="OnScroll"/> or one of the four edge callbacks is used.
/// </remarks>
public partial class BitScrollablePane : BitComponentBase
{
    private bool _autoScrolled;
    private bool _jsSetupDone;
    private BitScrollablePaneOptions? _jsOptions;
    private DotNetObjectReference<BitScrollablePane>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    // A tabindex of the consumer's own also makes the pane focusable, so it is honored ahead of the
    // parameter that only asks for the default one. A disabled pane is taken back out of the tab order
    // rather than left in it as something that cannot be scrolled.
    private string? _tabIndex => (TabIndex ?? (Focusable ? "0" : null)) is string tabIndex
        ? (IsEnabled ? tabIndex : "-1")
        : null;

    // Whether anything the browser side does is asked for. Everything else this component offers is CSS,
    // so a pane that wants none of these never sets up a listener, an observer or a .NET object reference.
    private bool _needsJs => IsEnabled && (AutoScroll || OnScroll.HasDelegate || _watchesContent);

    // Whether anything the pane draws or reports depends on the size of its content rather than only on
    // where it stands, which is what makes a change of content worth re-measuring after.
    private bool _watchesContent => Fade
                                    || OnReachedTop.HasDelegate
                                    || OnReachedBottom.HasDelegate
                                    || OnReachedLeft.HasDelegate
                                    || OnReachedRight.HasDelegate;



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
    /// Alias for the ChildContent parameter.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The content of the pane, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

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
    /// reaches. It is drawn with a mask, so it works over any background - and browsers that cannot
    /// composite masks simply do not draw it.
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
    /// The thickness comes from the <c>--bit-scp-sbs</c> custom property, which can be retuned per pane
    /// through <see cref="BitComponentBase.Style"/>. Note that <see cref="ScrollbarWidth"/> and
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
    [Parameter, ResetStyleBuilder]
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
    /// showing asks for them here. See <see cref="OnReachedBottom"/> for when the callbacks are made.
    /// </remarks>
    [Parameter] public EventCallback OnReachedTop { get; set; }

    /// <summary>
    /// Callback for when the pane is scrolled, carrying where it now stands.
    /// </summary>
    /// <remarks>
    /// The position is measured in the browser and handed over as a <see cref="BitScrollOffset"/>, which
    /// carries both offsets, the size of the content, the size of the visible area, and the answers
    /// derived from them - how far along each axis the pane is, and which edges it is standing at.
    /// <br />
    /// Reports are coalesced to one per animation frame, and <see cref="ScrollThrottle"/> spaces them out
    /// further where each one costs a round trip. A callback that only needs to know that an edge was
    /// reached is better served by the four edge callbacks, which are made once per arrival.
    /// </remarks>
    [Parameter] public EventCallback<BitScrollOffset> OnScroll { get; set; }

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
    /// The width of the pane.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    /// <summary>
    /// Scrolls the pane to the end of its content, both horizontally and vertically.
    /// </summary>
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
    /// column" and "scroll to both" alike. Positions outside the content are clamped to it, and a
    /// right-to-left pane is given the same offsets as a left-to-right one: 0 is the start of the content
    /// either way.
    /// </remarks>
    /// <param name="left">
    /// How far from the start of the content, along the horizontal axis, or null to leave that axis alone.
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
    /// The distances are measured in reading order, so a positive <paramref name="x"/> moves a
    /// right-to-left pane leftwards, which is the way its content runs.
    /// </remarks>
    /// <param name="x">
    /// How far to move along the horizontal axis; negative values move back towards the start.
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
    public ValueTask ScrollToElement(string elementId, double offset = 0, bool? smooth = null)
    {
        if (IsRendered is false || elementId.HasNoValue()) return ValueTask.CompletedTask;

        return _js.BitScrollablePaneScrollToElement(RootElement, elementId, offset, smooth ?? Smooth);
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

        return _js.BitScrollablePaneRefresh(_Id);
    }



    [JSInvokable("OnScroll")]
    public async Task _OnScroll(BitScrollOffset offset)
    {
        if (IsDisposed || offset is null) return;

        await OnScroll.InvokeAsync(offset);
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

        StyleBuilder.Register(() => FadeSize.HasValue() ? $"--bit-scp-fsz:{FadeSize}" : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Modern ? "bit-scp-mod" : string.Empty);

        ClassBuilder.Register(() => Horizontal ? "bit-scp-hor" : string.Empty);

        ClassBuilder.Register(() => Smooth ? "bit-scp-smt" : string.Empty);

        ClassBuilder.Register(() => Fade ? "bit-scp-fad" : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

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
                await _js.BitScrollablePaneDispose(_Id);
            }
            catch (JSDisconnectedException) { } // circuit gone; nothing to tear down
        }

        _dotnetObj?.Dispose();
        _dotnetObj = null;
    }



    // Everything the browser side needs is decided here rather than at the call sites, so that turning the
    // last of the features off tears the instance back down instead of leaving a listener behind, and so
    // that a render which changed none of them costs a comparison rather than a round trip.
    private async Task SetupJs()
    {
        try
        {
            if (_needsJs is false)
            {
                if (_jsSetupDone is false) return;

                _jsSetupDone = false;
                _autoScrolled = false;
                _jsOptions = null;

                await _js.BitScrollablePaneDispose(_Id);

                return;
            }

            var options = BuildOptions();

            if (_jsSetupDone is false)
            {
                _dotnetObj ??= DotNetObjectReference.Create(this);
                _jsOptions = options;
                _jsSetupDone = true;

                await _js.BitScrollablePaneSetup(_Id, RootElement, _dotnetObj, options);
            }
            else if (options != _jsOptions)
            {
                _jsOptions = options;

                await _js.BitScrollablePaneUpdate(_Id, options);
            }
            else if (AutoScroll is false && _watchesContent)
            {
                // Nothing about the pane changed, but its content may have, and what it draws and reports
                // is measured off that content. The browser side watches for the change itself, so this is
                // the belt to that observer's braces - for the render whose effect on the size of the
                // content a mutation record does not carry. A pane that only reports where it stands has
                // nothing to re-measure here, and an auto scrolling one is measured by the call below.
                await _js.BitScrollablePaneRefresh(_Id);
            }

            if (AutoScroll)
            {
                // The first pinning is unconditional - a pane that starts out with content already in it
                // belongs at the end of it - and every one after that only holds a pane that was left there.
                await _js.BitScrollablePaneAutoScroll(_Id, _autoScrolled is false);

                _autoScrolled = true;
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
        Top = OnReachedTop.HasDelegate,
        Bottom = OnReachedBottom.HasDelegate,
        Left = OnReachedLeft.HasDelegate,
        Right = OnReachedRight.HasDelegate,
        AutoScroll = AutoScroll,
        AutoScrollThreshold = Math.Max(0, AutoScrollThreshold),
        Smooth = Smooth,
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
