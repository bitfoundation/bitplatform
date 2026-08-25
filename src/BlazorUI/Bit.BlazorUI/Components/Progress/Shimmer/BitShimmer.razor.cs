namespace Bit.BlazorUI;

/// <summary>
/// Shimmer is a temporary animation placeholder for when a service call takes time to return data and you don't want to block rendering the rest of the UI.
/// </summary>
public partial class BitShimmer : BitComponentBase
{
    // What the markup actually swaps on. It follows Loaded exactly unless a MinShowTime is holding a
    // placeholder that has already been seen on the page for the rest of its shortest life.
    private bool _loaded;
    // The moment the current wait began, which is what MinShowTime and ShowDelay are both measured from.
    // Null until there has been a wait at all, so a shimmer that starts out loaded holds nothing back.
    private long? _waitStart;
    private CancellationTokenSource? _holdCts;

    // Circle is the older spelling of Shape.Circle and stays the fallback for a component that was written
    // before the shape had a name of its own; an explicit Shape always wins over it.
    private BitShimmerShape _shape => Shape ?? (Circle ? BitShimmerShape.Circle : BitShimmerShape.Rounded);

    // Pulse is the older spelling of Animation.Pulse, with the same precedence.
    private BitShimmerAnimation _animation => Animation ?? (Pulse ? BitShimmerAnimation.Pulse : BitShimmerAnimation.Wave);

    // A circle is one shape rather than a stack of lines, and a count below one is not a stack either.
    // An overlay is one shape as well: it covers a box whose size the content underneath already decides,
    // and a stack of bars inside that box would be a second layout rather than a cover for the first.
    private int _lineCount => Overlay || _shape == BitShimmerShape.Circle ? 1 : Math.Max(1, Lines);

    // A template draws the placeholder itself, which an overlay cannot let it do: the cover has to be the one
    // box laid over the content, so the template is left to the in-place placeholder it was written for.
    private bool _hasTemplate => Template is not null && Overlay is false;

    // The region is only worth having while there is something for it to say. It stays on the page across the
    // swap either way, so whichever of the two texts is missing simply empties it rather than removing it.
    private bool _hasLiveRegion => Label.HasValue() || LoadedLabel.HasValue();

    private string _ariaLive => Politeness switch
    {
        BitPoliteness.Off => "off",
        BitPoliteness.Assertive => "assertive",
        _ => "polite"
    };

    // The role is paired with the politeness rather than fixed: status carries an implicit polite live region
    // and alert an assertive one, so pinning status onto an assertive region would state one urgency in the
    // role and the opposite in aria-live - which is exactly the conflicting pair screen readers resolve
    // differently from one another.
    private string? _liveRole => Politeness switch
    {
        BitPoliteness.Off => null,
        BitPoliteness.Assertive => "alert",
        _ => "status"
    };

    private string _animationClass => _animation switch
    {
        BitShimmerAnimation.Pulse => "bit-smr-pul",
        BitShimmerAnimation.Fade => "bit-smr-fad",
        BitShimmerAnimation.None => "bit-smr-non",
        _ => "bit-smr-wav"
    };

    // The pace of the animation is a value rather than a class, so it lands on the element as a style. Both
    // parts are dropped for a placeholder that does not animate, where there is nothing for them to time.
    private string _animationStyle
    {
        get
        {
            if (_animation == BitShimmerAnimation.None) return string.Empty;

            var delay = Delay.HasValue ? $"animation-delay:{Delay}ms" : string.Empty;
            var duration = Duration.HasValue ? $"animation-duration:{Duration}ms" : string.Empty;

            return string.Join(';', [delay, duration]).Trim(';').Trim();
        }
    }

    private string _backgroundClass => Background switch
    {
        BitColor.Primary => "bit-smr-bpri",
        BitColor.Secondary => "bit-smr-bsec",
        BitColor.Tertiary => "bit-smr-bter",
        BitColor.Info => "bit-smr-binf",
        BitColor.Success => "bit-smr-bsuc",
        BitColor.Warning => "bit-smr-bwrn",
        BitColor.SevereWarning => "bit-smr-bswr",
        BitColor.Error => "bit-smr-berr",
        BitColor.PrimaryBackground => "bit-smr-bpbg",
        BitColor.SecondaryBackground => "bit-smr-bsbg",
        BitColor.TertiaryBackground => "bit-smr-btbg",
        BitColor.PrimaryForeground => "bit-smr-bpfg",
        BitColor.SecondaryForeground => "bit-smr-bsfg",
        BitColor.TertiaryForeground => "bit-smr-btfg",
        BitColor.PrimaryBorder => "bit-smr-bpbr",
        BitColor.SecondaryBorder => "bit-smr-bsbr",
        BitColor.TertiaryBorder => "bit-smr-btbr",
        _ => "bit-smr-bsbg"
    };



