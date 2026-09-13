using System.Globalization;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A slider provides a visual indication of adjustable content, as well as the current setting in the total
/// range of content. It can select a single value or a range of two, run horizontally or vertically, fill from
/// either end or from an arbitrary origin, carry marks along its track and a floating label on its thumb, and
/// is a form field that takes part in validation like every other input.
/// </summary>
public partial class BitSlider : BitInputBase<double>
{
    // The raw values of the two native inputs of a ranged slider. They are deliberately not kept sorted:
    // dragging the lower thumb past the upper one swaps which end each input represents, and re-sorting
    // them mid-drag would yank the thumb the pointer is holding back to the other side.
    private double _thumbA;
    private double _thumbB;

    // Whether the last pass had to move an end of the range to satisfy the scale or the distance constraints.
    // The correction itself happens in OnParametersSet, but telling the parent about it has to wait for the
    // async half of the lifecycle, since an EventCallback cannot be awaited from the synchronous one.
    private bool _rangeCorrected;

    // The values a correction was last reported for. A parent that does not store what it is told - a one-way
    // binding with a Changed callback that goes nowhere - would otherwise hand the same rejected values back
    // on the render the report itself caused, and the two would keep correcting each other forever.
    private (double Lower, double Upper)? _reportedRange;
    private double? _reportedValue;

    // Whether the change now being handled was produced by a key rather than by the pointer. Snapping to the
    // marks has to tell the two apart: a key is a request to move on, and has to be carried to the next mark
    // however short a Step is against the gap between them, while a pointer belongs at the mark nearest to
    // where it actually is and nowhere further.
    private bool _keyboardChange;

    // Whether that key was one of the larger jumps - a Page key, or an arrow held with Shift - which is
    // re-measured against LargeStep instead of being left at whatever distance the browser chose.
    private bool _largeStepChange;

    // The value a native input has been left holding while it disagrees with the value the component holds.
    // It is rendered for exactly one pass, which is what makes the pass after it a change the diff will carry
    // all the way to the element - see ResyncInputAsync.
    private double? _rawValue;
    private double? _rawA;
    private double? _rawB;
    private double? _rawBar;

    // The keydown handler exists only to note where the next change is coming from, and a render per keystroke
    // for a flag nothing is drawn from would double the round trips of every arrow key. Bound to a receiver
    // that is not the component, it does not go through ComponentBase and so does not re-render: the input
    // event that follows is what changes the value, and it renders the way it always did.
    private EventCallback<KeyboardEventArgs> _onKeyDown;

    // The same for the pointer, which has to clear what a key left behind: a key that moves nothing - an
    // ArrowRight already at the end of the scale - produces no input event to clear the flag on, and the drag
    // that came after it would then be snapped to the marks as if it were a keystroke.
    private EventCallback<PointerEventArgs> _onPointerDown;

    // The marks themselves, and the values they sit at - sorted and deduplicated. Both are looked up several
    // times a render and again on every step of a drag, and building a series of up to two hundred of them
    // that many times a second is work nothing asked for. The marks are drawn from the parameters alone, so
    // the caches live exactly as long as a parameter set does. It also means an IEnumerable handed to Marks
    // is enumerated once per parameter set rather than once per reader.
    private List<BitSliderMark>? _marks;
    private List<double>? _markValues;

    private ElementReference _lowerInputRef;
    private ElementReference _upperInputRef;



    /// <summary>
    ///  A text description of the Slider number value for the benefit of screen readers.
    ///  This should be used when the Slider number value is not accurately represented by a number.
    /// </summary>
    /// <remarks>
    /// The returned text becomes the <c>aria-valuetext</c> of every thumb, which is what a screen reader
    /// announces in place of the raw number - "Medium" instead of "2", or "12:30" instead of "750".
    /// </remarks>
    [Parameter] public Func<double, string>? AriaValueText { get; set; }

    /// <summary>
    /// If true, the slider automatically receives focus when the page renders
    /// (rendered as the <c>autofocus</c> attribute of the first input).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitSlider.
    /// </summary>
    [Parameter] public BitSliderClassStyles? Classes { get; set; }

    /// <summary>
    /// Lets the filled band between the two thumbs of a ranged Slider be dragged bodily, so the whole range
    /// travels at once and keeps the width it had.
    /// </summary>
    /// <remarks>
    /// It is what a window over a scale wants - a price bracket nudged up as a whole, a span of hours moved
    /// to later in the day - where dragging one end and then the other is two moves for what the user thinks
    /// of as one. The band takes the presses that land strictly between the two thumbs, so the thumbs
    /// themselves stay grabbable and a band narrower than a thumb simply has nothing to take hold of; that
    /// also means a press on the band drags the range instead of sending the nearer thumb there. The two ends
    /// keep their distance while they travel, so every range constraint is satisfied throughout, and the band
    /// stops when the leading end reaches the end of the scale. A slider drawing no fill still has the same
    /// stretch of rail between its thumbs to be dragged by, where the grab cursor is what says so.
    /// </remarks>
    [Parameter] public bool DraggableTrack { get; set; }

    /// <summary>
    /// Builds the text of every label the Slider draws - the ones beside the track, the ones under the marks
    /// and the floating one on the thumb - from the value they stand for.
    /// </summary>
    /// <remarks>
    /// This is what a scale whose numbers are not what they mean needs: minutes shown as "12:30", bytes as
    /// "1.5 MB", a step index as the word it stands for. It takes precedence over <see cref="ValueFormat"/>,
    /// which covers the far more common case of a plain numeric format, and it is also what
    /// <see cref="AriaValueText"/> falls back to when no separate spoken form is given.
    /// </remarks>
    [Parameter] public Func<double, string>? GetValueText { get; set; }

    /// <summary>
    /// The general color of the slider, applied to the filled part of the track and to the thumbs.
    /// The unfilled part of the track stays neutral whichever color is picked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The default lower value of the ranged Slider, used when <see cref="LowerValue"/> is not bound.
    /// </summary>
    [Parameter] public double? DefaultLowerValue { get; set; }

    /// <summary>
    /// The default upper value of the ranged Slider, used when <see cref="UpperValue"/> is not bound.
    /// </summary>
    [Parameter] public double? DefaultUpperValue { get; set; }

    /// <summary>
    /// Fills the track from the far end instead of from the near one, so the highlighted part is what lies
    /// beyond the value rather than what lies before it. A ranged slider inverts into the two outer
    /// segments, leaving the span between the thumbs unfilled.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Inverted { get; set; }

    /// <summary>
    /// Attaches the origin of the filled part of the track to zero, so that the fill grows out of zero in
    /// whichever direction the value lies. It is shorthand for setting <see cref="Origin"/> to 0, and like
    /// it, the origin is held inside the Min..Max range.
    /// </summary>
    [Parameter] public bool IsOriginFromZero { get; set; }

    /// <summary>
    /// If ranged is true, display two thumbs that allow the lower and upper bounds of a range to be selected.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsRanged { get; set; }

