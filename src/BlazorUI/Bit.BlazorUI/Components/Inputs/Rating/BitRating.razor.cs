using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Ratings show people’s opinions of a product, helping others make more informed purchasing decisions.
/// It supports fractional values down to any precision, a live hover preview, clearing, per-item titles,
/// a custom item template, and is fully operable from the keyboard.
/// </summary>
public partial class BitRating : BitInputBase<double>
{
    private double? _hoverValue;
    private ElementReference[] _itemRefs = [];



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Lets the current value be cleared, by clicking the item that is already selected or by pressing
    /// Delete or Backspace. Clearing sets the value to 0, so it also makes 0 a reachable value the same
    /// way <see cref="AllowZeroStars"/> does.
    /// </summary>
    [Parameter] public bool AllowClear { get; set; }

    /// <summary>
    /// Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard
    /// unless <see cref="AllowClear"/> is also set.
    /// </summary>
    [Parameter] public bool AllowZeroStars { get; set; }

    /// <summary>
    /// Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers.
    /// Placeholder {0} is the current rating and placeholder {1} is the max: for example,
    /// "Select {0} of {1} stars". (To set the label for the control as a whole, use GetAriaLabel or AriaLabel.)
    /// </summary>
    [Parameter] public string? AriaLabelFormat { get; set; }

    /// <summary>
    /// If true, the rating automatically receives focus when the page renders
    /// (rendered as the <c>autofocus</c> attribute of the item that holds the tab stop).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitRating.
    /// </summary>
    [Parameter] public BitRatingClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the rating, applied to the filled part of the items.
    /// The unfilled part stays neutral so it reads as "not rated yet" whichever color is picked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if AriaLabel parameter is not provided.
    /// The first argument is the current value and the second one is the max.
    /// </summary>
    [Parameter] public Func<double, double, string>? GetAriaLabel { get; set; }

    /// <summary>
    /// Chooses the selected (filled) icon of each rating item separately, from the one-based position of
    /// the item. Returning null falls back to <see cref="SelectedIcon"/> / <see cref="SelectedIconName"/>,
    /// so only the positions that need their own glyph have to be answered.
    /// </summary>
    /// <remarks>
    /// This is the lightweight half of <see cref="ItemTemplate"/>: it changes which glyph an item draws
    /// while the component keeps drawing it, so the partial fill of a fractional value still works.
    /// </remarks>
    /// <example>
    /// GetSelectedIcon="i => BitIconInfo.Bit(i > 3 ? "LikeSolid" : "DislikeSolid")"
    /// </example>
    [Parameter] public Func<int, BitIconInfo?>? GetSelectedIcon { get; set; }

    /// <summary>
    /// Chooses the unselected (empty) icon of each rating item separately, from the one-based position of
    /// the item. Returning null falls back to <see cref="UnselectedIcon"/> / <see cref="UnselectedIconName"/>,
    /// so only the positions that need their own glyph have to be answered.
    /// </summary>
    /// <example>
    /// GetUnselectedIcon="i => BitIconInfo.Bit(i > 3 ? "Like" : "Dislike")"
    /// </example>
    [Parameter] public Func<int, BitIconInfo?>? GetUnselectedIcon { get; set; }

    /// <summary>
    /// Highlights only the item matching the current value instead of every item up to it,
    /// turning the rating into a scale of standalone choices rather than a cumulative one.
    /// </summary>
    [Parameter] public bool HighlightSelectedOnly { get; set; }

    /// <summary>
    /// Replaces the default pair of icons of every rating item with custom content.
    /// The provided <see cref="BitRatingItemContext"/> describes the item being rendered,
    /// including how much of it is filled.
    /// </summary>
    [Parameter] public RenderFragment<BitRatingItemContext>? ItemTemplate { get; set; }

    /// <summary>
    /// The native tooltips of the rating items, in order, shown when hovering over each one:
    /// for example "Terrible", "Bad", "Normal", "Good", "Wonderful".
    /// Items beyond the end of the list simply get no tooltip.
    /// </summary>
    [Parameter] public IList<string>? ItemTitles { get; set; }

    /// <summary>
    /// Maximum rating, which is also the number of rendered items. Values below 1 are treated as 1.
    /// </summary>
    [Parameter] public int Max { get; set; } = 5;

    /// <summary>
    /// Turns off the preview that follows the pointer over the items and shows the value
    /// that a click would commit.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoHoverPreview { get; set; }

