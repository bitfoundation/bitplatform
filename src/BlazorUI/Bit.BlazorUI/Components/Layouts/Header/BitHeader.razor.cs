using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The BitHeader component renders a bar (with a title and possibly other components) at the top of a site or an application.
/// </summary>
/// <remarks>
/// It renders a semantic <c>header</c> element and lays its content out in a horizontal line whose color, variant,
/// size, alignment, wrapping and gutters are all parameters. It can stay in the flow of the page or be pinned to the
/// top of the viewport - <see cref="Fixed"/>, <see cref="Sticky"/>, revealing itself only while the page is scrolled
/// up (<see cref="Reveal"/>), lifting itself off the content once the page is scrolled at all
/// (<see cref="ElevateOnScroll"/>), or slid out of the way on demand (<see cref="Hidden"/>).
/// </remarks>
public partial class BitHeader : BitComponentBase
{
    private bool _hidden;
    private bool _scrolled;
    private bool _slidable;
    private string? _attachedId;
    private string? _attachedSignature;
    private DotNetObjectReference<BitHeader>? _dotnetObj;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the header component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple header components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitHeaderParams.ParamName)]
    public BitHeaderParams? CascadingParameters { get; set; }



    /// <summary>
    /// Renders the header with an absolute position at the top of its nearest positioned ancestor.
    /// </summary>
    /// <remarks>
    /// This is <see cref="Fixed"/> scoped to a box instead of to the page: the header of a card, a panel or a dialog
    /// pins itself to the top of that container and scrolls away with it, rather than staying on the screen.
    /// It needs an ancestor with a position other than static to pin itself to, and it overlaps the content of that
    /// ancestor just like a fixed header overlaps the page.
    /// <br />
    /// <see cref="Fixed"/> takes precedence over it, and it takes precedence over <see cref="Sticky"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Absolute { get; set; }

    /// <summary>
    /// Gets or sets the horizontal distribution of the content of the BitHeader (the CSS justify-content of the container).
    /// </summary>
    /// <remarks>
    /// The content of the header is laid out in a horizontal flex line, so this controls how the remaining
    /// free space of that line is shared between and around the children.
    /// <br />
    /// Baseline and Stretch say nothing about distributing that free space, so those two act on the cross axis
    /// (the vertical alignment of the content) instead, exactly like <see cref="VerticalAlign"/>, which takes
    /// precedence over them when it is set.
    /// <br />
    /// When not set, the content keeps the browser default (packed to the start of the line).
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Renders a divider line on the bottom edge of the BitHeader to separate it from the content below.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Bordered { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered inside the BitHeader.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitHeader.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitHeaderClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the BitHeader.
    /// </summary>
    /// <remarks>
    /// The color is applied through the <see cref="Variant"/>: as the background color in the Fill variant,
    /// and as the text and border color in the Outline and Text variants.
    /// <br />
    /// When not set, the header keeps the primary background and foreground colors of the current theme.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Gets or sets how far (in pixels) the scroll has to travel from the top before an
    /// <see cref="ElevateOnScroll"/> header lifts itself off the content.
    /// </summary>
    /// <remarks>
    /// When not set (or set to 0), the header gains its shadow as soon as the scroll leaves the top.
    /// </remarks>
    [Parameter] public int? ElevateOffset { get; set; }

    /// <summary>
    /// Keeps the BitHeader flat while the scrolling area sits at its top and lets it cast its shadow only once
    /// the content has been scrolled underneath it.
    /// </summary>
    /// <remarks>
    /// A pinned header that is shadowed from the start looks detached from a page that has not moved yet, and a
    /// header with no shadow at all gives no hint that content is passing behind it. Elevating on scroll is what
    /// says "you are no longer at the top" without spending anything while the user is.
    /// <br />
    /// It only has an effect on a <see cref="Fixed"/> or <see cref="Sticky"/> header, since a header in the normal
    /// flow scrolls away with the content it would have to lift itself above. <see cref="Elevated"/> takes
    /// precedence over the shadow it adds - a header that is always elevated has nothing left to gain from a
    /// scroll - but <see cref="OnScrolledChanged"/> and <see cref="IsScrolled"/> keep reporting the state either way.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ElevateOnScroll { get; set; }

    /// <summary>
    /// Renders the BitHeader with a shadow cast downwards, to lift it above the content it overlaps.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Elevated { get; set; }

    /// <summary>
    /// Renders the header with a fixed position at the top of the page.
    /// </summary>
    /// <remarks>
    /// A fixed header is taken out of the normal flow, so it overlaps the content it covers.
    /// Reserve room for it at the start of the page (for example with a top padding) to keep the first
    /// piece of content reachable.
    /// <br />
    /// Takes precedence over <see cref="Absolute"/> and <see cref="Sticky"/> when more than one of them is set.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Fixed { get; set; }

    /// <summary>
    /// Gets or sets the space between the children of the BitHeader (the CSS gap of the container).
    /// </summary>
    /// <remarks>
    /// Takes any CSS length or the two value form of the gap shorthand (for example <c>0.5rem</c> or <c>4px 8px</c>).
    /// <br />
    /// When not set, the children of the header sit right next to each other, so anything that needs
    /// breathing room between its parts either sets this or brings its own spacing.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Gets or sets the height of the BitHeader (in pixels).
    /// </summary>
    /// <remarks>
    /// The height includes the paddings and the border of the header (the root element is a border-box).
    /// <br />
    /// A <see cref="Fixed"/> or <see cref="Sticky"/> header adds the top safe area inset of the device on
    /// top of it, so the content of the header keeps the height that was asked for instead of losing part of
    /// it to the status bar or the notch.
    /// <br />
    /// When not set, the header is as tall as its content plus the paddings of the current <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Height { get; set; }

    /// <summary>
    /// Slides the BitHeader out of the view, and brings it back when it is turned off again.
    /// </summary>
    /// <remarks>
    /// This is the programmatic counterpart of <see cref="Reveal"/>: the header slides away because the application
    /// says so rather than because the page is being scrolled, which is what a bar that only belongs to part of a
    /// workflow (a distraction free reading mode, a full screen media view) needs.
    /// <br />
    /// A hidden header is also marked <c>inert</c>, so nothing inside it can be clicked or reached with the keyboard
    /// while it is out of the view. Unlike <see cref="BitComponentBase.Visibility"/>, which switches the header off at
    /// once, this slides it in and out and keeps the room it occupies in the layout.
    /// <br />
    /// It only slides over a <see cref="Fixed"/> or <see cref="Sticky"/> header; a header in the normal flow is
    /// translated over whatever sits above it in the page.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Hidden { get; set; }

    /// <summary>
    /// Removes the default paddings around the content of the BitHeader, so it can span the full width of the header.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoGutter { get; set; }

    /// <summary>
    /// Callback for when the reveal state of the header changes. The provided value is true when the header is revealed.
    /// </summary>
    /// <remarks>
    /// Only invoked while <see cref="Reveal"/> is enabled.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnRevealChanged { get; set; }

    /// <summary>
    /// Callback for when the scrolled state of the header changes. The provided value is true once the scrolling
    /// area has travelled past the <see cref="ElevateOffset"/>.
    /// </summary>
    /// <remarks>
    /// Only invoked while <see cref="ElevateOnScroll"/> is enabled. It is what a header that has to do more than
    /// cast a shadow at that moment - shrink itself, swap a logo for a compact one, reveal a search box - hooks into.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnScrolledChanged { get; set; }

    /// <summary>
    /// Slides the header out of the view while the page is scrolled down and brings it back while the page is scrolled up.
    /// </summary>
    /// <remarks>
    /// This only has an effect on a <see cref="Fixed"/> or <see cref="Sticky"/> header, since a header in the normal
    /// flow has nothing to slide over, and an <see cref="Absolute"/> one scrolls away with its container anyway.
    /// The header is always revealed at the very top of the scrolling area.
    /// <br />
    /// Unlike <see cref="Hidden"/>, a header rolled up by the scroll stays reachable: it comes back as soon as
    /// anything inside it takes the focus, so a keyboard user is never stranded on a control they cannot see.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reveal { get; set; }

    /// <summary>
    /// Gets or sets how far (in pixels) the scroll has to travel from the top before a <see cref="Reveal"/>
    /// header starts hiding itself.
    /// </summary>
    /// <remarks>
    /// The header stays revealed while the scroll is still within this offset, which keeps it from
    /// flickering away on the first few pixels of a scroll that has barely started.
    /// <br />
    /// When not set (or set to 0), the header starts hiding as soon as the scroll goes down.
    /// </remarks>
    [Parameter] public int? RevealOffset { get; set; }

    /// <summary>
    /// The size of the BitHeader, which determines the paddings around its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets the target of the skip link of the BitHeader, which is what makes it render at all.
    /// </summary>
    /// <remarks>
    /// A header is the block of links every keyboard and screen reader user has to walk through before they
    /// reach anything they came for, and a skip link is the shortcut past it (WCAG 2.4.1, Bypass Blocks).
    /// It is rendered as the very first focusable element of the header and stays out of sight until it is
    /// focused, so it costs a sighted user nothing.
    /// <br />
    /// The value is a plain href, so it points at the id of the main content of the page (for example
    /// <c>#main</c>). Give that target a <c>tabindex="-1"</c> so the browser really moves the focus there
    /// instead of only scrolling to it.
    /// </remarks>
    [Parameter] public string? SkipLinkHref { get; set; }

    /// <summary>
    /// Gets or sets the text of the skip link of the BitHeader.
    /// </summary>
    /// <remarks>
    /// Defaults to "Skip to main content". It is only rendered when a <see cref="SkipLinkHref"/> is provided.
    /// </remarks>
    [Parameter] public string? SkipLinkText { get; set; }

    /// <summary>
    /// Renders the header with a sticky position at the top of the viewport.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Fixed"/>, a sticky header stays in the normal flow, so it never overlaps the content
    /// and needs no extra room reserved for it. It requires an ancestor that scrolls (commonly the page itself)
    /// and no ancestor with an overflow other than visible.
    /// <br />
    /// It is the position of last resort: both <see cref="Fixed"/> and <see cref="Absolute"/> take precedence over it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Sticky { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitHeader.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitHeaderClassStyles? Styles { get; set; }

    /// <summary>
    /// Softens the background of the BitHeader and blurs what passes behind it, for the frosted glass look
    /// of a header pinned over scrolling content.
    /// </summary>
    /// <remarks>
    /// Only the Fill variant has a background to soften, so this has no effect on the Outline and Text variants,
    /// whose background is already transparent.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Translucent { get; set; }

    /// <summary>
    /// The visual variant of the BitHeader.
    /// </summary>
    /// <remarks>
    /// Fill (the default) paints the <see cref="Color"/> as the background of the header, Outline keeps the
    /// background transparent and draws a border, and Text keeps the background transparent with no border.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment of the content of the BitHeader (the CSS align-items of the container).
    /// </summary>
    /// <remarks>
    /// Only Start, End, Center, Baseline and Stretch align a line on the cross axis, so the three space distributions
    /// of <see cref="BitAlignment"/> have no meaning here and are ignored.
    /// <br />
    /// When not set, the content is centered in the height of the header, which is what a bar of mixed content
    /// (a logo next to a row of buttons) wants.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Lets the content of the BitHeader wrap onto more than one line instead of being squeezed into a single one.
    /// </summary>
    /// <remarks>
    /// A header with a brand, a set of navigation links and a row of account actions runs out of room on a narrow
    /// screen, where wrapping is what keeps it readable instead of shrinking every item.
    /// <br />
    /// The lines of a wrapped header are packed by <see cref="VerticalAlign"/> (centered by default) and separated
    /// by the row part of <see cref="Gap"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Wrap { get; set; }



    /// <summary>
    /// Gets a value indicating whether the header is currently revealed. It is always true unless <see cref="Reveal"/> is enabled.
    /// </summary>
    /// <remarks>
    /// This reports the scroll driven reveal state alone, so it stays true for a header that was slid out of the
    /// view with <see cref="Hidden"/>.
    /// </remarks>
    public bool IsRevealed => _hidden is false;

    /// <summary>
    /// Gets a value indicating whether the scrolling area of the header has travelled past the <see cref="ElevateOffset"/>.
    /// It is always false unless <see cref="ElevateOnScroll"/> is enabled.
    /// </summary>
    public bool IsScrolled => _scrolled;



    /// <summary>
    /// Called by the scroll script of the header when the scroll direction of the page flips.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    [JSInvokable("OnRevealChange")]
    public async Task _OnRevealChange(bool hidden)
    {
        // The script is disposed asynchronously, so a scroll of the very last frame can still land here
        // after the component is gone, where there is nothing left to re-render.
        if (IsDisposed) return;

        if (_hidden == hidden) return;

        _hidden = hidden;

        ClassBuilder.Reset();

        await OnRevealChanged.InvokeAsync(hidden is false);

        StateHasChanged();
    }

    /// <summary>
    /// Called by the scroll script of the header when the scrolling area crosses the elevate offset.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    [JSInvokable("OnScrollChange")]
    public async Task _OnScrollChange(bool scrolled)
    {
        if (IsDisposed) return;

        if (_scrolled == scrolled) return;

        _scrolled = scrolled;

        ClassBuilder.Reset();

        await OnScrolledChanged.InvokeAsync(scrolled);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-hdr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-hdr-pri",
            BitColor.Secondary => "bit-hdr-sec",
            BitColor.Tertiary => "bit-hdr-ter",
            BitColor.Info => "bit-hdr-inf",
            BitColor.Success => "bit-hdr-suc",
            BitColor.Warning => "bit-hdr-wrn",
            BitColor.SevereWarning => "bit-hdr-swr",
            BitColor.Error => "bit-hdr-err",
            BitColor.PrimaryBackground => "bit-hdr-pbg",
            BitColor.SecondaryBackground => "bit-hdr-sbg",
            BitColor.TertiaryBackground => "bit-hdr-tbg",
            BitColor.PrimaryForeground => "bit-hdr-pfg",
            BitColor.SecondaryForeground => "bit-hdr-sfg",
            BitColor.TertiaryForeground => "bit-hdr-tfg",
            BitColor.PrimaryBorder => "bit-hdr-pbr",
            BitColor.SecondaryBorder => "bit-hdr-sbr",
            BitColor.TertiaryBorder => "bit-hdr-tbr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-hdr-fil",
            BitVariant.Outline => "bit-hdr-otl",
            BitVariant.Text => "bit-hdr-txt",
            _ => "bit-hdr-fil"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-hdr-sm",
            BitSize.Medium => "bit-hdr-md",
            BitSize.Large => "bit-hdr-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "bit-hdr-srt",
            BitAlignment.End => "bit-hdr-end",
            BitAlignment.Center => "bit-hdr-cnt",
            BitAlignment.SpaceBetween => "bit-hdr-sbt",
            BitAlignment.SpaceAround => "bit-hdr-sar",
            BitAlignment.SpaceEvenly => "bit-hdr-sev",
            // Baseline and Stretch distribute nothing, so they fall through to the cross axis registration below.
            _ => string.Empty
        });

        // The cross axis takes VerticalAlign, and falls back to the two members of Alignment that only make sense
        // there, so a baseline or stretched header can be spelled either way.
        ClassBuilder.Register(() => (VerticalAlign ?? (Alignment is BitAlignment.Baseline or BitAlignment.Stretch ? Alignment : null)) switch
        {
            BitAlignment.Start => "bit-hdr-vst",
            BitAlignment.End => "bit-hdr-ved",
            BitAlignment.Center => "bit-hdr-vcn",
            BitAlignment.Baseline => "bit-hdr-bsl",
            BitAlignment.Stretch => "bit-hdr-str",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Wrap ? "bit-hdr-wrp" : string.Empty);

        // An element has one position, so the three cannot be combined and the widest reach wins: Fixed pins
        // the header to the page, Absolute pins it to its container, and Sticky leaves it in the flow.
        ClassBuilder.Register(() => Fixed
                                    ? "bit-hdr-fix"
                                    : (Absolute ? "bit-hdr-abs" : (Sticky ? "bit-hdr-stk" : string.Empty)));

        ClassBuilder.Register(() => Translucent ? "bit-hdr-trs" : string.Empty);

        ClassBuilder.Register(() => Bordered ? "bit-hdr-brd" : string.Empty);

        // The shadow of a header that only elevates on scroll fades in and out rather than appearing at once,
        // so the transition is registered for as long as the scroll can still change the state.
        ClassBuilder.Register(() => (ElevateOnScroll && Elevated is false) ? "bit-hdr-esc" : string.Empty);

        ClassBuilder.Register(() => (Elevated || (ElevateOnScroll && _scrolled)) ? "bit-hdr-elv" : string.Empty);

        ClassBuilder.Register(() => NoGutter ? "bit-hdr-ngt" : string.Empty);

        // The slide is only worth a compositor layer on a header that can actually slide: one driven by the scroll,
        // or one that has been handed a Hidden state. The flag is sticky, so the transition stays in place while
        // Hidden is false and the way back in is animated just like the way out.
        ClassBuilder.Register(() => (Reveal || _slidable) ? "bit-hdr-anm" : string.Empty);

        ClassBuilder.Register(() => (Hidden || (Reveal && _hidden)) ? "bit-hdr-hdn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // A pinned header pads itself with the top safe area inset of the device, and the root is a
        // border-box, so an explicit height would be eaten into by that inset and leave the content of the
        // header shorter than it was asked to be. Adding the inset to the height keeps the two apart: the
        // height is the header, the inset is the room the device asks for above it. env() resolves to
        // the 0px fallback wherever there is no inset, which leaves the plain height untouched.
        StyleBuilder.Register(() => Height.HasValue
                                    ? ((Fixed || Sticky)
                                        ? $"height:calc({Height}px + env(safe-area-inset-top, 0px))"
                                        : $"height:{Height}px")
                                    : string.Empty);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-hdr-gap:{Gap}" : string.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitHeaderParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // A header whose Hidden is bound at all is going to slide sooner or later, so it is made animatable up
        // front: a transition that only arrives with the state it is meant to animate does not run for that first
        // change. The cascaded spelling can only be recognized once it actually hides, which is why Hidden itself
        // is checked as well.
        if (_slidable is false && (Hidden || HasNotBeenSet(nameof(Hidden)) is false))
        {
            _slidable = true;

            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        // Only a positioned header has anything to slide over or to lift itself above, so the scroll listener
        // is attached for those alone. Toggling any of these parameters at runtime attaches or detaches it
        // accordingly. Absolute is skipped: it scrolls away with its container, and the position classes give
        // it precedence over Sticky, so a header rendered as bit-hdr-abs has nothing to react to.
        var shouldAttach = (Reveal || ElevateOnScroll) && (Fixed || (Sticky && Absolute is false));

        // Everything the script is handed at setup time is part of the signature, so a change of any of it sets
        // the listener up again - including the id, which is what the script keys its registration by, and which
        // would otherwise leave the listeners behind on the element that id no longer names.
        var revealOffset = Math.Max(0, RevealOffset.GetValueOrDefault());
        var elevateOffset = Math.Max(0, ElevateOffset.GetValueOrDefault());
        var signature = shouldAttach ? $"{_Id}|{revealOffset}|{elevateOffset}|{Reveal}|{ElevateOnScroll}" : null;

        if (signature == _attachedSignature) return;

        if (_attachedId is not null)
        {
            await _js.BitHeadersDispose(_attachedId);

            _attachedId = null;
            _attachedSignature = null;
        }

        if (shouldAttach)
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);

            await _js.BitHeadersSetup(_Id, _dotnetObj, revealOffset, elevateOffset, Reveal, ElevateOnScroll);

            // The flags are only moved once the interop call has gone through, so a call that failed
            // leaves them as they were and the next render tries to set the listener up again.
            _attachedId = _Id;
            _attachedSignature = signature;
        }
        else if (_hidden || _scrolled)
        {
            // The header is no longer driven by the script, so it must not stay stuck in a scroll driven state.
            _hidden = false;
            _scrolled = false;

            ClassBuilder.Reset();

            StateHasChanged();
        }
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
            // The listener is keyed by the id it was attached under, which is not the current one
            // anymore when the Id changed after the setup and the component went away before the
            // next render could move the registration over.
            await _js.BitHeadersDispose(_attachedId ?? _Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