    /// <summary>
    /// Whether to render the slider vertically.
    /// </summary>
    /// <remarks>
    /// A vertical slider has no length of its own to inherit, so it is given one through the
    /// <c>--bit-sld-length</c> CSS variable, which defaults to 12rem and can be changed per instance
    /// (<c>Style="--bit-sld-length: 20rem"</c>) or globally.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool IsVertical { get; set; }

    /// <summary>
    /// Description label of the Slider.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Replaces the plain text <see cref="Label"/> of the Slider with custom content.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The distance the larger keyboard jumps cover: <c>Page Up</c> and <c>Page Down</c>, and an arrow key
    /// held with <c>Shift</c>. It defaults to whatever the browser does on its own.
    /// </summary>
    /// <remarks>
    /// The slider pattern asks for a jump longer than a single <see cref="Step"/>, and a native range input
    /// does provide one on the Page keys - but each browser measures it differently and none of them makes
    /// anything of Shift, so a scale that wants a jump of a stated size has to name it. Setting this both
    /// pins the Page keys to that distance and gives Shift with an arrow key the same reach, without taking
    /// anything else off the keyboard: the arrows still move by a Step and Home and End still go to the ends
    /// of the scale. A jump is held inside the same bounds every other move is, and lands on a mark like
    /// every other move while <see cref="RestrictToMarks"/> is on.
    /// </remarks>
    [Parameter] public double? LargeStep { get; set; }

    /// <summary>
    /// The accessible name of the lower thumb of a ranged slider. Without it both thumbs are named after
    /// the slider as a whole, which leaves a screen reader user unable to tell them apart.
    /// </summary>
    [Parameter] public string? LowerAriaLabel { get; set; }

    /// <summary>
    /// The lower value of the ranged Slider.
    /// </summary>
    [Parameter, TwoWayBound]
    public double LowerValue { get; set; }

    /// <summary>
    /// The marks drawn along the track of the Slider, each optionally carrying a label under it.
    /// Marks outside the Min..Max range are skipped. Setting this takes precedence over <see cref="ShowMarks"/>.
    /// </summary>
    [Parameter] public IEnumerable<BitSliderMark>? Marks { get; set; }

    /// <summary>
    /// The interval between the marks generated by <see cref="ShowMarks"/>. It defaults to the
    /// <see cref="Step"/>, which is the right choice for a coarse slider but far too dense for a fine one:
    /// a 0..1000 slider stepping by 1 wants its marks every 100, not every 1.
    /// </summary>
    [Parameter] public double? MarkStep { get; set; }

    /// <summary>
    /// The largest distance the two thumbs of a ranged slider are allowed to be apart. A thumb dragged far
    /// enough to stretch the range past it stops there instead, and a pair that starts wider than this is
    /// narrowed to fit. Setting it also stops the thumbs from crossing, since a largest distance between them
    /// is only meaningful for an ordered pair. It is held at or above <see cref="MinRange"/>, so a pair of
    /// limits that contradict each other leaves the smallest distance in charge rather than locking the
    /// slider up.
    /// </summary>
    [Parameter] public double? MaxRange { get; set; }

    /// <summary>
    /// The smallest distance the two thumbs of a ranged slider are allowed to be apart, which is what keeps
    /// a range from collapsing onto a single value. Any value above zero also stops the thumbs from crossing,
    /// and a pair that starts closer together than this is widened to fit. A negative value is no distance
    /// at all rather than a licence to overlap.
    /// </summary>
    [Parameter] public double MinRange { get; set; }

    /// <summary>
    /// The min value of the Slider.
    /// </summary>
    [Parameter] public double Min { get; set; }

    /// <summary>
    /// The max value of the Slider.
    /// </summary>
    [Parameter] public double Max { get; set; } = 10;

    /// <summary>
    /// Leaves the track unfilled, so the slider is a bare rail with a thumb on it rather than a bar that
    /// grows. It is what a value with no sense of "how much" wants - a hue, a position, a seek point - and
    /// it also turns off the highlighting of the marks the fill would otherwise have reached.
    /// </summary>
    [Parameter] public bool NoFill { get; set; }

    /// <summary>
    /// Stops the two thumbs of a ranged slider from crossing over each other, so the lower one can never be
    /// dragged past the upper one and back.
    /// </summary>
    [Parameter] public bool NoSwap { get; set; }

    /// <summary>
    /// Callback for when the interaction that changes the value of the Slider ends - the pointer is released
    /// or the focus leaves after a keyboard change - rather than on every step along the way.
    /// This is where an expensive reaction to the value belongs.
    /// </summary>
    [Parameter] public EventCallback<double> OnChangeEnd { get; set; }

    /// <summary>
    /// Callback for when a thumb of the Slider receives the focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when a thumb of the Slider loses the focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when the range of a ranged Slider changes. This is called on every individual step.
    /// </summary>
    [Parameter] public EventCallback<BitSliderRangeValue> OnRangeChange { get; set; }

    /// <summary>
    /// Callback for when the interaction that changes the range of a ranged Slider ends,
    /// rather than on every step along the way.
    /// </summary>
    [Parameter] public EventCallback<BitSliderRangeValue> OnRangeChangeEnd { get; set; }

    /// <summary>
    /// The value the filled part of the track grows out of, instead of the near end of the track. A value
    /// outside the Min..Max range is held to it, and <see cref="IsOriginFromZero"/> is the shorthand for
    /// putting it at zero. It has no effect on a ranged slider, whose fill already has two ends of its own.
    /// </summary>
    [Parameter] public double? Origin { get; set; }

    /// <summary>
    /// Lets a thumb of a ranged slider push the other one along instead of stopping against it, so a range
    /// held apart by <see cref="MinRange"/> (or merely ordered by <see cref="NoSwap"/>) can be slid across
    /// the whole scale from either of its ends rather than having to be dragged one thumb at a time.
    /// </summary>
    /// <remarks>
    /// The pushed end stops at the end of the scale, and the pushing one comes to rest beside it. Pushing and
    /// crossing are opposites, so a pushable slider keeps its thumbs on their own sides whatever
    /// <see cref="NoSwap"/> says, and it has nothing to do until there is a distance to keep between them.
    /// </remarks>
    [Parameter] public bool Pushable { get; set; }

    /// <summary>
    /// Draws a mark at every <see cref="MarkStep"/> (or every <see cref="Step"/> when that is not set) along
    /// the track. Setting <see cref="Marks"/> explicitly takes precedence over this.
    /// </summary>
    [Parameter] public bool ShowMarks { get; set; }

    /// <summary>
    /// Labels the generated marks of <see cref="ShowMarks"/> with their own values, formatted the same way
    /// the value of the slider is. Marks provided through <see cref="Marks"/> carry their own labels instead.
    /// </summary>
    [Parameter] public bool ShowMarkLabels { get; set; }

    /// <summary>
    /// Restricts the values the user can pick to the marks alone, so the thumb jumps from one mark to the
    /// next instead of moving by the <see cref="Step"/> in between them.
    /// </summary>
    /// <remarks>
    /// This is what turns an irregular scale - a set of named tiers, or a series that grows by tens and then
    /// by hundreds - into something the pointer and the keyboard can both land on exactly. It constrains only
    /// what the user can pick: a value bound from elsewhere is always drawn where it actually is.
    /// It has no effect while there are no marks to snap to.
    /// </remarks>
    [Parameter] public bool RestrictToMarks { get; set; }