    /// <summary>
    /// The animation the shimmer plays while it stands in for content that has not arrived yet.
    /// <br />
    /// The default value is <strong>Wave</strong>.
    /// </summary>
    /// <remarks>
    /// <see cref="Duration"/> and <see cref="Delay"/> retune whichever animation is chosen, and
    /// <see cref="BitShimmerAnimation.None"/> leaves a static block that neither of them applies to.
    /// <br />
    /// The animation loops for as long as the shimmer is on the page, so it stops outright - rather than
    /// running faster - as soon as the reader asks for reduced motion, unless <c>ForceAnimation</c> says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitShimmerAnimation? Animation { get; set; }

    /// <summary>
    /// The background color of the container of the shimmer.
    /// </summary>
    /// <remarks>
    /// This is the resting color of the placeholder - the box the animation plays over - while
    /// <see cref="Color"/> is the color of the animated part itself.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Background { get; set; }

    /// <summary>
    /// The content that will be shown when the Loaded parameter changes to true.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Renders the shimmer as circle instead of a rectangle.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the short spelling of <c>Shape="BitShimmerShape.Circle"</c>, which wins over it when both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Circle { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitShimmer.
    /// </summary>
    [Parameter] public BitShimmerClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the animated part of the shimmer.
    /// </summary>
    /// <remarks>
    /// This is the color of the wave band, or of the block the pulse and the fade play on, over the resting
    /// <see cref="Background"/> of the placeholder. Keep the two close together: a placeholder is not content,
    /// and a high-contrast pair reads as something the page is actually showing.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>
    /// The animation delay value in ms.
    /// </summary>
    /// <remarks>
    /// This is the CSS <c>animation-delay</c> of the animation: it postpones the start of the first loop, and
    /// the loops that follow it run back to back. It is not the wait before the placeholder itself appears -
    /// that one is <see cref="ShowDelay"/>.
    /// </remarks>
    [Parameter]
    public int? Delay { get; set; }

    /// <summary>
    /// The animation duration value in ms.
    /// </summary>
    /// <remarks>
    /// One full sweep of the wave, or one full breath of the pulse and the fade. A longer duration is calmer on
    /// a page carrying many placeholders at once.
    /// </remarks>
    [Parameter]
    public int? Duration { get; set; }

    /// <summary>
    /// The gap between the lines of a multi-line shimmer, as a CSS length.
    /// </summary>
    /// <remarks>
    /// Only applies while <see cref="Lines"/> is greater than 1. Defaults to the rhythm unit of the theme.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// The shimmer height value.
    /// </summary>
    /// <remarks>
    /// It sizes the placeholder rather than the component: once <see cref="Loaded"/> turns true the content
    /// decides its own height, so the box never crops what it was standing in for.
    /// <br />
    /// With more than one line it is the height of each single line, not of the stack.
    /// <br />
    /// Left unset, the height comes from <see cref="Size"/>, and an <see cref="Overlay"/> takes it from the
    /// content it covers instead.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// Lays the shimmer out in the flow of a line of text instead of as a block of its own.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A block shimmer takes the full width of its container, which is what a placeholder standing in for a
    /// paragraph or a card wants. An inline one sits next to the words around it and takes the width given by
    /// <see cref="Width"/>, falling back to the minimum control width of the theme.
    /// <br />
    /// A <see cref="Height"/> of <c>1em</c> keeps it exactly as tall as the type it sits in, whatever that
    /// type turns out to be.
    /// <br />
    /// The root is rendered as a <c>span</c> rather than a <c>div</c> while it is inline, so a placeholder
    /// standing in the middle of a paragraph is phrasing content and the paragraph stays in one piece.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Inline { get; set; }

