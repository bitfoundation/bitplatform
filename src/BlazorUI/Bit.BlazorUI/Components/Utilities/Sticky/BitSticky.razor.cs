using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// A Sticky is a component that enables elements to stick during scrolling.
/// </summary>
/// <remarks>
/// The component is a thin, dependable wrapper over the browser's own <c>position: sticky</c>: the
/// content stays in the normal flow - keeping the room it occupies, so nothing jumps when it pins -
/// until the scroll carries it to an edge of its nearest scrolling container, where it stays while
/// the rest of the content passes. Which edge is either a <see cref="Position"/> (the two vertical
/// edges, the two horizontal ones - named Start and End, so they follow the reading direction - or
/// both of a pair) or an exact offset from any of the four sides (<see cref="Top"/>,
/// <see cref="Bottom"/>, <see cref="Left"/>, <see cref="Right"/>). With none of them set, it sticks
/// to the top.
/// <br />
/// CSS has no event for the moment an element actually pins, so the component derives one:
/// <see cref="OnStuckChanged"/> reports the flips, <see cref="IsStuck"/> holds the current state,
/// and <see cref="StuckClass"/> / <see cref="StuckStyle"/> are applied only while stuck - which is
/// what a header that casts a shadow only once content passes under it needs. The detection is only
/// wired up when one of those three is used, so a sticky that does not ask for it stays pure CSS.
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
    private DotNetObjectReference<BitSticky>? _dotnetObj;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Specifying the vertical position of a positioned element from bottom.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// ("2rem", "10%", "calc(1rem + 2px)") is accepted. Setting any of the four offsets replaces the
    /// default stick-to-top behavior with exactly the edges the offsets name, and an offset set
    /// alongside a <see cref="Position"/> overrides that side of it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Bottom { get; set; }

    /// <summary>
    /// The content of the Sticky, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from left.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// is accepted. Horizontal sticking needs a container that scrolls horizontally, and the offset
    /// names a physical side - for a side that follows the reading direction, use the Start and End
    /// members of <see cref="Position"/> instead.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Left { get; set; }

    /// <summary>
    /// Callback for when the stuck state of the component changes. The provided value is true while
    /// the element is pinned to an edge of its scrolling container.
    /// </summary>
    /// <remarks>
    /// CSS itself has no such event, so the state is derived by a small script watching the scroll.
    /// The script is only attached while this callback, <see cref="StuckClass"/> or
    /// <see cref="StuckStyle"/> is used, and only while the component is enabled.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnStuckChanged { get; set; }

    /// <summary>
    /// Region to render sticky component in.
    /// </summary>
    /// <remarks>
    /// Top, Bottom and TopAndBottom pin the element while the container scrolls vertically; Start,
    /// End and StartAndEnd pin it while the container scrolls horizontally, and follow the reading
    /// direction (Start is left in LTR and right in RTL). When neither a Position nor any offset is
    /// set, the component sticks to the top.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitStickyPosition? Position { get; set; }

    /// <summary>
    /// Specifying the horizontal position of a positioned element from right.
    /// </summary>
    /// <remarks>
    /// A bare number is read as a pixel count; anything else is used as written, so any CSS length
    /// is accepted. Horizontal sticking needs a container that scrolls horizontally, and the offset
    /// names a physical side - for a side that follows the reading direction, use the Start and End
    /// members of <see cref="Position"/> instead.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? Right { get; set; }

    /// <summary>
    /// The CSS class applied to the root element only while the component is stuck.
    /// </summary>
    /// <remarks>
    /// This is what styles the pinned state differently from the flowing one - a shadow, an opaque
    /// background, a border once content passes underneath. Using it attaches the same stuck
    /// detection that drives <see cref="OnStuckChanged"/>, and the component also carries the
    /// <c>bit-stk-stc</c> class while stuck.
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
    /// alongside a <see cref="Position"/> overrides that side of it.
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
    /// Raise it where positioned content in the same stacking context has to pass underneath.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ZIndex { get; set; }



    /// <summary>
    /// Gets a value indicating whether the component is currently stuck to an edge of its scrolling
    /// container. It is always false unless <see cref="OnStuckChanged"/>, <see cref="StuckClass"/>
    /// or <see cref="StuckStyle"/> is used, since those are what attach the stuck detection.
    /// </summary>
    public bool IsStuck => _stuck;



    /// <summary>
    /// Called by the scroll script of the component when the stuck state of the element flips.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    [JSInvokable("OnStuckChange")]
    public async Task _OnStuckChange(bool stuck)
    {
        // The script is disposed asynchronously, so a scroll of the very last frame can still land
        // here after the component is gone, where there is nothing left to re-render - or after it
        // was disabled, where the report is of a stickiness that is already off and nothing would be
        // left to clear the state it latched.
        if (IsDisposed || IsEnabled is false) return;

        if (_stuck == stuck) return;

        _stuck = stuck;

        ClassBuilder.Reset();

        await OnStuckChanged.InvokeAsync(stuck);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-stk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Position switch
        {
            BitStickyPosition.Top => "bit-stk-top",
            BitStickyPosition.Bottom => "bit-stk-btm",
            BitStickyPosition.TopAndBottom => "bit-stk-tab",
            BitStickyPosition.Start => "bit-stk-srt",
            BitStickyPosition.End => "bit-stk-end",
            BitStickyPosition.StartAndEnd => "bit-stk-sae",
            _ => (Top.HasNoValue() && Bottom.HasNoValue() && Left.HasNoValue() && Right.HasNoValue())
                    ? "bit-stk-top"
                    : string.Empty
        });

        ClassBuilder.Register(() => _stuck ? "bit-stk-stc" : string.Empty);

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

    private async Task SetupStuckDetection()
    {
        // The script only earns its scroll listener where something observes the state it derives,
        // and a disabled sticky is not sticky at all, so there is no state left to derive.
        var shouldAttach = IsEnabled && (OnStuckChanged.HasDelegate || StuckClass.HasValue() || StuckStyle.HasValue());

        var attachId = shouldAttach ? _Id : null;

        if (attachId == _attachedId) return;

        if (_attachedId is not null)
        {
            await _js.BitStickiesDispose(_attachedId);

            // The component can go away while that call is in flight, and its own disposal has
            // released the reference and taken the listeners off the element by the time this
            // resumes. Going on from here would hand the script a disposed reference and leave
            // behind a registration that nothing is left to dispose.
            if (IsDisposed) return;

            _attachedId = null;
        }

        if (shouldAttach)
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);

            await _js.BitStickiesSetup(_Id, _dotnetObj);

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
        }
        else if (_stuck)
        {
            // The element is no longer watched, so it must not stay stuck in a state nothing is left
            // to update.
            _stuck = false;

            ClassBuilder.Reset();

            StateHasChanged();
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
        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
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