    /// <summary>
    /// Callback invoked before the value of the rating changes, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitRatingChangeArgs"/> to keep the current value.
    /// Since the callback is awaited, it can also run asynchronous work like a confirmation prompt.
    /// </remarks>
    [Parameter] public EventCallback<BitRatingChangeArgs> OnChanging { get; set; }

    /// <summary>
    /// Callback for when the previewed value changes, which is the value a click would commit.
    /// It receives null when the pointer leaves the rating and the preview ends.
    /// </summary>
    [Parameter] public EventCallback<double?> OnHoverChange { get; set; }

    /// <summary>
    /// The smallest change of the value the user can make, as a fraction of a single item.
    /// The default of 1 only allows whole items, 0.5 adds halves, 0.1 makes every tenth selectable, and so on.
    /// It constrains what the user can pick, not what can be displayed: a bound value of any precision is
    /// always rendered exactly.
    /// </summary>
    /// <remarks>
    /// A precision that does not divide an item evenly - 0.3, say - is rounded to the closest number of equal
    /// steps, so the items always end on a whole value, and an item is never split into more than 100 steps.
    /// </remarks>
    [Parameter] public double Precision { get; set; } = 1;

    /// <summary>
    /// Gets or sets the icon to display for selected rating elements using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SelectedIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="SelectedIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: SelectedIcon="BitIconInfo.Bi("star-fill")"
    /// FontAwesome: SelectedIcon="BitIconInfo.Fa("solid star")"
    /// Custom CSS: SelectedIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? SelectedIcon { get; set; }

    /// <summary>
    /// Custom icon name for selected rating elements. If unset, default will be the FavoriteStarFill icon.
    /// The icon name should be from the Fluent UI icon set. For external icon libraries, use <see cref="SelectedIcon"/> instead.
    /// </summary>
    [Parameter] public string? SelectedIconName { get; set; }

    /// <summary>
    /// Size of rating elements.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitRating.
    /// </summary>
    [Parameter] public BitRatingClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the icon to display for unselected rating elements using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UnselectedIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="UnselectedIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: UnselectedIcon="BitIconInfo.Bi("star")"
    /// FontAwesome: UnselectedIcon="BitIconInfo.Fa("solid star")"
    /// Custom CSS: UnselectedIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? UnselectedIcon { get; set; }

    /// <summary>
    /// Custom icon name for unselected rating elements. If unset, default will be the FavoriteStar icon.
    /// The icon name should be from the Fluent UI icon set. For external icon libraries, use <see cref="UnselectedIcon"/> instead.
    /// </summary>
    [Parameter] public string? UnselectedIconName { get; set; }

    /// <summary>
    /// The format of the spoken form of the current value, where placeholder {0} is the value and
    /// placeholder {1} is the max: for example "{0} out of {1} stars". It is what the live region of an
    /// interactive rating announces for a value no radio can carry, and what a read-only rating falls back
    /// to when it is given no other label. The default is "{0} of {1}".
    /// </summary>
    [Parameter] public string? ValueTextFormat { get; set; }