    /// <summary>
    /// The text announced by assistive technologies while the shimmer is standing in for content.
    /// </summary>
    /// <remarks>
    /// A placeholder is a visual stand-in and says nothing on its own, so this renders a live region - visible
    /// only to assistive technologies - that carries this text while loading and swaps to
    /// <see cref="LoadedLabel"/> once the content arrives. The swap is what gets announced, which is the point:
    /// a screen reader user is told the wait is over rather than being left on a silent page.
    /// <br />
    /// Set it on the one shimmer that stands for the whole region rather than on each of a group of them, so
    /// that a card built from six placeholders is announced once.
    /// </remarks>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The width of the last line of a multi-line shimmer, as a CSS length.
    /// </summary>
    /// <remarks>
    /// Only applies while <see cref="Lines"/> is greater than 1. The last line of a paragraph rarely reaches the
    /// end of its measure, so it is shortened by default; set it to <c>100%</c> for a stack of even lines.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? LastLineWidth { get; set; }

    /// <summary>
    /// The number of placeholder lines rendered as a stack, which is what a paragraph of text reads as.
    /// <br />
    /// The default value is <strong>1</strong>.
    /// </summary>
    /// <remarks>
    /// Each line takes the <see cref="Height"/> of a single line and they are separated by <see cref="Gap"/>,
    /// with the last one shortened to <see cref="LastLineWidth"/>.
    /// <br />
    /// A <see cref="BitShimmerShape.Circle"/> is a single shape rather than a stack, so it ignores this, and
    /// so does an <see cref="Overlay"/>, which is one box laid over the whole of the content it covers.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public int Lines { get; set; } = 1;

    /// <summary>
    /// Controls when the shimmer is swapped with actual data through an animated transition.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The placeholder and the content are never on the page at the same time: the content replaces the
    /// placeholder and fades in, and the sizing of the placeholder is dropped with it - unless the shimmer
    /// <see cref="Overlay"/>s its content, where the two are on the page together and only one is shown.
    /// <br />
    /// The swap follows this parameter at once, except where <see cref="MinShowTime"/> is holding a
    /// placeholder that has only just appeared.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Loaded { get; set; }

    /// <summary>
    /// The text announced by assistive technologies once the content has replaced the shimmer.
    /// </summary>
    /// <remarks>
    /// It replaces <see cref="Label"/> in the live region, so the change is what a screen reader announces.
    /// Keep it short - "Loaded" - and let the content itself say the rest.
    /// </remarks>
    [Parameter] public string? LoadedLabel { get; set; }

    /// <summary>
    /// The shortest time in ms a placeholder that has been seen stays on the page.
    /// </summary>
    /// <remarks>
    /// <see cref="ShowDelay"/> keeps a fast response from ever showing a placeholder; this keeps a response
    /// that lands just after one has appeared from taking it away again in the same breath, which reads as a
    /// flicker rather than as loading. The two are the two halves of the same answer, and this one is measured
    /// from the moment the placeholder appears, so the wait as a whole is at most the delay plus this.
    /// <br />
    /// Nothing is held back if the response arrives before the placeholder was ever shown.
    /// </remarks>
    [Parameter] public int? MinShowTime { get; set; }

    /// <summary>
    /// Draws the placeholder over the content instead of in place of it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The content is rendered but not shown, so the box keeps the size of the thing it is waiting on and the
    /// page never reflows as the placeholder is swapped out. This is what a section that is being refreshed
    /// wants - the layout is already known - while content arriving for the first time, whose size is not,
    /// is better served by the placeholder standing in for it.
    /// <br />
    /// The cover is one box over the whole content, so <see cref="Lines"/> and <see cref="Template"/> no longer
    /// apply and the size comes from the content rather than from <see cref="Height"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Overlay { get; set; }

    /// <summary>
    /// How urgently the live region of the shimmer interrupts a screen reader.
    /// <br />
    /// The default value is <strong>Polite</strong>.
    /// </summary>
    /// <remarks>
    /// Only applies while <see cref="Label"/> or <see cref="LoadedLabel"/> is set. A wait ending is rarely worth
    /// cutting a screen reader off mid-sentence, so raise it beyond polite only where the content is the reason
    /// the reader is on the page.
    /// </remarks>
    [Parameter] public BitPoliteness Politeness { get; set; } = BitPoliteness.Polite;

    /// <summary>
    /// Changes the animation type of the shimmer to pulse.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// This is the short spelling of <c>Animation="BitShimmerAnimation.Pulse"</c>, which wins over it when both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Pulse { get; set; }

    /// <summary>
    /// The corner radius of the placeholder, as a CSS length.
    /// </summary>
    /// <remarks>
    /// <see cref="Shape"/> already carries the three radii a placeholder usually wants; this is for the corner
    /// that has to match a surface of its own - a card, a thumbnail, a control with a radius the theme does not
    /// have a name for - and it wins over the shape wherever both are set.
    /// <br />
    /// A <see cref="BitShimmerShape.Circle"/> is round by construction, so it ignores this.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Radius { get; set; }

