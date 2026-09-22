using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Ratings show people’s opinions of a product, helping others make more informed purchasing decisions.
/// It supports fractional values down to any precision, a live hover preview, clearing, a label and a
/// description, per-item icons and titles, a custom item template, a horizontal or vertical layout, and is
/// fully operable from the keyboard.
/// </summary>
public partial class BitRating : BitInputBase<double>
{
    private string _labelId = default!;
    private string _valueTextId = default!;
    private string _descriptionId = default!;
    private double? _hoverValue;
    private ElementReference[] _itemRefs = [];



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the rating component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple rating components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitRatingParams.ParamName)]
    public BitRatingParams? CascadingParameters { get; set; }



    /// <summary>
    /// Lets the current value be cleared, by clicking the item that is already selected or by pressing
    /// Delete or Backspace. Clearing sets the value to 0, so it also makes 0 a reachable value the same
    /// way <see cref="AllowZeroStars"/> does.
    /// </summary>
    /// <remarks>
    /// Since the hover preview is the value a click would commit, the items empty while the pointer rests
    /// on the value that is already committed - which is what makes clearing discoverable with the pointer.
    /// <see cref="OnHoverChange"/> reports that 0 for the same reason.
    /// </remarks>
    [Parameter] public bool AllowClear { get; set; }

    /// <summary>
    /// Puts the unrated 0 in the range of the rating, so a value of 0 is kept instead of being pulled up to
    /// the smallest step and the rating can start empty.
    /// </summary>
    /// <remarks>
    /// It makes 0 part of the scale rather than something the user can undo their way back to: the keys that
    /// reach the ends of the range - <c>Home</c> and the <c>0</c> key - reach it, while the pointer always
    /// commits at least one step and <c>Delete</c> stays behind <see cref="AllowClear"/>, which is the
    /// parameter for clearing as an action.
    /// </remarks>
    [Parameter] public bool AllowZeroStars { get; set; }

    /// <summary>
    /// Names each individual rating item - not the rating as a whole - for screen readers. Placeholder {0} is
    /// the rating that item stands for, which is its one-based position, and placeholder {1} is the max: for
    /// example, "Select {0} of {1} stars" names the third of five items "Select 3 of 5 stars".
    /// </summary>
    /// <remarks>
    /// Without it an item is named by its <see cref="ItemTitles"/> tooltip, and failing that by its position
    /// in the scale, so an item is never left nameless. To name the rating as a whole use
    /// <see cref="Label"/>, <see cref="BitComponentBase.AriaLabel"/>, <see cref="AriaLabelledBy"/> or
    /// <see cref="GetAriaLabel"/> instead.
    /// </remarks>
    [Parameter] public string? AriaLabelFormat { get; set; }

    /// <summary>
    /// The id of an element that names the rating as a whole, for a name that is already written somewhere
    /// on the page. It wins over every other source of the name, including the visible <see cref="Label"/>.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

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
    /// The hint shown under the items and pointed at by <c>aria-describedby</c>, for the instruction a row
    /// of stars cannot give by itself - that half a star is selectable, say, or that clicking the current
    /// one clears it. It describes the rating rather than naming it, so it is announced after the label.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Replaces the <see cref="Description"/> with custom content, which is still what describes the rating
    /// for assistive technologies.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// The general color of the rating, applied to the filled part of the items.
    /// The unfilled part stays neutral so it reads as "not rated yet" whichever color is picked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Names the rating as a whole from its current value and the max, which arrive as the first and the
    /// second argument. It is used whenever <see cref="BitComponentBase.AriaLabel"/> is not set, and like
    /// that label it wins over the visible <see cref="Label"/>.
    /// </summary>
    /// <remarks>
    /// A read-only rating has to carry its value in its name, since its items are hidden behind that single
    /// name - this is how to word it. An interactive one announces the value through a live region instead,
    /// so there the callback only renames the group.
    /// </remarks>
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
    /// <remarks>
    /// The template draws the item and nothing else: the item goes on being the radio of the group, keeping
    /// its hit area, its hover preview, its keyboard handling and its name, and the drawing is hidden from
    /// assistive technologies as the built-in glyphs are - so a template that renders a number or a face does
    /// not append it to the name of the item. Anything focusable is therefore out of place inside one.
    /// </remarks>
    [Parameter] public RenderFragment<BitRatingItemContext>? ItemTemplate { get; set; }

    /// <summary>
    /// The native tooltips of the rating items, in order, shown when hovering over each one:
    /// for example "Terrible", "Bad", "Normal", "Good", "Wonderful". They double as the accessible name of
    /// each item unless <see cref="AriaLabelFormat"/> overrides it, so they say more about an item than its
    /// position does. Items beyond the end of the list simply get no tooltip.
    /// </summary>
    /// <remarks>
    /// The items of a read-only or disabled rating take no pointer events, so their tooltips never appear
    /// there: a read-only rating is announced as a single value rather than as a set of choices to describe.
    /// </remarks>
    [Parameter] public IList<string>? ItemTitles { get; set; }

    /// <summary>
    /// The visible label of the rating, which also becomes its accessible name: a row of stars carries no
    /// text of its own, so without a label - or an <see cref="BitComponentBase.AriaLabel"/> - the group is
    /// announced without saying what is being rated. A required rating marks its label with an asterisk.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Where the label sits relative to the items: above them by default, and beside them with
    /// <see cref="BitLabelPosition.Start"/> or <see cref="BitLabelPosition.End"/> for the compact
    /// "Quality: 3 of 5" row.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Replaces the <see cref="Label"/> with custom content, which still names the rating for assistive
    /// technologies the same way the plain label does.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Maximum rating, which is also the number of rendered items. Values below 1 are treated as 1.
    /// </summary>
    [Parameter] public int Max { get; set; } = 5;

    /// <summary>
    /// Turns off the preview that follows the pointer over the items and shows the value
    /// that a click would commit.
    /// </summary>
    /// <remarks>
    /// Only the preview drawn by the component stops: <see cref="OnHoverChange"/> goes on reporting the
    /// hovered value, which is what a page that draws a preview of its own needs.
    /// </remarks>
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
    /// Callback for when the rating receives the focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when the focus leaves the rating.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when the previewed value changes, which is the value a click would commit.
    /// It receives null when the pointer leaves the rating and the preview ends.
    /// </summary>
    /// <remarks>
    /// It reports the hovered value whether or not the component draws the preview itself, so it keeps
    /// working under <see cref="NoHoverPreview"/>.
    /// </remarks>
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
    /// Anything at or above 1, and anything at or below 0, leaves the items whole.
    /// <br />
    /// It is also the floor of the scale: the smallest rating that can be given is a single step, so a
    /// half-star rating reaches 0.5 without opening up the unrated 0 that <see cref="AllowZeroStars"/> and
    /// <see cref="AllowClear"/> are for.
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
    /// Size of the rating, which scales the item glyphs, the label and the description together.
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
        _labelId = $"BitRating-{UniqueId}-label";
        _valueTextId = $"BitRating-{UniqueId}-value";
        _descriptionId = $"BitRating-{UniqueId}-description";

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

        // The asterisk is a property of an answer that is still expected, so a read-only or disabled
        // rating - which is no longer asking anything - does not draw one.
        ClassBuilder.Register(() => IsEnabled && ReadOnly is false && Required ? "bit-rtg-req" : string.Empty);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Bottom => "bit-rtg-lbm",
            BitLabelPosition.Start => "bit-rtg-lst",
            BitLabelPosition.End => "bit-rtg-led",
            _ => string.Empty
        });

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

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRatingParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        var max = _Max;

        if (_itemRefs.Length != max)
        {
            _itemRefs = new ElementReference[max];
        }

        // A rating turned read-only or disabled under the pointer stops receiving the mouseleave that would
        // normally end the preview, so a stale one would go on rendering in place of the committed value.
        // NoHoverPreview is not one of these: it hides the preview rather than ending it, so the hovered
        // value stays tracked for OnHoverChange and simply stops being what the items are drawn from.
        if (IsEnabled is false || ReadOnly)
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
    /// The smallest value the rating can hold. Both AllowZeroStars and AllowClear open up the unrated 0;
    /// without them the floor is the smallest rating that can still be given, which is a single step -
    /// a whole item at the default Precision, and the first half of the first one at a Precision of 0.5.
    /// </summary>
    private double _MinValue => (AllowZeroStars || AllowClear) ? 0 : _Step;

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
    /// The value the items are rendered from: the hovered one while a preview is active, the committed one
    /// otherwise. A rating whose preview is turned off still tracks the hovered value for OnHoverChange,
    /// so the choice of what to draw is made here rather than by stopping the tracking.
    /// </summary>
    private double _DisplayValue => (NoHoverPreview ? null : _hoverValue) ?? CurrentValue;

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
    /// Whether the rating draws a label of its own, which is also what makes it able to name itself.
    /// </summary>
    /// <remarks>
    /// Only rendered when there is something to show, so its id is only worth referencing then: pointing
    /// aria-labelledby at an element that is not there would leave the group without a name at all, since a
    /// name given by reference wins over the aria-label beside it.
    /// </remarks>
    internal bool HasLabel => LabelTemplate is not null || Label.HasValue();

    /// <summary>
    /// Whether the rating draws a description of its own, on the same terms as its label.
    /// </summary>
    internal bool HasDescription => DescriptionTemplate is not null || Description.HasValue();

    /// <summary>
    /// The value of an aria attribute the consumer splatted onto the component. Every aria attribute the
    /// rating computes sits after the HtmlAttributes splat in the markup, so it is what ends up rendered no
    /// matter what - and a null would even remove a splatted value. This is what the computed attributes
    /// hand back rather than erasing what the page wrote.
    /// </summary>
    private string? _GetSplattedAttribute(string name)
    {
        HtmlAttributes.TryGetValue(name, out var value);

        return value?.ToString();
    }

    /// <summary>
    /// The elements that describe the rating, which is a splatted aria-describedby carried over rather than
    /// replaced: both are kept, since aria-describedby is a space separated list of IDREFs.
    /// </summary>
    private string? _AriaDescribedBy
    {
        get
        {
            var splattedDescribedBy = _GetSplattedAttribute("aria-describedby");

            if (HasDescription is false) return splattedDescribedBy;

            return splattedDescribedBy.HasValue() ? $"{splattedDescribedBy} {_descriptionId}" : _descriptionId;
        }
    }

    /// <summary>
    /// The name of the rating as an inline string, which is only rendered when nothing names it by
    /// reference. A name the page splatted is the last resort, so that writing aria-label on the component
    /// works as it reads even though the component owns the attribute.
    /// </summary>
    private string? _AriaLabelAttribute => _AriaLabelledBy is null
        ? (_AriaLabel ?? _GetSplattedAttribute("aria-label"))
        : null;

    /// <summary>
    /// The element the name of the rating is read from, when it is read from the page rather than given as a
    /// string: the explicit AriaLabelledBy, then the visible label. The two string forms - AriaLabel and the
    /// GetAriaLabel callback - are deliberately allowed to win over the visible label, since aria-labelledby
    /// would otherwise silently discard them.
    /// </summary>
    private string? _AriaLabelledBy
    {
        get
        {
            if (AriaLabelledBy.HasValue()) return AriaLabelledBy;

            if (AriaLabel.HasValue() || GetAriaLabel is not null) return null;

            if (HasLabel is false) return _GetSplattedAttribute("aria-labelledby");

            return _RendersHiddenValueText ? $"{_labelId} {_valueTextId}" : _labelId;
        }
    }

    /// <summary>
    /// Whether the value joins the name of the rating from a hidden element of its own. A read-only rating is
    /// a picture of a value whose items are hidden behind a single name, so naming it by its visible label
    /// alone would leave the value it exists to show unannounced.
    /// </summary>
    private bool _RendersHiddenValueText => ReadOnly
                                         && HasLabel
                                         && AriaLabelledBy.HasNoValue()
                                         && AriaLabel.HasNoValue()
                                         && GetAriaLabel is null;

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
        if (IsEnabled is false || ReadOnly) return;

        // The preview is the value a click would commit, and under AllowClear a click on the value that is
        // already committed commits 0 instead - so that is the value the preview shows, rather than the one
        // the pointer is over. It costs nothing and buys the one thing AllowClear otherwise lacks: with the
        // row emptying under the pointer, clearing is discoverable without having to be described first.
        if (AllowClear && value == CurrentValue)
        {
            value = 0;
        }

        // With the preview off and nobody listening there is nothing a hover could change, so the render
        // it would cost is skipped entirely.
        if (NoHoverPreview && OnHoverChange.HasDelegate is false) return;

        if (_hoverValue == value) return;

        _hoverValue = value;

        await OnHoverChange.InvokeAsync(value);
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

    private Task HandleOnMouseLeave() => EndPreview();

    /// <summary>
    /// Takes the preview down, which puts the items back to the committed value and reports the end of it.
    /// </summary>
    private async Task EndPreview()
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

        // Every key this handler answers to is also half of a browser or system shortcut - Alt+ArrowLeft goes
        // back, Ctrl+Home reaches the top of a page, Ctrl+digit switches tabs - so a held modifier hands the
        // key back rather than silently spending it on the rating. Shift is the exception: it is the rating's
        // own modifier, the one that turns a step into a whole item.
        if (e.CtrlKey || e.AltKey || e.MetaKey) return;

        var isRtl = Dir == BitDir.Rtl;
        var value = CurrentValue;

        // Holding Shift - and the Page keys, which need no modifier for it - moves by a whole item to the
        // next or previous one, so crossing a rating split into tenths costs five presses instead of fifty.
        var coarse = e.ShiftKey || e.Key is "PageUp" or "PageDown";
        var step = coarse ? 1 : _Step;
        var up = StepFrom(value, step, true);
        var down = StepFrom(value, step, false);

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
    /// The next value up or down the grid a step of the given size lays over the scale.
    /// </summary>
    /// <remarks>
    /// The move lands on that grid rather than adding the step to whatever the value happens to be, which
    /// matters for a value the Precision never snapped: one bound from elsewhere at 4.3 on a half-star
    /// scale moves to 4.5 and 4 instead of carrying its own remainder up and down a scale that cannot
    /// express it. On a value already on the grid the two are the same thing.
    /// </remarks>
    private static double StepFrom(double value, double step, bool up)
    {
        var steps = value / step;

        // A value that is exactly on the grid divides into a whole number of steps only to within the
        // rounding of binary floating point, so the index is taken with a tolerance: without it the floor
        // of a 2.9999999999 would move up to the step the value is already sitting on.
        const double tolerance = 1e-4;

        var index = up ? Math.Floor(steps + tolerance) + 1
                       : Math.Ceiling(steps - tolerance) - 1;

        return index * step;
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
        await CommitValue(value);

        // Whatever became of it, the interaction that got here has spent the preview: keeping it would go
        // on rendering the hovered value in place of the committed one, and a value an OnChanging refused
        // or a one-way binding never took would be previewed as though it had landed. The pointer leaving
        // is no longer the only way out of it, because on a touch device that never happens.
        await EndPreview();
    }

    private async Task CommitValue(double value)
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
