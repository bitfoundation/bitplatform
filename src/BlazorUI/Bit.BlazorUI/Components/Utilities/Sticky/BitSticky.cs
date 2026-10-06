using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A Sticky is a component that enables elements to stick during scrolling.
/// </summary>
/// <remarks>
/// The component is a thin, dependable wrapper over the browser's own <c>position: sticky</c>: the
/// content stays in the normal flow - keeping the room it occupies, so nothing jumps when it pins -
/// until the scroll carries it to an edge of its nearest scrolling container, where it stays while
/// the rest of the content passes. Which edge is either a <see cref="Placement"/> (the two vertical
/// edges, the two horizontal ones - named Start and End, so they follow the reading direction - or
/// both of a pair) or an exact offset from any of the four sides (<see cref="Top"/>,
/// <see cref="Bottom"/>, <see cref="Left"/>, <see cref="Right"/>). With none of them set, it sticks
/// to the top.
/// <br />
/// CSS has no event for the moment an element actually pins, so the component derives one:
/// <see cref="OnStuckChanged"/> reports the flips, <see cref="OnStuckEdgesChanged"/> the edge that
/// holds it, <see cref="IsStuck"/> and <see cref="StuckEdges"/> hold the current state, and
/// <see cref="StuckClass"/> / <see cref="StuckStyle"/> are applied only while stuck - which is what
/// a header that casts a shadow only once content passes under it needs, and what
/// <see cref="ElevateOnStuck"/> does out of the box with the theme's surface and shadows. An element
/// that has not moved is never reported as pinned, however exactly it happens to rest on an edge, so
/// the header of a container nobody has scrolled yet is not stuck. The detection is only wired up when
/// one of those members is used, so a sticky that does not ask for it stays pure CSS.
/// <br />
/// A pinned element covers what scrolls under it, focused controls included;
/// <see cref="ScrollPadding"/> reserves that room on its scrolling container so the browser never
/// scrolls a control into view behind it (WCAG 2.4.11).
/// <br />
/// Two things decide whether a sticky element has anywhere to stick at all, and both belong to the
/// markup around it rather than to the component: it pins within its nearest scrolling ancestor
/// (any ancestor with an overflow other than visible becomes that boundary, even one that does not
/// scroll), and it only travels within its own parent - a parent no taller than the element, or a
/// flex parent stretching it, gives it no room to stick in.
/// </remarks>
public partial class BitSticky : BitComponentBase
{
    private bool _stuck;
    private bool _settingUp;
    private bool _setupPending;
    private string? _attachedId;
    private string? _attachedSignature;
    private BitStickyEdges _edges;
    private DotNetObjectReference<BitSticky>? _dotnetObj;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the sticky component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple sticky components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitStickyParams.ParamName)]
    public BitStickyParams? CascadingParameters { get; set; }



    /// <summary>
    /// Specifying the vertical position of a positioned element from bottom.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// ("2rem", "10%", "calc(1rem + 2px)") is accepted. Setting any of the four offsets replaces the
    /// default stick-to-top behavior with exactly the edges the offsets name, and an offset set
    /// alongside a <see cref="Placement"/> overrides that side of it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Bottom { get; set; }

    /// <summary>
    /// The content of the Sticky, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    /// <remarks>
    /// A sticky element is very often one HTML already has a name for - the "header" of a page or of
    /// a pane, the "footer" that keeps a toolbar in reach, the "nav" of a table of contents beside an
    /// article, the "aside" of a sidebar, or the "th" and "tr" of a frozen table header - and the name
    /// is what tells assistive technologies which of them it is. The tag decides nothing about the
    /// stickiness itself: every parameter of the component works the same whichever one is rendered.
    /// <br />
    /// The name is used as written, but only while it is a name a tag can have - a letter followed by
    /// letters, digits and the "-", "_", "." and ":" that join them. Anything else falls back to the
    /// default tag, since a name carrying whitespace or a "&lt;" would be a way to write markup rather
    /// than to name an element. A void element (an "hr", an "img") is rendered without the
    /// <see cref="ChildContent"/>, which it cannot hold.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// Gives the component a surface and a theme shadow, cast away from the edge holding it, only while it is
    /// stuck - the cue that content is passing underneath.
    /// </summary>
    /// <remarks>
    /// A pinned bar that is shadowed from the start looks detached from content nobody has scrolled yet, and one
    /// with no shadow at all gives no hint that content is passing behind it. The shadow of a top or bottom pin is
    /// the theme's app-bar shadow (<c>--bit-Sticky-shadow-top</c> / <c>--bit-Sticky-shadow-bottom</c>), that of a
    /// side pin the card shadow (<c>--bit-Sticky-shadow-left</c> / <c>--bit-Sticky-shadow-right</c>), and the
    /// surface is the primary background (<c>--bit-Sticky-background</c>), which a background brought by
    /// <see cref="BitComponentBase.Class"/> or <see cref="BitComponentBase.Style"/> still wins over. Forced
    /// colors mode, which drops every shadow, draws an outline instead.
    /// <br />
    /// Using it attaches the stuck detection that drives <see cref="OnStuckChanged"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ElevateOnStuck { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from left.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// is accepted. Horizontal sticking needs a container that scrolls horizontally, and the offset
    /// names a physical side - for a side that follows the reading direction, use the Start and End
    /// members of <see cref="Placement"/> instead.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Left { get; set; }

    /// <summary>
    /// Callback for when the stuck state of the component changes. The provided value is true while
    /// the element is pinned to an edge of its scrolling container.
    /// </summary>
    /// <remarks>
    /// CSS itself has no such event, so the state is derived by a small script watching the scroll.
    /// The script is only attached while this callback, <see cref="OnStuckEdgesChanged"/>,
    /// <see cref="StuckClass"/>, <see cref="StuckStyle"/> or <see cref="ElevateOnStuck"/> is used, and
    /// only while the component is enabled.
    /// <br />
    /// This reports only that the element is pinned, not to what: an element that moves from one edge
    /// of a pair to the other stays stuck throughout and raises nothing here.
    /// <see cref="OnStuckEdgesChanged"/> is what reports that move.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnStuckChanged { get; set; }

    /// <summary>
    /// Callback for when the set of edges the component is pinned to changes.
    /// </summary>
    /// <remarks>
    /// This is the finer grained half of <see cref="OnStuckChanged"/>: it names the edges rather than
    /// only reporting that there are some, so a bar pinned by a <see cref="BitPlacement"/> that
    /// holds a pair of them can tell which of the two is holding it - which side to cast its shadow
    /// toward, which border to draw - and it also reports the move from one of them to the other,
    /// which never flips the boolean. The edges are physical, so a Start sticky reports
    /// <see cref="BitStickyEdges.Left"/> in a left-to-right container and
    /// <see cref="BitStickyEdges.Right"/> in a right-to-left one.
    /// </remarks>
    [Parameter] public EventCallback<BitStickyEdges> OnStuckEdgesChanged { get; set; }

    /// <summary>
    /// Region to render sticky component in.
    /// </summary>
    /// <remarks>
    /// Top, Bottom and TopAndBottom pin the element while the container scrolls vertically; Start,
    /// End and StartAndEnd pin it while the container scrolls horizontally, and follow the reading
    /// direction (Start is left in LTR and right in RTL). When neither a Placement nor any offset is
    /// set, the component sticks to the top.
    /// <br />
    /// Every value but the physical pair and Center is meaningful here: Left, Right and Center fall back to the
    /// default, since a sticky element is pinned against an edge of the axis it scrolls on, never to a side of the
    /// screen or to the middle of the container.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from right.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// is accepted. Horizontal sticking needs a container that scrolls horizontally, and the offset
    /// names a physical side - for a side that follows the reading direction, use the Start and End
    /// members of <see cref="Placement"/> instead.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Right { get; set; }

    /// <summary>
    /// Reserves the room the component covers as the scroll padding of its scrolling container, so nothing
    /// scrolled into view ever lands underneath it.
    /// </summary>
    /// <remarks>
    /// A pinned element covers the content passing under it, and a browser bringing something into view -
    /// the next control the focus moves to, an anchor followed, a call to scrollIntoView - stops with that
    /// thing hidden behind it (WCAG 2.4.11, Focus Not Obscured). This sets the scroll padding of the
    /// container on every edge the element can pin to, to its offset plus its size there, and keeps it in
    /// step while either changes. An edge is claimed only where the element can actually be pinned: one it
    /// has an offset on and room in its parent to travel toward - so a TopAndBottom bar at the top of a pane
    /// claims no room at the bottom, which it can never reach. Several stickies of one container share it:
    /// each edge carries the largest claim, the scroll padding the container already has (from a stylesheet,
    /// an inline style, or a BitHeader or BitFooter reserving its own) is kept wherever it is the larger one,
    /// and it is all that is left once the last of them lets go.
    /// <br />
    /// It needs no stuck detection, so it attaches nothing on the scroll, and it is released while the
    /// component is disabled. The claim is made again whenever the element changes size or anything that
    /// moves its edges or offsets changes on it (<see cref="Position"/>, the offsets,
    /// <see cref="BitComponentBase.Dir"/>, <see cref="BitComponentBase.Class"/>,
    /// <see cref="BitComponentBase.Style"/>); one moved from outside it - a <c>--bit-Sticky-offset-*</c>
    /// variable set on an ancestor, a stylesheet - is picked up by <see cref="RefreshAsync"/>.
    /// </remarks>
    [Parameter]
    public bool ScrollPadding { get; set; }

    /// <summary>
    /// The CSS class applied to the root element only while the component is stuck.
    /// </summary>
    /// <remarks>
    /// This is what styles the pinned state differently from the flowing one - a shadow, an opaque
    /// background, a border once content passes underneath. Using it attaches the same stuck
    /// detection that drives <see cref="OnStuckChanged"/>, and the component also carries the
    /// <c>bit-stk-stc</c> class while stuck, plus one naming each edge that holds it
    /// (<c>bit-stk-stc-top</c>, <c>bit-stk-stc-btm</c>, <c>bit-stk-stc-lft</c>, <c>bit-stk-stc-rgt</c>),
    /// which is what a shadow that has to fall away from the edge it is pinned to selects on.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? StuckClass { get; set; }

    /// <summary>
    /// The CSS style applied to the root element only while the component is stuck.
    /// </summary>
    /// <remarks>
    /// The inline counterpart of <see cref="StuckClass"/>, for a pinned look that is one or two
    /// declarations rather than a class. It is appended after every other inline style, so a
    /// declaration here wins over the same one in <see cref="BitComponentBase.Style"/> for as long
    /// as the element is pinned. Using it attaches the same stuck detection that drives
    /// <see cref="OnStuckChanged"/>.
    /// </remarks>
    [Parameter] public string? StuckStyle { get; set; }

    /// <summary>
    /// Specifying the vertical position of a positioned element from top.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// ("2rem", "10%", "calc(1rem + 2px)") is accepted. Setting any of the four offsets replaces the
    /// default stick-to-top behavior with exactly the edges the offsets name, and an offset set
    /// alongside a <see cref="Placement"/> overrides that side of it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Top { get; set; }

    /// <summary>
    /// The z-index of the root element, which decides what the pinned content passes over and what
    /// passes over it.
    /// </summary>
    /// <remarks>
    /// When not set, the component keeps a z-index of 1 - enough to stay above the plain flowing
    /// content it sticks over without covering the popups and overlays of the rest of the page.
    /// Raise it where positioned content in the same stacking context has to pass underneath. The
    /// <c>--bit-Sticky-z-index</c> custom property sets the same default from a stylesheet, for a whole
    /// region at once; the parameter wins over it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ZIndex { get; set; }



    /// <summary>
    /// Gets a value indicating whether the component is currently stuck to an edge of its scrolling
    /// container. It is always false unless <see cref="OnStuckChanged"/>,
    /// <see cref="OnStuckEdgesChanged"/>, <see cref="StuckClass"/>, <see cref="StuckStyle"/> or
    /// <see cref="ElevateOnStuck"/> is used, since those are what attach the stuck detection.
    /// </summary>
    public bool IsStuck => _stuck;

    /// <summary>
    /// Gets the edges of the scrolling container the component is currently pinned to.
    /// </summary>
    /// <remarks>
    /// This is <see cref="IsStuck"/> with the edges named: it is
    /// <see cref="BitStickyEdges.None"/> exactly while that one is false, and it carries the two
    /// edges that meet in a corner while the element is pinned into one. Like <see cref="IsStuck"/>,
    /// it stays None unless one of the members that attach the stuck detection is used.
    /// </remarks>
    public BitStickyEdges StuckEdges => _edges;



    /// <summary>
    /// Reads the stuck state of the component again, along with everything it is derived from.
    /// </summary>
    /// <remarks>
    /// The state settles itself: it is read on every scroll of the container, and again whenever the
    /// element, its parent, the scrolling container or the page changes size, and whenever a parameter
    /// that moves the edges or the offsets changes. What is left over is a layout change none of those
    /// can see - content moved around inside the container without any of the watched boxes changing
    /// size, a stylesheet or a <c>--bit-Sticky-offset-*</c> variable changed - and this is what such a
    /// change is answered with. It does
    /// nothing while the detection is not attached, and nothing before the first render.
    /// </remarks>
    public async ValueTask RefreshAsync()
    {
        if (IsDisposed || _attachedId is null) return;

        try
        {
            await _js.BitStickiesRefresh(_attachedId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    /// <summary>
    /// Called by the scroll script of the component when the edges the element is pinned to change.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="edges">The <see cref="BitStickyEdges"/> flags the script has resolved.</param>
    [JSInvokable("OnStuckChange")]
    public async Task _OnStuckChange(int edges)
    {
        // The script is disposed asynchronously, so a scroll of the very last frame can still land
        // here after the component is gone, where there is nothing left to re-render - or after it
        // was disabled, where the report is of a stickiness that is already off and nothing would be
        // left to clear the state it latched.
        if (IsDisposed || Disabled) return;

        await SetStuckEdges((BitStickyEdges)edges);
    }



    protected override string RootElementClass => "bit-stk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Placement switch
        {
            BitPlacement.Top => "bit-stk-top",
            BitPlacement.Bottom => "bit-stk-btm",
            BitPlacement.TopAndBottom => "bit-stk-tab",
            BitPlacement.Start => "bit-stk-srt",
            BitPlacement.End => "bit-stk-end",
            BitPlacement.StartAndEnd => "bit-stk-sae",
            _ => (Top.HasNoValue() && Bottom.HasNoValue() && Left.HasNoValue() && Right.HasNoValue())
                    ? "bit-stk-top"
                    : string.Empty
        });

        ClassBuilder.Register(() => ElevateOnStuck ? "bit-stk-elv" : string.Empty);

        ClassBuilder.Register(() => _stuck ? "bit-stk-stc" : string.Empty);

        // One class per edge that holds the element, so a pinned look can be told apart by the side it
        // is pinned to without a callback and a field to remember it in.
        ClassBuilder.Register(() => (_edges & BitStickyEdges.Top) == 0 ? string.Empty : "bit-stk-stc-top");
        ClassBuilder.Register(() => (_edges & BitStickyEdges.Bottom) == 0 ? string.Empty : "bit-stk-stc-btm");
        ClassBuilder.Register(() => (_edges & BitStickyEdges.Left) == 0 ? string.Empty : "bit-stk-stc-lft");
        ClassBuilder.Register(() => (_edges & BitStickyEdges.Right) == 0 ? string.Empty : "bit-stk-stc-rgt");

        ClassBuilder.Register(() => _stuck ? StuckClass : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Top.HasValue() ? $"top: {GetValueWithUnit(Top)}" : string.Empty);
        StyleBuilder.Register(() => Bottom.HasValue() ? $"bottom: {GetValueWithUnit(Bottom)}" : string.Empty);
        StyleBuilder.Register(() => Left.HasValue() ? $"left: {GetValueWithUnit(Left)}" : string.Empty);
        StyleBuilder.Register(() => Right.HasValue() ? $"right: {GetValueWithUnit(Right)}" : string.Empty);

        StyleBuilder.Register(() => ZIndex.HasValue ? $"z-index: {ZIndex.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitStickyParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = GetElement();

        builder.OpenElement(0, element);
        // Written before the splatted attributes, so a role the page writes itself wins over it.
        builder.AddAttribute(1, "role", GetRole(element));
        builder.AddMultipleAttributes(2, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(3, "id", _Id);
        // A null value here is not the same as nothing at all: the builder still records the name and
        // drops the attribute of the same name that came out of HtmlAttributes, so the two that are
        // not always written are only added while the parameter itself carries a value, and the
        // splatted one is left alone otherwise.
        if (AriaLabel is not null)
        {
            builder.AddAttribute(4, "aria-label", AriaLabel);
        }
        if (Dir is not null)
        {
            builder.AddAttribute(5, "dir", Dir.Value.ToString().ToLowerInvariant());
        }
        // The stuck style is appended after every other inline style, since the later declaration of
        // the same property is the one an inline style resolves to.
        builder.AddAttribute(6, "style", _stuck ? JoinStyles(StyleBuilder.Value, StuckStyle) : StyleBuilder.Value);
        builder.AddAttribute(7, "class", ClassBuilder.Value);
        builder.AddElementReferenceCapture(8, v => RootElement = v);
        // A void element (a sticky hr, an img) holds no content, and the static renderer drops whatever
        // follows its tag anyway.
        if (IsVoidElement(element) is false)
        {
            builder.AddContent(9, ChildContent);
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        // Setting the script up is a disposal and a setup with the bookkeeping of the attached id in
        // between, so a render arriving while one of those awaits is in flight would interleave with
        // it and could leave the flag naming a registration that is not the one on the element
        // anymore. A render that finds the sequence running only leaves a mark, and the call in
        // flight runs it again afterwards, reading the parameters and the id as they are by then.
        if (_settingUp)
        {
            _setupPending = true;
            return;
        }

        _settingUp = true;

        try
        {
            do
            {
                _setupPending = false;

                await SetupStuckDetection();
            }
            while (_setupPending && IsDisposed is false);
        }
        finally
        {
            // Released even when the interop threw, so the flag cannot keep every later render out of the setup.
            _settingUp = false;
        }
    }



    // The tag the root element is rendered as. A name that is not one a tag can have is not used at
    // all, since a name carrying whitespace or a "<" would write markup of its own rather than name
    // an element, and one document.createElement refuses throws where the renderer builds the
    // element and takes the whole render batch with it.
    private string GetElement()
    {
        var element = Element?.Trim();

        return element.HasValue() && IsValidElement(element!) ? element! : "div";
    }

    // A div or a span is a generic element, which ARIA prohibits naming, so a name given to one - the
    // AriaLabel, or an aria-label or aria-labelledby written on the tag - would be dropped by assistive
    // technologies. Such a sticky is announced as a group instead; every other tag keeps its own role.
    private string? GetRole(string element)
    {
        if (element.Equals("div", StringComparison.OrdinalIgnoreCase) is false &&
            element.Equals("span", StringComparison.OrdinalIgnoreCase) is false) return null;

        var named = AriaLabel.HasValue() ||
                    GetSplattedAttribute("aria-label").HasValue() ||
                    GetSplattedAttribute("aria-labelledby").HasValue();

        return named ? "group" : null;
    }

    // The one place the derived state is written, so the boolean, the edges, the classes and the two
    // callbacks can never disagree about it.
    private async Task SetStuckEdges(BitStickyEdges edges)
    {
        if (_edges == edges) return;

        var stuck = edges != BitStickyEdges.None;
        var flipped = _stuck != stuck;

        _edges = edges;
        _stuck = stuck;

        ClassBuilder.Reset();

        // The boolean is raised first and only where it actually changed: an element carried from one
        // edge of a pair to the other never stopped being stuck.
        if (flipped)
        {
            await OnStuckChanged.InvokeAsync(stuck);
        }

        await OnStuckEdgesChanged.InvokeAsync(edges);

        StateHasChanged();
    }

    private async Task SetupStuckDetection()
    {
        // The script only earns its scroll listener where something observes the state it derives, and
        // its resize observer where the scroll padding has to follow the size of the element. A disabled
        // sticky is not sticky at all, so there is neither a state to derive nor a room to reserve.
        var report = Disabled is false && (OnStuckChanged.HasDelegate ||
                                   OnStuckEdgesChanged.HasDelegate ||
                                   StuckClass.HasValue() ||
                                   StuckStyle.HasValue() ||
                                   ElevateOnStuck);

        var scrollPadding = Disabled is false && ScrollPadding;

        var shouldAttach = report || scrollPadding;

        // The script holds the element it found under that id, and a change of tag does not change the
        // element - it replaces it, leaving the registration watching a node that is not in the document
        // anymore. So the tag is part of what the registration is keyed by, along with what it does.
        var signature = shouldAttach
            ? $"{_Id}|{GetElement()}|{report}|{scrollPadding}"
            : null;

        // What moves the edges and the insets the element pins at without resizing anything - a Position,
        // an offset, a Dir, a Class or a Style of its own, and the stuck class and style as well - is
        // written on the element itself, so the script watches its attributes for it rather than being
        // told about it on every render.
        if (signature == _attachedSignature) return;

        if (_attachedId is not null)
        {
            await _js.BitStickiesDispose(_attachedId);

            // The component can go away while that call is in flight, and its own disposal has
            // released the reference and taken the listeners off the element by the time this
            // resumes. Going on from here would hand the script a disposed reference and leave
            // behind a registration that nothing is left to dispose.
            if (IsDisposed) return;

            _attachedId = null;
            _attachedSignature = null;
        }

        if (shouldAttach)
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);

            await _js.BitStickiesSetup(_Id, _dotnetObj, report, scrollPadding);

            if (IsDisposed)
            {
                try
                {
                    // The disposal of the component may have run its own cleanup before the setup
                    // above came back, in which case the registration just made is the one it could
                    // not see. Disposing an id that is not registered anymore is a no-op, so this is
                    // safe either way.
                    await _js.BitStickiesDispose(_Id);
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here

                return;
            }

            _attachedId = _Id;
            _attachedSignature = signature;
        }

        if (report is false && _edges != BitStickyEdges.None)
        {
            // The state is no longer derived, so it must not stay stuck in a state nothing is left to
            // update. The state flipped, so whoever is watching it hears about it the same way they
            // hear about a flip the script reported - the detachment is not a reason to leave an
            // observer holding a stuck state the component does not have anymore.
            await SetStuckEdges(BitStickyEdges.None);
        }
    }

    /// <summary>
    /// A CSS length from a parameter that also accepts a bare number, which is read as a pixel count.
    /// The number is parsed with the invariant culture, since it is a value written into a stylesheet
    /// rather than one shown to a user: read with the current one, "9.5" would be a different length
    /// in a culture whose decimal separator is the comma, and none at all in the CSS that came out of it.
    /// </summary>
    private static string? GetValueWithUnit(string? val)
    {
        // The infinities and the not-a-number that double.TryParse also accepts by name are numbers no
        // length can be written of, so they are left to the stylesheet as the words they were given as.
        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) &&
            double.IsFinite(result))
        {
            return FormattableString.Invariant($"{result}px");
        }

        return val;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is null) return;

        _dotnetObj.Dispose();
        _dotnetObj = null;

        try
        {
            // The script is keyed by the id it was attached under, which is not the current one
            // anymore when the Id changed after the setup and the component went away before the
            // next render could move the registration over.
            await _js.BitStickiesDispose(_attachedId ?? _Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