    /// <summary>
    /// The shape of the placeholder the shimmer draws.
    /// <br />
    /// The default value is <strong>Rounded</strong>.
    /// </summary>
    /// <remarks>
    /// Shape the placeholder like the thing it stands in for: a circle for an avatar, a pill for a button or a
    /// tag, a square for an image that meets its container edge to edge.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitShimmerShape? Shape { get; set; }

    /// <summary>
    /// The wait in ms before the placeholder appears.
    /// </summary>
    /// <remarks>
    /// A response that arrives in a few hundred milliseconds turns a placeholder into a flash of grey that is
    /// worse than no placeholder at all. Holding the shimmer back for a moment lets a fast call finish without
    /// ever being seen, while a slow one still gets the placeholder it needs.
    /// <br />
    /// The wait is held in CSS rather than in a timer, so it costs no render and works under static server-side
    /// rendering. It delays the appearance of the placeholder only - the animation of a placeholder already on
    /// the page is timed by <see cref="Delay"/>.
    /// <br />
    /// <see cref="MinShowTime"/> is its other half: this one keeps a placeholder from being shown too soon,
    /// that one from being taken away too soon after it has been.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ShowDelay { get; set; }

    /// <summary>
    /// The size of the shimmer, which is the height of a line and the diameter of a circle.
    /// <br />
    /// The default value is <strong>Medium</strong>.
    /// </summary>
    /// <remarks>
    /// An explicit <see cref="Height"/> or <see cref="Width"/> always wins over it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitShimmer.
    /// </summary>
    [Parameter] public BitShimmerClassStyles? Styles { get; set; }

    /// <summary>
    /// The custom template to replace the default shimmer container and animation.
    /// </summary>
    /// <remarks>
    /// It replaces the placeholder itself, so <see cref="Shape"/>, <see cref="Lines"/>, <see cref="Animation"/>
    /// and the sizing parameters no longer apply: the template is built out of shimmers of its own, each with
    /// its own shape and size, which is how a card or a list row is drawn.
    /// <br />
    /// <see cref="ShowDelay"/> still applies, so a whole templated skeleton is held back as one.
    /// <br />
    /// An <see cref="Overlay"/> is a single box laid over the content rather than a layout of its own, so it
    /// leaves the template to the in-place placeholder it was written for.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? Template { get; set; }

    /// <summary>
    /// The shimmer width value.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Height"/> this stays with the component after the swap, so a placeholder and the
    /// content that replaces it occupy the same column.
    /// <br />
    /// A block shimmer takes the full width of its container when it is not set, and a
    /// <see cref="BitShimmerShape.Circle"/> takes its diameter from <see cref="Height"/> or <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    protected override string RootElementClass => "bit-smr";

    protected override void OnParametersSet()
    {
        if (Loaded is false)
        {
            // A wait that is starting is one the placeholder is measured from; a wait already under way keeps
            // the moment it started, so a rerender in the middle of one does not push its shortest life out.
            _waitStart ??= Environment.TickCount64;
            CancelHold();
            SetLoaded(false);
        }
        else if (_loaded is false && _holdCts is null)
        {
            var shown = ShowDelay ?? 0;
            var elapsed = _waitStart.HasValue ? Environment.TickCount64 - _waitStart.Value : 0;
            var remaining = _waitStart.HasValue ? shown + (MinShowTime ?? 0) - elapsed : 0;

            // Nothing is held back for a placeholder that was never seen - the response beat the ShowDelay
            // that was holding it back - nor for one that has already lived out its shortest life.
            if (MinShowTime.HasValue is false || elapsed < shown || remaining <= 0)
            {
                _waitStart = null;
                SetLoaded(true);
            }
            else
            {
                HoldPlaceholder((int)remaining);
            }
        }

        base.OnParametersSet();
    }