    /// <summary>
    /// Whether to show the value beside the Slider.
    /// </summary>
    [Parameter] public bool ShowValue { get; set; } = true;

    /// <summary>
    /// Size of the Slider, which scales its track, thumbs and labels together.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Additional html attributes for the Slider box, the element the track and the inputs are laid inside of.
    /// </summary>
    [Parameter] public Dictionary<string, object>? SliderBoxHtmlAttributes { get; set; }

    /// <summary>
    /// The difference between the two adjacent values of the Slider. Values of zero or below fall back to 1.
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>
    /// Custom CSS styles for different parts of the BitSlider.
    /// </summary>
    [Parameter] public BitSliderClassStyles? Styles { get; set; }

    /// <summary>
    /// Decides when the floating label that rides along with the thumb is shown: never (the default),
    /// while the slider is hovered, dragged or focused, or always.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSliderThumbLabel? ThumbLabel { get; set; }

    /// <summary>
    /// Replaces the text of the floating label that rides along with the thumb with custom content, built
    /// from the value that thumb stands for. It is only rendered while <see cref="ThumbLabel"/> asks for the
    /// label at all, and a ranged slider renders it once per thumb.
    /// </summary>
    [Parameter] public RenderFragment<double>? ThumbLabelTemplate { get; set; }

    /// <summary>
    /// The accessible name of the upper thumb of a ranged slider. Without it both thumbs are named after
    /// the slider as a whole, which leaves a screen reader user unable to tell them apart.
    /// </summary>
    [Parameter] public string? UpperAriaLabel { get; set; }

    /// <summary>
    /// The upper value of the ranged Slider.
    /// </summary>
    [Parameter, TwoWayBound]
    public double UpperValue { get; set; }

    /// <summary>
    /// The range value of the Slider. Use this parameter to get or set both the LowerValue and the UpperValue at once.
    /// </summary>
    [Parameter, TwoWayBound]
    public BitSliderRangeValue? RangeValue
    {
        get => new(LowerValue, UpperValue);
        set
        {
            if (value is null) return;

            LowerValue = value.Lower;
            UpperValue = value.Upper;
        }
    }

    /// <summary>
    /// Gets or sets an expression that identifies the bound <see cref="RangeValue"/>, which is what lets a
    /// ranged Slider take part in the validation of an EditForm the way a single-value one already does.
    /// </summary>
    /// <remarks>
    /// It is filled in by <c>@bind-RangeValue</c> itself, so a ranged slider bound that way is validated
    /// against the annotations of the property it is bound to, reports every move of a thumb to the
    /// EditContext, and repaints itself while the pair is invalid. The two ends bound separately through
    /// LowerValue and UpperValue are two properties rather than one field, and are validated on submit
    /// through the model rather than as a field of their own.
    /// </remarks>
    [Parameter] public Expression<Func<BitSliderRangeValue?>>? RangeValueExpression { get; set; }

    /// <summary>
    /// Custom format for the displayed value of the Slider, applied to the value labels, the mark labels
    /// and the floating thumb labels alike. It is a standard or custom .NET numeric format string.
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



    /// <summary>
    /// The reference to the lower input of a ranged slider, which is also the input a single-value slider renders.
    /// </summary>
    public ElementReference SliderInputElement => _lowerInputRef;

    /// <summary>
    /// The reference to the upper input of a ranged slider. It is the reference to the single input of a
    /// slider that is not ranged, since that slider has no upper end of its own.
    /// </summary>
    public ElementReference UpperSliderInputElement => IsRanged ? _upperInputRef : _lowerInputRef;

    /// <summary>
    /// Gives focus to the upper thumb of a ranged slider, which <see cref="BitInputBase{TValue}.FocusAsync()"/>
    /// cannot reach: it is pointed at the single tab stop of a plain slider, and so at the lower thumb of a
    /// ranged one.
    /// </summary>
    /// <param name="preventScroll">Whether to leave the page where it is instead of scrolling the thumb into view.</param>
    public ValueTask FocusUpperAsync(bool preventScroll = false) => UpperSliderInputElement.FocusAsync(preventScroll);