    /// <summary>
    /// Stacks the rating items in a column instead of a row, filling from the bottom up so that "more" is
    /// up, the way the ArrowUp key means more. The partial fill of a fractional value grows from the bottom
    /// edge of its item accordingly.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    protected override async Task OnInitializedAsync()
    {
        SetDefaultValue();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The hidden number input is the form value carrier of the rating, not something the user is ever
        // meant to land on, so FocusAsync is pointed at the item that actually holds the tab stop instead.
        var index = _TabbableIndex;

        if (index >= 1 && index <= _itemRefs.Length)
        {
            InputElement = _itemRefs[index - 1];
        }

        if (firstRender)
        {
            try
            {
                // Prevents the default behavior (scrolling) of the navigation keys handled by the items'
                // keydown handler, since Blazor cannot conditionally preventDefault per key.
                await _js.BitRatingsSetup(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override string RootElementClass => "bit-rtg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ReadOnly ? "bit-rtg-rdl" : string.Empty);

        ClassBuilder.Register(() => NoHoverPreview ? "bit-rtg-nhp" : string.Empty);

        ClassBuilder.Register(() => Vertical ? "bit-rtg-vrt" : string.Empty);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-rtg-pri",
            BitColor.Secondary => "bit-rtg-sec",
            BitColor.Tertiary => "bit-rtg-ter",
            BitColor.Info => "bit-rtg-inf",
            BitColor.Success => "bit-rtg-suc",
            BitColor.Warning => "bit-rtg-wrn",
            BitColor.SevereWarning => "bit-rtg-swr",
            BitColor.Error => "bit-rtg-err",
            BitColor.PrimaryBackground => "bit-rtg-pbg",
            BitColor.SecondaryBackground => "bit-rtg-sbg",
            BitColor.TertiaryBackground => "bit-rtg-tbg",
            BitColor.PrimaryForeground => "bit-rtg-pfg",
            BitColor.SecondaryForeground => "bit-rtg-sfg",
            BitColor.TertiaryForeground => "bit-rtg-tfg",
            BitColor.PrimaryBorder => "bit-rtg-pbr",
            BitColor.SecondaryBorder => "bit-rtg-sbr",
            BitColor.TertiaryBorder => "bit-rtg-tbr",
            _ => "bit-rtg-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-rtg-sm",
            BitSize.Medium => "bit-rtg-md",
            BitSize.Large => "bit-rtg-lg",
            _ => "bit-rtg-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        var max = _Max;

        if (_itemRefs.Length != max)
        {
            _itemRefs = new ElementReference[max];
        }

        // A rating turned read-only or disabled under the pointer stops receiving the mouseleave that would
        // normally end the preview, so a stale one would go on rendering in place of the committed value.
        if (IsEnabled is false || ReadOnly || NoHoverPreview)
        {
            _hoverValue = null;
        }

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        // The value is kept inside the range the parameters describe, but it is never snapped to the
        // Precision: that constrains what the user can pick, not what the component is able to render.
        var clamped = ClampValue(CurrentValue);
        if (clamped != CurrentValue)
        {
            // A one-way bound value has nobody to notify, so it is corrected locally instead. Assigning
            // CurrentValue is avoided here as well, since its setter drops the write while IsEnabled is false.
            if (InvalidValueBinding())
            {
                Value = clamped;
            }
            else
            {
                // Awaited rather than fired and forgotten, so a handler that throws surfaces through the
                // lifecycle instead of disappearing into an unobserved task.
                await SetCurrentValueAsync(clamped);
            }
        }

        await base.OnParametersSetAsync();
    }

    // The value travels through a native number input, whose value attribute is only ever read and written
    // in the invariant format - a "3,5" produced by a comma-decimal culture would be rejected outright by
    // the browser - so both directions of the conversion are pinned to it.
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
    /// The number of rendered items, which never drops below a single one.
    /// </summary>
    private int _Max => Math.Max(Max, 1);

    /// <summary>
    /// The smallest value the rating can hold. Both AllowZeroStars and AllowClear open up the unrated 0.
    /// </summary>
    private double _MinValue => (AllowZeroStars || AllowClear) ? 0 : 1;

    /// <summary>
    /// How many selectable steps each item is divided into, derived from the Precision.
    /// </summary>
    private int _StepsPerItem
    {
        get
        {
            if (Precision >= 1 || double.IsNaN(Precision)) return 1;

            // A precision of 0 (or below) would ask for infinitely many steps, so it falls back to whole items.
            if (Precision <= 0) return 1;

            // A precision that does not divide an item evenly - 0.3, say - is rounded to the closest
            // number of equal steps (3 of them there), so the items always end on a whole value.
            return Math.Clamp((int)Math.Round(1 / Precision), 1, 100);
        }
    }

    /// <summary>
    /// The size of a single step, which is the Precision after it has been made to divide an item evenly.
    /// </summary>
    private double _Step => 1d / _StepsPerItem;

    /// <summary>
    /// The value the items are rendered from: the hovered one while a preview is active, the committed one otherwise.
    /// </summary>
    private double _DisplayValue => _hoverValue ?? CurrentValue;

    /// <summary>
    /// The item that holds the single tab stop of the group. It follows the value - a fractional one lands
    /// on the item it ends inside - and falls back to the first item whenever the value points at no item at
    /// all, so an unrated rating stays reachable with the Tab key.
    /// </summary>
    private int _TabbableIndex
    {
        get
        {
            var index = (int)Math.Ceiling(CurrentValue);

            return (index >= 1 && index <= _Max) ? index : 1;
        }
    }

    /// <summary>
    /// The accessible name of the whole rating: the explicit AriaLabel, then the GetAriaLabel callback, and
    /// finally - in read-only mode, where there is nothing left to describe the value - a "3.5 of 5" fallback.
    /// </summary>
    private string? _AriaLabel
    {
        get
        {
            if (AriaLabel.HasValue()) return AriaLabel;

            if (GetAriaLabel is not null) return GetAriaLabel(CurrentValue, _Max);

            if (ReadOnly is false) return null;

            return _ValueText;
        }
    }

    /// <summary>
    /// The default format both the value text and the per-item labels fall back to.
    /// </summary>
    private const string DefaultTextFormat = "{0} of {1}";

    /// <summary>
    /// The spoken form of the current value, which the ValueTextFormat can reword for another language or
    /// for a scale whose items are not stars.
    /// </summary>
    private string _ValueText => string.Format(CultureInfo.CurrentCulture,
                                               ValueTextFormat.HasValue() ? ValueTextFormat! : DefaultTextFormat,
                                               CurrentValue, _Max);

    /// <summary>
    /// What the live region of an interactive rating announces. A whole value is already carried by the
    /// radio it checks, so the region only speaks up for the fractional values no radio can express - but
    /// the region itself is rendered either way, since one inserted at the moment of the change would come
    /// too late for screen readers to announce it.
    /// </summary>
    private string? _LiveValueText
    {
        get
        {
            var isCarriedByARadio = CurrentValue > 0
                                 && CurrentValue <= _Max
                                 && CurrentValue == Math.Floor(CurrentValue);

            return isCarriedByARadio ? null : _ValueText;
        }
    }

    /// <summary>
    /// The min attribute of the hidden input, which native form validation reads.
    /// </summary>
    private string _MinAttribute => _MinValue.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The step attribute of the hidden input. A precision that has no exact decimal form - a third of an
    /// item, say - would make native constraint validation reject values the rating itself considers valid,
    /// so anything below a whole item leaves the stepping to the component and tells the browser "any".
    /// </summary>
    private string _StepAttribute => _StepsPerItem == 1 ? "1" : "any";

    private double ClampValue(double value)
    {
        if (double.IsNaN(value)) return _MinValue;

        return Math.Clamp(value, _MinValue, _Max);
    }

    /// <summary>
    /// How much of the item at the given position is filled, from 0 to 100.
    /// </summary>
    private double GetPercentage(int index)
    {
        var value = _DisplayValue;

        // Rounded because the subtraction is where binary floating point starts showing: a value of 3.7
        // would otherwise reach the width of the fourth item as 70.00000000000002%.
        var fill = Math.Round(Math.Clamp(value - (index - 1), 0, 1) * 100, 2);

        // Highlighting the selected item alone leaves every item before it empty, but the selected one is
        // still filled by however much of it the value covers, so a fractional value stays readable.
        if (HighlightSelectedOnly)
        {
            return Math.Ceiling(value) == index ? fill : 0;
        }

        return fill;
    }

    /// <summary>
    /// The inline style of the filled layer of an item. A vertical rating grows the fill along the block
    /// axis instead, which is what puts the partial fill of a fractional value at the bottom of its item.
    /// </summary>
    private string GetFillStyle(double percentage)
    {
        return Vertical
            ? FormattableString.Invariant($"height:{percentage}%; {Styles?.SelectedIcon}")
            : FormattableString.Invariant($"width:{percentage}%; {Styles?.SelectedIcon}");
    }

    /// <summary>
    /// The inline style of a single step slice, laid along the same axis the item fills on.
    /// </summary>
    private string GetSegmentStyle(int step)
    {
        return Vertical
            ? FormattableString.Invariant($"height:{GetStepWidth()}%; bottom:{GetStepOffset(step)}%")
            : FormattableString.Invariant($"width:{GetStepWidth()}%; inset-inline-start:{GetStepOffset(step)}%");
    }

    /// <summary>
    /// The width of a single step slice, as a percentage of the item it covers.
    /// </summary>
    private double GetStepWidth() => Math.Round(100d / _StepsPerItem, 4);

    /// <summary>
    /// Where a single step slice starts, as a percentage from the leading edge of the item it covers.
    /// </summary>
    private double GetStepOffset(int step) => Math.Round((step - 1) * 100d / _StepsPerItem, 4);

    /// <summary>
    /// The value a click on the given step of the given item commits.
    /// </summary>
    private double GetStepValue(int index, int step)
    {
        return Math.Round(index - 1 + step * _Step, 5);
    }

    /// <summary>
    /// The visually hidden per-item label screen readers announce. An explicit AriaLabelFormat decides it,
    /// then the tooltip of the item - "Wonderful" says more about the item than "5 of 5" does - and finally
    /// the position of the item in the scale, so a radio is never left without a name.
    /// </summary>
    private string GetItemAriaLabel(int index)
    {
        if (AriaLabelFormat.HasValue()) return string.Format(CultureInfo.CurrentCulture, AriaLabelFormat!, index, _Max);

        var title = GetItemTitle(index);

        if (title.HasValue()) return title!;

        return string.Format(CultureInfo.CurrentCulture, DefaultTextFormat, index, _Max);
    }

    private string? GetItemTitle(int index)
    {
        if (ItemTitles is null || index > ItemTitles.Count) return null;

        return ItemTitles[index - 1];
    }

    private async Task HandleOnClick(double value)
    {
        if (IsEnabled is false || ReadOnly) return;

        // Clicking the value that is already committed clears the rating, the way a second click on a
        // selected radio never could - which is the only way back to "not rated" once something is picked.
        if (AllowClear && value == CurrentValue)
        {
            value = 0;
        }

        await ChangeValue(value);
    }

    private async Task HandleOnHover(double value)
    {
        if (IsEnabled is false || ReadOnly || NoHoverPreview) return;

        if (_hoverValue == value) return;

        _hoverValue = value;

        await OnHoverChange.InvokeAsync(value);
    }

    private async Task HandleOnMouseLeave()
    {
        if (_hoverValue is null) return;

        _hoverValue = null;

        await OnHoverChange.InvokeAsync(null);
    }

    // The whole rating is a single tab stop that the arrow keys move through, as the WAI-ARIA radiogroup
    // pattern describes. The step of every move is the Precision, so a half-star rating is reachable from
    // the keyboard exactly like it is with the pointer.
    // The arrow, Home, End and Page keys scroll the page by default, so their default action is suppressed
    // while the rating navigates with them. That is done from JavaScript (see Ratings.setup) rather than
    // through @onkeydown:preventDefault, whose value only takes effect from the next render on - which is
    // one press too late - and is kept key-scoped there so Tab, Space and Enter still behave normally.
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        var isRtl = Dir == BitDir.Rtl;
        var value = CurrentValue;

        // Holding Shift - and the Page keys, which need no modifier for it - moves by a whole item to the
        // next or previous one, so crossing a rating split into tenths costs five presses instead of fifty.
        var coarse = e.ShiftKey || e.Key is "PageUp" or "PageDown";
        var up = coarse ? Math.Floor(value) + 1 : value + _Step;
        var down = coarse ? Math.Ceiling(value) - 1 : value - _Step;

        double? newValue = e.Key switch
        {
            "ArrowRight" => isRtl ? down : up,
            "ArrowLeft" => isRtl ? up : down,
            "ArrowUp" or "PageUp" => up,
            "ArrowDown" or "PageDown" => down,
            "Home" => _MinValue,
            "End" => _Max,
            // Escape is deliberately not a clearing key: a rating inside a modal or a panel would clear
            // itself on the way to dismissing its container, which is one press doing two things.
            "Delete" or "Backspace" => AllowClear ? 0 : null,
            _ => GetDigitValue(e.Key)
        };

        if (newValue is null) return;

        await ChangeValue(Math.Round(newValue.Value, 5));

        // The tab stop moves with the value, so the focus follows it to the item that now holds it.
        await FocusItem(_TabbableIndex);
    }

    /// <summary>
    /// The value a digit key jumps straight to, which is how a keyboard user reaches "4 of 5" in one press
    /// instead of four. A digit beyond the ends of the scale is held to them like any other value.
    /// </summary>
    /// <remarks>
    /// Any Unicode decimal digit counts, not only the ASCII ones, so the shortcut still works on the
    /// keyboard layouts a right-to-left rating is most likely to be used with.
    /// </remarks>
    private static double? GetDigitValue(string? key)
    {
        if (key is null || key.Length != 1) return null;

        var digit = key[0];

        return char.IsDigit(digit) ? char.GetNumericValue(digit) : null;
    }

    private async Task ChangeValue(double value)
    {
        if (InvalidValueBinding()) return;

        value = ClampValue(value);

        if (value == CurrentValue) return;

        if (OnChanging.HasDelegate)
        {
            var args = new BitRatingChangeArgs(value, CurrentValue);

            await OnChanging.InvokeAsync(args);

            if (args.Cancel) return;
        }

        // The preview has served its purpose once a value lands, and keeping it would render the
        // hovered value instead of the committed one until the pointer moves away.
        if (_hoverValue is not null)
        {
            _hoverValue = null;

            await OnHoverChange.InvokeAsync(null);
        }

        await SetCurrentValueAsync(value);
    }

    private async Task FocusItem(int index)
    {
        if (index < 1 || index > _itemRefs.Length) return;

        try
        {
            await _itemRefs[index - 1].FocusAsync();
        }
        catch (InvalidOperationException)
        {
            // The element reference is not attached yet (or anymore), which leaves the focus where it is.
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            await _js.BitRatingsDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