    // The swap is what the class and the style builders are keyed on, so a swap that is deferred defers their
    // reset with it: the ResetClassBuilder on the parameter fires when the parameter changes, which is not
    // necessarily when the placeholder goes away.
    private void SetLoaded(bool value)
    {
        if (_loaded == value) return;

        _loaded = value;

        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private void HoldPlaceholder(int duration)
    {
        var cts = _holdCts = new CancellationTokenSource();

        _ = HoldPlaceholderAsync(duration, cts);
    }

    private async Task HoldPlaceholderAsync(int duration, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(duration, cts.Token);

            if (cts.IsCancellationRequested || IsDisposed) return;

            await InvokeAsync(() =>
            {
                if (cts.IsCancellationRequested || IsDisposed) return;

                _holdCts = null;

                // The response the wait was held for may have been taken back in the meantime, in which case
                // the placeholder is still the right thing to be showing and there is nothing to swap.
                if (Loaded is false) return;

                _waitStart = null;
                SetLoaded(true);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException) { }
        finally
        {
            cts.Dispose();
        }
    }

    private void CancelHold()
    {
        if (_holdCts is null) return;

        _holdCts.Cancel();
        _holdCts.Dispose();
        _holdCts = null;
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        CancelHold();

        return base.DisposeAsync(disposing);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Width.HasValue() ? $"width:{Width}" : string.Empty);

        // The height is published as a custom property rather than being set on the root: with more than one
        // line it is each line that has to read it, and once the content is in it is nobody's business at all.
        // The four of them are dropped after the swap for the same reason the sizing classes are - what the
        // content is laid out by is the page - and a custom property that is not published is also one that
        // cannot be inherited by the shimmers a Template is built out of.
        StyleBuilder.Register(() => _loaded is false && Height.HasValue() ? $"--bit-smr-hgt:{Height}" : string.Empty);
        StyleBuilder.Register(() => _loaded is false && Gap.HasValue() ? $"--bit-smr-gap:{Gap}" : string.Empty);
        StyleBuilder.Register(() => _loaded is false && LastLineWidth.HasValue() ? $"--bit-smr-llw:{LastLineWidth}" : string.Empty);
        StyleBuilder.Register(() => _loaded is false && Radius.HasValue() ? $"--bit-smr-rad:{Radius}" : string.Empty);
        StyleBuilder.Register(() => _loaded is false && ShowDelay.HasValue ? $"--bit-smr-dly:{ShowDelay}ms" : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // Everything that shapes and sizes the placeholder is dropped once the content is in, so the box the
        // shimmer was drawn in never crops or stretches what replaced it. A template is not that box either:
        // it brings boxes of its own, and a height meant for a single bar would crop the whole skeleton. What
        // the root keeps in that case is the entry animation - and with it the ShowDelay that holds the
        // skeleton back as one.
        ClassBuilder.Register(() => _loaded
                                  ? "bit-smr-ldd"
                                  : _hasTemplate
                                    ? "bit-smr-tpl"
                                    : _shape switch
                                    {
                                        BitShimmerShape.Circle => "bit-smr-crl",
                                        BitShimmerShape.Square => "bit-smr-lin bit-smr-sqr",
                                        BitShimmerShape.Pill => "bit-smr-lin bit-smr-pil",
                                        _ => "bit-smr-lin"
                                    });

        // The cover takes its box from the content it is laid over, so it comes after the shape class and
        // hands back the width and the height the shape would otherwise have imposed - the corner it draws
        // is all that is left of the shape.
        ClassBuilder.Register(() => _loaded is false && Overlay ? "bit-smr-ovl" : string.Empty);

        ClassBuilder.Register(() => _loaded is false && _hasTemplate is false && _lineCount > 1 ? "bit-smr-mln" : string.Empty);

        ClassBuilder.Register(() => Inline ? "bit-smr-inl" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-smr-sm",
            BitSize.Medium => "bit-smr-md",
            BitSize.Large => "bit-smr-lg",
            _ => "bit-smr-md"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-smr-pri",
            BitColor.Secondary => "bit-smr-sec",
            BitColor.Tertiary => "bit-smr-ter",
            BitColor.Info => "bit-smr-inf",
            BitColor.Success => "bit-smr-suc",
            BitColor.Warning => "bit-smr-wrn",
            BitColor.SevereWarning => "bit-smr-swr",
            BitColor.Error => "bit-smr-err",
            BitColor.PrimaryBackground => "bit-smr-pbg",
            BitColor.SecondaryBackground => "bit-smr-sbg",
            BitColor.TertiaryBackground => "bit-smr-tbg",
            BitColor.PrimaryForeground => "bit-smr-pfg",
            BitColor.SecondaryForeground => "bit-smr-sfg",
            BitColor.TertiaryForeground => "bit-smr-tfg",
            BitColor.PrimaryBorder => "bit-smr-pbr",
            BitColor.SecondaryBorder => "bit-smr-sbr",
            BitColor.TertiaryBorder => "bit-smr-tbr",
            _ => "bit-smr-tbg"
        });
    }
}