    protected override string RootElementClass => "bit-sld";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ReadOnly ? "bit-sld-rdl" : string.Empty);

        ClassBuilder.Register(() => Inverted ? "bit-sld-ivt" : string.Empty);

        ClassBuilder.Register(() => IsRanged ? "bit-sld-rgd" : string.Empty);

        // The composite class is kept beside the two simple ones so stylesheets written against the
        // previous markup keep matching the same elements.
        ClassBuilder.Register(() => $"bit-sld-{(IsRanged ? "rgd-" : null)}{(IsVertical ? "vrt" : "hrz")}");

        ClassBuilder.Register(() => IsRanged ? $"bit-sld-{(IsVertical ? "vrt" : "hrz")}" : string.Empty);

        ClassBuilder.Register(() => ThumbLabel switch
        {
            BitSliderThumbLabel.Auto => "bit-sld-tla",
            BitSliderThumbLabel.On => "bit-sld-tlo",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-sld-pri",
            BitColor.Secondary => "bit-sld-sec",
            BitColor.Tertiary => "bit-sld-ter",
            BitColor.Info => "bit-sld-inf",
            BitColor.Success => "bit-sld-suc",
            BitColor.Warning => "bit-sld-wrn",
            BitColor.SevereWarning => "bit-sld-swr",
            BitColor.Error => "bit-sld-err",
            BitColor.PrimaryBackground => "bit-sld-pbg",
            BitColor.SecondaryBackground => "bit-sld-sbg",
            BitColor.TertiaryBackground => "bit-sld-tbg",
            BitColor.PrimaryForeground => "bit-sld-pfg",
            BitColor.SecondaryForeground => "bit-sld-sfg",
            BitColor.TertiaryForeground => "bit-sld-tfg",
            BitColor.PrimaryBorder => "bit-sld-pbr",
            BitColor.SecondaryBorder => "bit-sld-sbr",
            BitColor.TertiaryBorder => "bit-sld-tbr",
            _ => "bit-sld-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-sld-sm",
            BitSize.Medium => "bit-sld-md",
            BitSize.Large => "bit-sld-lg",
            _ => "bit-sld-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _onKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(new object(), HandleOnKeyDown);
        _onPointerDown = EventCallback.Factory.Create<PointerEventArgs>(new object(), HandleOnPointerDown);

        // The default of an unbound ranged slider is applied here rather than in OnParametersSet, so that a
        // later re-render with the same parameters cannot undo what the user has done since.
        if (IsRanged)
        {
            // RangeValue carries both ends at once, so a slider given one is not an unbound slider looking
            // for a default - the defaults would silently overwrite half of what it was handed.
            if (RangeValueHasBeenSet is false)
            {
                if (LowerValueHasBeenSet is false && DefaultLowerValue.HasValue)
                {
                    LowerValue = DefaultLowerValue.Value;
                }

                if (UpperValueHasBeenSet is false && DefaultUpperValue.HasValue)
                {
                    UpperValue = DefaultUpperValue.Value;
                }
            }
        }
        else
        {
            SetDefaultValue();
        }

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        _marks = null;
        _markValues = null;

        if (IsRanged)
        {
            // Held inside the range the parameters describe, and ordered, before anything is drawn from them.
            var lower = ClampValue(LowerValue);
            var upper = ClampValue(UpperValue);

            if (lower > upper)
            {
                (lower, upper) = (upper, lower);
            }

            (lower, upper) = NormalizeRange(lower, upper);

            // A pair that had to be moved is a pair the parent is now holding a different value for, so the
            // correction is reported back to it below rather than kept to ourselves - otherwise a bound range
            // would silently disagree with the one the slider is drawing. Each incoming pair is reported on
            // only once, so a parent that keeps handing the same rejected pair back is answered once as well.
            var incoming = (LowerValue, UpperValue);

            if (lower != LowerValue || upper != UpperValue)
            {
                _rangeCorrected = _reportedRange != incoming;
                _reportedRange = incoming;
            }
            else
            {
                _rangeCorrected = false;
                _reportedRange = null;
            }

            LowerValue = lower;
            UpperValue = upper;

            // The two inputs are re-seeded from the values, keeping whichever of them currently holds the
            // upper end: a crossed pair stays crossed, so the thumb under the pointer is not sent flying to
            // the other side halfway through a drag.
            if (_thumbA > _thumbB)
            {
                _thumbA = upper;
                _thumbB = lower;
            }
            else
            {
                _thumbA = lower;
                _thumbB = upper;
            }
        }

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsRanged)
        {
            if (_rangeCorrected)
            {
                _rangeCorrected = false;

                await NotifyRangeAsync(LowerValue, UpperValue);
            }
        }
        else
        {
            var incoming = CurrentValue;
            var clamped = ClampValue(incoming);

            if (clamped != incoming)
            {
                // A one-way bound value has nobody to notify, so it is corrected locally instead. Assigning
                // CurrentValue is avoided here as well, since its setter drops the write while IsEnabled is false.
                if (InvalidValueBinding())
                {
                    Value = clamped;
                }
                else if (_reportedValue != incoming)
                {
                    // Reported once per rejected value, so a parent that hands the same one back on the render
                    // this report causes does not start an endless exchange of corrections.
                    _reportedValue = incoming;

                    await SetCurrentValueAsync(clamped);
                }
                else
                {
                    Value = clamped;
                }
            }
            else
            {
                _reportedValue = null;
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // FocusAsync is pointed at the input that carries the value of a plain slider, and at the lower one
        // of a ranged slider, which is the first of its two tab stops.
        InputElement = _lowerInputRef;

        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// The field a ranged slider reports to is the pair it selects, not the single value it never moves.
    /// </summary>
    /// <remarks>
    /// The parameters of the component itself are already assigned by the time this runs - the generated
    /// SetParametersAsync fills them in before handing over to the base one - so both IsRanged and the
    /// expression are available to decide with.
    /// </remarks>
    protected override void CreateFieldIdentifier()
    {
        if (IsRanged && RangeValueExpression is not null)
        {
            CreateFieldIdentifier(RangeValueExpression, typeof(BitSliderRangeValue));
            return;
        }

        base.CreateFieldIdentifier();
    }

    // The value travels through a native range input, whose value attribute is only ever read and written in
    // the invariant format - a "0,5" produced by a comma-decimal culture would be rejected outright by the
    // browser - so both directions of the conversion are pinned to it.
    protected override string? FormatValueAsString(double value) => value.ToString(CultureInfo.InvariantCulture);

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out double result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
        {
            result = parsedValue;
            parsingErrorMessage = null;
            return true;
        }

        result = default;
        parsingErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }



    /// <summary>
    /// The lower end of the scale. A Min above the Max would make the whole geometry run backwards, so the
    /// pair is read in order rather than taken literally.
    /// </summary>
    private double _Min => Math.Min(Min, Max);

    /// <summary>
    /// The upper end of the scale.
    /// </summary>
    private double _Max => Math.Max(Min, Max);

    /// <summary>
    /// The distance the whole scale covers, which is what every position along the track is measured against.
    /// </summary>
    private double _Range => _Max - _Min;

    /// <summary>
    /// The size of a single step. A step of zero or below would let the browser fall back to its own default
    /// silently, so it is brought back to the documented 1 here instead.
    /// </summary>
    private double _Step => Step > 0 ? Step : 1;

    /// <summary>
    /// The value the fill of a single-value slider grows out of: the explicit Origin, then zero when the
    /// origin is attached to it, and otherwise the end of the track the fill normally starts from - which
    /// an inverted slider moves to the other end.
    /// </summary>
    private double _Origin
    {
        get
        {
            if (Origin.HasValue) return ClampValue(Origin.Value);

            if (IsOriginFromZero) return ClampValue(0);

            return Inverted ? _Max : _Min;
        }
    }

    /// <summary>
    /// The smallest distance to hold between the two thumbs. A negative one is no distance at all rather
    /// than a licence to overlap, which is what taking it literally in the bounds of the inputs would mean.
    /// </summary>
    private double _MinRange => MinRange > 0 ? MinRange : 0;

    /// <summary>
    /// The largest distance to allow between the two thumbs, held above the smallest one: a pair of limits
    /// that contradict each other would otherwise hand an input a min above its own max.
    /// </summary>
    private double? _MaxRange => MaxRange.HasValue ? Math.Max(_MinRange, Math.Max(0, MaxRange.Value)) : null;

    /// <summary>
    /// The distance one of the larger keyboard jumps covers, which falls back to a single step so that a
    /// jump of nothing at all can never leave a key doing nothing.
    /// </summary>
    private double _LargeStep => LargeStep is > 0 ? LargeStep.Value : _Step;

    /// <summary>
    /// Whether the two thumbs of a ranged slider are kept apart, which is what turns off their ability to
    /// cross: any distance to hold between them makes crossing meaningless.
    /// </summary>
    private bool _IsConstrained => NoSwap || _MinRange > 0 || _MaxRange.HasValue;

    /// <summary>
    /// Whether the filled band between the two thumbs is draggable as a whole. A band of no width has no
    /// room to be pressed on, and one that already covers the whole scale has nowhere to travel.
    /// </summary>
    private bool _CanDragTrack => DraggableTrack && IsRanged && _IsInteractive && UpperValue > LowerValue;

    /// <summary>
    /// Whether a thumb carries the other one along when it runs into it. Pushing needs something to push
    /// against, so it is only ever on where the two thumbs are kept apart or merely kept in order.
    /// </summary>
    private bool _CanPush => Pushable && (NoSwap || _MinRange > 0);

    /// <summary>
    /// The lowest value the lower input is allowed to reach. The constraints are pushed into the native
    /// min/max of the inputs rather than corrected afterwards, so the browser itself never produces a value
    /// the slider would have to reject - and the thumb can never drift away from the value it draws.
    /// </summary>
    private double _LowerInputMin => _MaxRange.HasValue ? Denoise(Math.Max(_Min, UpperValue - _MaxRange.Value)) : _Min;

    /// <summary>
    /// The highest value the lower input is allowed to reach: the upper thumb, less whatever distance has to
    /// be kept from it - or, when that thumb is pushed along instead of stood against, the far end of the
    /// scale less the room the pushed thumb needs there.
    /// </summary>
    private double _LowerInputMax => _CanPush
        ? Denoise(Math.Max(_Min, _Max - _MinRange))
        : (_IsConstrained ? Denoise(Math.Max(_Min, UpperValue - _MinRange)) : _Max);

    private double _UpperInputMin => _CanPush
        ? Denoise(Math.Min(_Max, _Min + _MinRange))
        : (_IsConstrained ? Denoise(Math.Min(_Max, LowerValue + _MinRange)) : _Min);

    private double _UpperInputMax => _MaxRange.HasValue ? Denoise(Math.Min(_Max, LowerValue + _MaxRange.Value)) : _Max;

    /// <summary>
    /// The distance the two ends of the range currently keep between them, which is the width the draggable
    /// band travels with.
    /// </summary>
    private double _RangeWidth => Denoise(UpperValue - LowerValue);

    /// <summary>
    /// The highest value the draggable band can carry: its value is the lower end of the range, and the
    /// highest lower end there is room for is the one that puts the upper end at the end of the scale.
    /// </summary>
    private double _TrackInputMax => Denoise(_Max - _RangeWidth);

    /// <summary>
    /// The lower end of the filled part of the track, as a percentage of the whole scale.
    /// </summary>
    private double _FillStart => IsRanged ? GetPercent(LowerValue) : GetPercent(Math.Min(_Origin, CurrentValue));

    /// <summary>
    /// The upper end of the filled part of the track, as a percentage of the whole scale.
    /// </summary>
    private double _FillEnd => IsRanged ? GetPercent(UpperValue) : GetPercent(Math.Max(_Origin, CurrentValue));

    /// <summary>
    /// The marks actually drawn: the explicit ones when they are given, and otherwise the ones generated at
    /// a fixed interval. The generated series is capped, since a mark per step of a fine slider is thousands
    /// of elements nobody can tell apart anyway.
    /// </summary>
    private List<BitSliderMark> _Marks => _marks ??= BuildMarks();

    private List<BitSliderMark> BuildMarks()
    {
        if (Marks is not null) return Marks.Where(m => m is not null && m.Value >= _Min && m.Value <= _Max).ToList();

        if (ShowMarks is false) return [];

        return GenerateMarks();
    }

    private List<double> _MarkValues => _markValues ??= _Marks.Select(m => m.Value).Distinct().OrderBy(v => v).ToList();

    private const int MaxGeneratedMarks = 200;

    private List<BitSliderMark> GenerateMarks()
    {
        var step = MarkStep is > 0 ? MarkStep.Value : _Step;
        var range = _Range;

        if (range <= 0) return [new BitSliderMark(_Min, ShowMarkLabels ? GetDisplayValue(_Min) : null)];

        // Counted in double first: a step small enough against the range would overflow an int on the way in,
        // and an overflowed count silently produces a single mark rather than the capped series below.
        var wanted = Math.Floor(range / step);

        // A step that would draw more marks than anyone can see is widened until the series fits, which
        // keeps a "show the marks" on a thousand-step slider from putting a thousand elements in the DOM.
        var count = MaxGeneratedMarks;

        if (wanted < MaxGeneratedMarks)
        {
            count = (int)wanted;
        }
        else
        {
            step = range / MaxGeneratedMarks;
        }

        var marks = new List<BitSliderMark>(count + 2);

        for (var i = 0; i <= count; i++)
        {
            // Rounded because repeated addition of a step is where binary floating point starts showing: a
            // series of tenths would otherwise reach 0.30000000000000004 and label itself with all of it.
            var value = Denoise(_Min + i * step);

            if (value > _Max) break;

            marks.Add(new BitSliderMark(value, ShowMarkLabels ? GetDisplayValue(value) : null));
        }

        // The far end is worth a mark of its own even when the step does not land on it exactly, since it is
        // the one position of the track a reader is most likely to look for.
        if (marks.Count == 0 || marks[^1].Value < _Max)
        {
            marks.Add(new BitSliderMark(_Max, ShowMarkLabels ? GetDisplayValue(_Max) : null));
        }

        return marks;
    }

    /// <summary>
    /// Rounds off the noise binary floating point leaves behind whenever a step or a distance is added to a
    /// value: 0.1 + 0.2 reaches 0.30000000000000004, and a value carrying all of that is what would then be
    /// drawn, announced, posted and handed to the browser as a min or a max. Ten decimals is far beyond any
    /// scale a slider is asked to carry and well short of where a double runs out of precision of its own.
    /// </summary>
    private static double Denoise(double value) => double.IsFinite(value) ? Math.Round(value, 10) : value;

    /// <summary>
    /// Where a value sits along the track, from 0 to 100. An empty scale collapses everything onto its start
    /// rather than dividing by zero, and so does one that is not a scale at all.
    /// </summary>
    private double GetPercent(double value)
    {
        var range = _Range;

        if (double.IsFinite(range) is false || range <= 0) return 0;

        var fraction = (value - _Min) / range;

        if (double.IsNaN(fraction)) return 0;

        return Math.Round(Math.Clamp(fraction, 0, 1) * 100, 4);
    }

    private double ClampValue(double value)
    {
        if (double.IsNaN(value)) return _Min;

        return Math.Clamp(value, _Min, _Max);
    }

    /// <summary>
    /// Brings a pair of range ends inside the distance constraints, widening a pair that sits closer
    /// together than <see cref="MinRange"/> and narrowing one that sits further apart than
    /// <see cref="MaxRange"/>.
    /// </summary>
    /// <remarks>
    /// The constraints are otherwise handed to the inputs as their own min and max, which only works while
    /// the current values already satisfy them: a pair closer together than MinRange would hand an input a
    /// max below its own value, and the browser would quietly clamp it somewhere the component is not
    /// drawing. So every pair passes through here before anything is rendered from it.
    /// </remarks>
    private (double Lower, double Upper) NormalizeRange(double lower, double upper)
    {
        var minRange = _MinRange;

        if (minRange > 0 && upper - lower < minRange)
        {
            // The far end is preferred, and the near one moved only when there is no room left above.
            upper = Denoise(Math.Min(_Max, lower + minRange));
            lower = Denoise(Math.Max(_Min, upper - minRange));
        }

        var maxRange = _MaxRange;

        if (maxRange.HasValue && upper - lower > maxRange.Value)
        {
            upper = Denoise(lower + maxRange.Value);
        }

        return (lower, upper);
    }

    /// <summary>
    /// The text of every label the slider draws. It is the display side of the value, so it follows the
    /// culture of the user rather than the invariant one the inputs are written in.
    /// </summary>
    private string GetDisplayValue(double value)
    {
        if (GetValueText is not null) return GetValueText(value);

        return ValueFormat.HasValue()
            ? value.ToString(ValueFormat, CultureInfo.CurrentCulture)
            : value.ToString(CultureInfo.CurrentCulture);
    }

    private string GetAriaValueText(double value) => AriaValueText is not null ? AriaValueText(value) : GetDisplayValue(value);

    private static string ToInvariant(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The inline position of something that sits at a point along the track. A vertical slider grows from
    /// its bottom edge upwards, so "more" is up the same way the ArrowUp key already means more.
    /// </summary>
    private string GetPointStyle(double percent)
    {
        return IsVertical
            ? FormattableString.Invariant($"bottom:{percent}%")
            : FormattableString.Invariant($"inset-inline-start:{percent}%");
    }

    /// <summary>
    /// The piece of the track a native input is laid over, as the pair of fractions its own min and max sit
    /// at along the scale.
    /// </summary>
    /// <remarks>
    /// A constrained input no longer runs across the whole scale - its thumb travels between the bounds the
    /// constraints gave it - so covering the whole track would map its value onto the wrong span and leave
    /// the invisible thumb well away from the drawn one. Since only the native thumb takes the pointer, that
    /// is a slider that cannot be grabbed where it is seen. Each input is therefore given exactly the span
    /// its bounds describe, which brings the two thumbs back together whatever the constraints are.
    /// <para>
    /// The two inputs of a ranged slider also carry the point their hit areas are cut at - the midpoint
    /// between the thumbs - so that every point of the rail belongs to the input whose thumb is nearest to
    /// it. It is given in the coordinates of the input's own span rather than of the whole scale, since that
    /// span is the box the cut is measured in.
    /// </para>
    /// </remarks>
    private string GetInputStyle(double inputMin, double inputMax, double? midPercent = null)
    {
        var start = GetPercent(inputMin) / 100;
        var end = Math.Max(start, GetPercent(inputMax) / 100);

        if (midPercent.HasValue is false)
        {
            return FormattableString.Invariant($"--bit-sld-inp-start:{start};--bit-sld-inp-end:{end}");
        }

        // A pinned input - a read-only one, or one the constraints have left no room in - has no span to
        // measure a cut against, and nothing to be grabbed for either way.
        var cut = end > start
            ? Math.Round(Math.Clamp((midPercent.Value / 100 - start) / (end - start), 0, 1), 4)
            : 0.5;

        return FormattableString.Invariant($"--bit-sld-inp-start:{start};--bit-sld-inp-end:{end};--bit-sld-inp-mid:{cut}");
    }

    /// <summary>
    /// The two ends of the draggable band, as fractions of the whole scale.
    /// </summary>
    /// <remarks>
    /// The band is a third range input laid over the whole box, whose own thumb is made as wide as the band
    /// itself. That is what makes it drag the way a band should: a press lands on the thumb rather than
    /// beside it, so the browser carries it along relative to the pointer instead of jumping it to where the
    /// press was - and the distance such a thumb has left to travel is, by the very arithmetic a range input
    /// already uses, exactly the room the band has left on the scale. Both come out of these two fractions,
    /// which is why they are handed to the stylesheet rather than a finished length.
    /// </remarks>
    private static string GetTrackInputStyle(double startPercent, double endPercent)
    {
        return FormattableString.Invariant($"--bit-sld-bar-start:{startPercent / 100};--bit-sld-bar-end:{endPercent / 100}");
    }

    /// <summary>
    /// The inline geometry of a filled segment of the track, laid along whichever axis the slider runs on.
    /// </summary>
    private string GetSegmentStyle(double start, double end)
    {
        var size = Math.Max(0, end - start);

        return IsVertical
            ? FormattableString.Invariant($"bottom:{start}%;height:{size}%")
            : FormattableString.Invariant($"inset-inline-start:{start}%;width:{size}%");
    }

    /// <summary>
    /// A mark the fill has reached is painted in the contrasting color, so that it stays legible on top of
    /// the fill instead of disappearing into it. An inverted range fills the two outer segments rather than
    /// the span between the thumbs, so what counts as reached is inverted with it.
    /// </summary>
    private string GetMarkClass(BitSliderMark mark, double fillStart, double fillEnd)
    {
        // Nothing has been reached on a slider that draws no fill in the first place.
        if (NoFill) return "bit-sld-mrk";

        var percent = GetPercent(mark.Value);
        var isInside = percent >= fillStart && percent <= fillEnd;
        var isActive = (Inverted && IsRanged) ? isInside is false : isInside;

        return isActive ? "bit-sld-mrk bit-sld-mrk-act" : "bit-sld-mrk";
    }

    /// <summary>
    /// The accessible name of a thumb: the one written for that end of a range, then the name of the slider
    /// as a whole, and finally its visible label - so a thumb is never left without a name.
    /// </summary>
    private string? GetThumbAriaLabel(string? specific) => specific.HasValue() ? specific : (AriaLabel.HasValue() ? AriaLabel : Label);

    /// <summary>
    /// The element a thumb borrows its name from when there is no text to name it with directly.
    /// </summary>
    /// <remarks>
    /// A slider labelled only by a <see cref="LabelTemplate"/> has no string to put in an aria-label, and the
    /// for/id association of the label element only ever reaches one input - which leaves the upper thumb of
    /// a ranged slider nameless. Pointing both of them at the label element covers every case at once.
    /// </remarks>
    private string? GetThumbAriaLabelledBy(string? specific)
    {
        if (GetThumbAriaLabel(specific).HasValue()) return null;

        return (Label.HasValue() || LabelTemplate is not null) ? $"{_Id}-label" : null;
    }

    /// <summary>
    /// Whether the value can be changed at all.
    /// </summary>
    /// <remarks>
    /// A native range input ignores the readonly attribute entirely, so a read-only slider pins its inputs
    /// by handing each one a min and a max that are both its current value: the browser then refuses the
    /// change itself instead of the component having to undo it afterwards. The true bounds are still
    /// announced, through the explicit aria-valuemin and aria-valuemax the inputs always carry.
    /// </remarks>
    private bool _IsInteractive => IsEnabled && ReadOnly is false;



    /// <summary>
    /// Snaps a value the user has just produced onto the nearest mark.
    /// </summary>
    /// <remarks>
    /// The nearest mark alone is not enough for the keyboard: a single Step is usually shorter than the gap
    /// between two marks, so the value it lands on would snap straight back to the mark it started from and
    /// the arrow keys would do nothing at all. A key is therefore carried on to the next mark in the
    /// direction it was going whenever rounding would have undone it. A pointer is not: it lands where it is
    /// pointing, and carrying it on would send the thumb a whole mark further than the pointer ever went -
    /// and then back again on the next move, which is a drag that jitters rather than one that snaps.
    /// </remarks>
    private double SnapToMark(double value, double from, bool fromKeyboard)
    {
        if (RestrictToMarks is false) return value;

        var marks = _MarkValues;

        if (marks.Count == 0) return value;

        var nearest = marks[0];

        foreach (var mark in marks)
        {
            if (Math.Abs(mark - value) < Math.Abs(nearest - value))
            {
                nearest = mark;
            }
        }

        if (fromKeyboard is false) return nearest;

        if (nearest != from || value == from) return nearest;

        // Rounding would have cancelled the move, so it is carried on to the next mark that way instead.
        return value > from
            ? marks.FirstOrDefault(m => m > from, nearest)
            : marks.LastOrDefault(m => m < from, nearest);
    }

    /// <summary>
    /// Holds a value that snapping has just moved inside the bounds the input it belongs to actually carries.
    /// </summary>
    /// <remarks>
    /// Every other move is already held there by the browser, since the range constraints are pushed into the
    /// min and the max of the inputs - but snapping happens after the browser is done with the move, and
    /// carrying a key on to the next mark is a distance those bounds never saw. Left alone it would take a
    /// thumb straight through the one beside it, and the normalization that follows would then drag that other
    /// thumb along - a push on a slider nobody asked to be pushable. The mark nearest to it that does fit is
    /// where such a move belongs instead; a scale with no mark inside the bounds at all leaves the thumb
    /// against the bound itself, which is still the furthest it was allowed to go.
    /// </remarks>
    private double ConstrainSnapped(double value, double min, double max)
    {
        if (RestrictToMarks is false) return value;

        if (value >= min && value <= max) return value;

        double? nearest = null;

        foreach (var mark in _MarkValues)
        {
            if (mark < min || mark > max) continue;

            if (nearest is null || Math.Abs(mark - value) < Math.Abs(nearest.Value - value))
            {
                nearest = mark;
            }
        }

        return nearest ?? ClampInto(value, min, max);
    }

    /// <summary>
    /// Notes that the change about to arrive was produced by a key, and whether that key was one of the
    /// larger jumps. Only the keys that actually move a range input count, so that a key which changes
    /// nothing - a Tab out of the slider - does not leave the next drag looking like a keystroke.
    /// </summary>
    private void HandleOnKeyDown(KeyboardEventArgs e)
    {
        var isArrow = e.Key is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown";
        var isPage = e.Key is "PageUp" or "PageDown";

        _keyboardChange = isArrow || isPage || e.Key is "Home" or "End";

        // A browser measures a Page key its own way and makes nothing at all of a Shift, so both are
        // re-measured against LargeStep - but only once there is one to measure against, since taking the
        // Page keys over to hand them the very distance the browser already chose would be no change at all.
        _largeStepChange = LargeStep is > 0 && (isPage || (isArrow && e.ShiftKey));
    }

    /// <summary>
    /// Clears what a key left behind. A key that moves nothing - an ArrowRight already at the end of the
    /// scale, a Page key on a slider the constraints have pinned - produces no input event to clear the flag
    /// on, and the drag that came next would be carried on to the following mark as if it were a keystroke.
    /// </summary>
    private void HandleOnPointerDown(PointerEventArgs e)
    {
        _keyboardChange = false;
        _largeStepChange = false;
    }

    /// <summary>
    /// The value a change actually lands on: the one the browser produced, except for the larger keyboard
    /// jumps, which are taken the same way but <see cref="LargeStep"/> at a time.
    /// </summary>
    /// <remarks>
    /// The direction is read off the move the browser has already made rather than off the key that caused
    /// it, since a horizontal RTL slider maps the arrows onto the scale the other way round - and a key that
    /// moved nothing has no direction to be taken further in.
    /// </remarks>
    private double ResolveIncoming(double parsed, double from, bool largeStep)
    {
        if (largeStep is false || parsed == from) return parsed;

        return ClampValue(Denoise(parsed > from ? from + _LargeStep : from - _LargeStep));
    }

    /// <summary>
    /// Holds a value inside a pair of bounds that are not guaranteed to be in order, which is what a pair of
    /// range constraints that contradict each other can leave behind.
    /// </summary>
    private static double ClampInto(double value, double min, double max) => max < min ? min : Math.Clamp(value, min, max);

    private async Task HandleOnInput(ChangeEventArgs e)
    {
        // Read and cleared before the change is refused as well as before it is taken, so that a slider which
        // is turned interactive again does not begin with a keystroke somebody made while it was not.
        var fromKeyboard = _keyboardChange;
        var largeStep = _largeStepChange;
        _keyboardChange = false;
        _largeStepChange = false;

        if (_IsInteractive is false) return;

        if (RestrictToMarks is false && largeStep is false)
        {
            await SetCurrentValueAsStringAsync(e.Value?.ToString());
            return;
        }

        if (double.TryParse(e.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) is false) return;

        var incoming = ResolveIncoming(parsed, CurrentValue, largeStep);

        var snapped = ClampValue(SnapToMark(incoming, CurrentValue, fromKeyboard));

        if (snapped == CurrentValue) return;

        await SetCurrentValueAsync(snapped);
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (_IsInteractive is false) return;

        await OnChangeEnd.InvokeAsync(CurrentValue);

        await ResyncInputAsync(e.Value?.ToString(), CurrentValue, v => _rawValue = v);
    }

    private async Task HandleOnRangeInput(ChangeEventArgs e, bool isLower)
    {
        var fromKeyboard = _keyboardChange;
        var largeStep = _largeStepChange;
        _keyboardChange = false;
        _largeStepChange = false;

        if (_IsInteractive is false) return;
        if (InvalidRangeBinding()) return;

        if (double.TryParse(e.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) is false) return;

        parsed = ResolveIncoming(parsed, isLower ? _thumbA : _thumbB, largeStep);

        if (largeStep)
        {
            // A jump measured here is not one the browser held inside the bounds of the input, so the
            // constraints that were pushed into those bounds are applied to it directly instead.
            parsed = isLower
                ? ClampInto(parsed, _LowerInputMin, _LowerInputMax)
                : ClampInto(parsed, _UpperInputMin, _UpperInputMax);
        }

        double lower;
        double upper;

        if (_CanPush)
        {
            // Pushing and crossing are opposites, so the two inputs keep their sides and the one that moved
            // simply carries the other along as soon as it runs into it. The far end of the scale is where
            // the pushed thumb stops, and the pushing one then comes to rest beside it rather than through it.
            if (isLower)
            {
                _thumbA = ClampValue(SnapToMark(parsed, _thumbA, fromKeyboard));

                if (_thumbB - _thumbA < _MinRange) _thumbB = Denoise(Math.Min(_Max, _thumbA + _MinRange));
                if (_thumbB - _thumbA < _MinRange) _thumbA = Denoise(Math.Max(_Min, _thumbB - _MinRange));
            }
            else
            {
                _thumbB = ClampValue(SnapToMark(parsed, _thumbB, fromKeyboard));

                if (_thumbB - _thumbA < _MinRange) _thumbA = Denoise(Math.Max(_Min, _thumbB - _MinRange));
                if (_thumbB - _thumbA < _MinRange) _thumbB = Denoise(Math.Min(_Max, _thumbA + _MinRange));
            }

            (lower, upper) = NormalizeRange(_thumbA, _thumbB);
            (_thumbA, _thumbB) = (lower, upper);

            await SetRangeAsync(lower, upper);
            return;
        }

        // Snapping is held against the bounds the constraints gave the moving input, which the browser has
        // already applied to the move itself but which the snap that follows it would otherwise step over.
        if (isLower)
        {
            _thumbA = ConstrainSnapped(SnapToMark(parsed, _thumbA, fromKeyboard), _LowerInputMin, _LowerInputMax);
        }
        else
        {
            _thumbB = ConstrainSnapped(SnapToMark(parsed, _thumbB, fromKeyboard), _UpperInputMin, _UpperInputMax);
        }

        // The inputs are never re-sorted, so the ends of the range are read off them instead: whichever one
        // the pointer has taken further is the upper end, which is what lets the thumbs cross. The browser
        // already holds each input inside the bounds the constraints give it, so the normalization here only
        // has anything left to do when snapping to the marks has moved a thumb past one of them.
        (lower, upper) = NormalizeRange(Math.Min(_thumbA, _thumbB), Math.Max(_thumbA, _thumbB));

        // What came out of the normalization goes back into the inputs, keeping whichever of them currently
        // holds the upper end so a crossed pair stays crossed. Without this the render that follows would put
        // the un-normalized value back on an input while the thumb beside it is drawn at the normalized one -
        // and a re-render of an unbound slider never passes through OnParametersSet to correct it.
        if (_thumbA > _thumbB)
        {
            (_thumbA, _thumbB) = (upper, lower);
        }
        else
        {
            (_thumbA, _thumbB) = (lower, upper);
        }

        await SetRangeAsync(lower, upper);
    }

    private async Task HandleOnRangeChange(ChangeEventArgs e, bool isLower)
    {
        if (_IsInteractive is false) return;

        await OnRangeChangeEnd.InvokeAsync(new BitSliderRangeValue(LowerValue, UpperValue));

        await (isLower
            ? ResyncInputAsync(e.Value?.ToString(), _thumbA, v => _rawA = v)
            : ResyncInputAsync(e.Value?.ToString(), _thumbB, v => _rawB = v));
    }

    /// <summary>
    /// Moves the whole range as the band between the thumbs is dragged. The band keeps the width it had, so
    /// the value the input carries - the lower end - is all there is to read, and every distance constraint
    /// is satisfied throughout by construction.
    /// </summary>
    private async Task HandleOnTrackInput(ChangeEventArgs e)
    {
        if (_IsInteractive is false) return;
        if (InvalidRangeBinding()) return;

        if (double.TryParse(e.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) is false) return;

        var width = _RangeWidth;

        // Snapping the leading end alone is what a band on a marked scale can promise: the trailing one is
        // held a fixed distance away, which an irregular scale has no reason to have a mark at. The band
        // stops when its far end reaches the end of the scale rather than being squashed against it, and a
        // snap that would take it past that lands on the outermost mark still leaving room for the width.
        var lower = ConstrainSnapped(SnapToMark(parsed, LowerValue, false), _Min, _TrackInputMax);

        lower = ClampInto(lower, _Min, _TrackInputMax);

        var upper = ClampValue(Denoise(lower + width));

        _thumbA = lower;
        _thumbB = upper;

        await SetRangeAsync(lower, upper);
    }

    private async Task HandleOnTrackChange(ChangeEventArgs e)
    {
        if (_IsInteractive is false) return;

        await OnRangeChangeEnd.InvokeAsync(new BitSliderRangeValue(LowerValue, UpperValue));

        await ResyncInputAsync(e.Value?.ToString(), LowerValue, v => _rawBar = v);
    }

    /// <summary>
    /// Puts the value the component holds back onto the native input whenever the browser has been left
    /// holding a different one - a change that was snapped to a mark, refused by a one-way binding, or
    /// rounded onto the step grid on the way in.
    /// </summary>
    /// <remarks>
    /// Blazor writes an attribute to the element only when it differs from the one the previous render put
    /// there, and the one the previous render put there is exactly the value the input has since drifted away
    /// from - so simply rendering again changes nothing, and the invisible thumb stays where the browser left
    /// it while the drawn one stands somewhere else. The input is therefore taken through the value the
    /// browser is holding first, which turns the render after it into a change the diff will carry all the
    /// way to the DOM. Nothing is drawn from the intermediate value: it lives for one pass and is gone.
    /// </remarks>
    private async Task ResyncInputAsync(string? raw, double held, Action<double?> hold)
    {
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) is false) return;

        if (parsed == held) return;

        hold(parsed);
        StateHasChanged();

        // Yielding is what separates the two passes: without it both assignments would land in the same
        // render and the element would never be written to.
        await Task.Yield();

        // A slider taken off the page between the two passes - the release of a pointer that closed the panel
        // it sits in - has no second render to give, and asking a disposed component for one throws.
        if (IsDisposed) return;

        hold(null);
        StateHasChanged();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocusOut.InvokeAsync(e);
    }

    /// <summary>
    /// Reports a pair of range ends to everything bound to them, without the OnRangeChange that stands for a
    /// change the user made: this is the slider correcting what it was handed, not the user moving a thumb.
    /// </summary>
    private async Task NotifyRangeAsync(double lower, double upper)
    {
        await LowerValueChanged.InvokeAsync(lower);
        await UpperValueChanged.InvokeAsync(upper);
        await RangeValueChanged.InvokeAsync(new BitSliderRangeValue(lower, upper));

        // A correction moves the field as surely as a drag does, so the EditContext is told about it here as
        // well - otherwise a pair narrowed to fit the constraints would keep the error of the pair it replaced.
        NotifyFieldChanged();
    }

    private async Task SetRangeAsync(double lower, double upper)
    {
        var lowerChanged = LowerValue != lower;
        var upperChanged = UpperValue != upper;

        if (lowerChanged is false && upperChanged is false) return;

        LowerValue = lower;
        UpperValue = upper;

        if (lowerChanged)
        {
            await LowerValueChanged.InvokeAsync(lower);
        }

        if (upperChanged)
        {
            await UpperValueChanged.InvokeAsync(upper);
        }

        var range = new BitSliderRangeValue(lower, upper);

        // The composite parameter is notified whichever end moved, since either one is a change of the range.
        await RangeValueChanged.InvokeAsync(range);

        NotifyFieldChanged();

        await OnRangeChange.InvokeAsync(range);
    }

    /// <summary>
    /// Tells the EditContext that the range has moved, which is what a single-value slider already gets out
    /// of SetCurrentValueAsync and what makes the validation of a ranged one run as the thumbs are dragged
    /// rather than only when the form is submitted.
    /// </summary>
    private void NotifyFieldChanged()
    {
        // Without an expression to build it from there is no field to report, and the identifier of one that
        // was never created cannot even be hashed.
        if (EditContext is null || NoValidate || FieldIdentifier.Model is null) return;

        EditContext.NotifyFieldChanged(FieldIdentifier);
    }

    /// <summary>
    /// Whether the range is pinned by a binding that has no way of being told about a change. Each of the
    /// three parameters that can carry the range blocks on its own: one bound one-way, with neither its
    /// Changed callback nor OnRangeChange to report to, is a value the parent means to control.
    /// </summary>
    private bool InvalidRangeBinding()
    {
        if (OnRangeChange.HasDelegate) return false;

        if (LowerValueHasBeenSet && LowerValueChanged.HasDelegate is false) return true;
        if (UpperValueHasBeenSet && UpperValueChanged.HasDelegate is false) return true;
        if (RangeValueHasBeenSet && RangeValueChanged.HasDelegate is false) return true;

        return false;
    }
}
