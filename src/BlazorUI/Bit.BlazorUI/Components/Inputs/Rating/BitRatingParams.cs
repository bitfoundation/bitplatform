namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitRating"/> component.
/// </summary>
public class BitRatingParams : BitInputBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitRating"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitRating value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitRating)}";



    public string Name => ParamName;



    /// <summary>
    /// Lets the current value be cleared, by clicking the item that is already selected or by pressing
    /// Delete or Backspace. Clearing sets the value to 0, so it also makes 0 a reachable value the same
    /// way <see cref="AllowZeroStars"/> does, and the items empty under a pointer resting on the committed
    /// value, since the preview is the value a click would commit.
    /// </summary>
    public bool? AllowClear { get; set; }

    /// <summary>
    /// Puts the unrated 0 in the range of the rating, so a value of 0 is kept instead of being pulled up to
    /// the smallest step and the rating can start empty. The keys that reach the ends of the range - Home and
    /// the 0 key - reach it, while the pointer always commits at least one step and Delete stays behind
    /// <see cref="AllowClear"/>.
    /// </summary>
    public bool? AllowZeroStars { get; set; }

    /// <summary>
    /// Names each individual rating item - not the rating as a whole - for screen readers. Placeholder {0} is
    /// the rating that item stands for, which is its one-based position, and placeholder {1} is the max: for
    /// example, "Select {0} of {1} stars" names the third of five items "Select 3 of 5 stars".
    /// </summary>
    public string? AriaLabelFormat { get; set; }

    /// <summary>
    /// The id of an element that names the rating as a whole. It wins over every other source of the name,
    /// including the visible <see cref="Label"/>.
    /// </summary>
    public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// If true, the rating automatically receives focus when the page renders.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitRating.
    /// </summary>
    public BitRatingClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the rating, applied to the filled part of the items.
    /// The unfilled part stays neutral so it reads as "not rated yet" whichever color is picked.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The hint shown under the items and pointed at by <c>aria-describedby</c>, for the instruction a row of
    /// stars cannot give by itself.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Names the rating as a whole from its current value and the max, which arrive as the first and the
    /// second argument. It is used whenever the AriaLabel is not set, and like that label it wins over the
    /// visible <see cref="Label"/>.
    /// </summary>
    public Func<double, double, string>? GetAriaLabel { get; set; }

    /// <summary>
    /// Chooses the selected (filled) icon of each rating item separately, from the one-based position of the item.
    /// Returning null falls back to <see cref="SelectedIcon"/> / <see cref="SelectedIconName"/>.
    /// </summary>
    public Func<int, BitIconInfo?>? GetSelectedIcon { get; set; }

    /// <summary>
    /// Chooses the unselected (empty) icon of each rating item separately, from the one-based position of the item.
    /// Returning null falls back to <see cref="UnselectedIcon"/> / <see cref="UnselectedIconName"/>.
    /// </summary>
    public Func<int, BitIconInfo?>? GetUnselectedIcon { get; set; }

    /// <summary>
    /// Highlights only the item matching the current value instead of every item up to it,
    /// turning the rating into a scale of standalone choices rather than a cumulative one.
    /// </summary>
    public bool? HighlightSelectedOnly { get; set; }

    /// <summary>
    /// The native tooltips of the rating items, in order, shown when hovering over each one, and used as the
    /// accessible name of the item unless the AriaLabelFormat overrides it. The items of a read-only or
    /// disabled rating take no pointer events, so their tooltips never appear there.
    /// </summary>
    public IList<string>? ItemTitles { get; set; }

    /// <summary>
    /// The visible label of the rating, which also becomes its accessible name.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Where the label sits relative to the items.
    /// </summary>
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Maximum rating, which is also the number of rendered items. Values below 1 are treated as 1.
    /// </summary>
    public int? Max { get; set; }

    /// <summary>
    /// Turns off the preview that follows the pointer over the items and shows the value that a click would
    /// commit. Only the preview the component paints stops: the OnHoverChange callback goes on reporting.
    /// </summary>
    public bool? NoHoverPreview { get; set; }

    /// <summary>
    /// The smallest change of the value the user can make, as a fraction of a single item.
    /// The default of 1 only allows whole items, 0.5 adds halves, 0.1 makes every tenth selectable, and so on;
    /// anything at or above 1, and anything at or below 0, leaves the items whole.
    /// It is also the floor of the scale unless <see cref="AllowZeroStars"/> or <see cref="AllowClear"/>
    /// opens up the unrated 0.
    /// </summary>
    public double? Precision { get; set; }

    /// <summary>
    /// The icon to display for selected rating elements using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SelectedIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? SelectedIcon { get; set; }

    /// <summary>
    /// Custom icon name for selected rating elements. If unset, default will be the FavoriteStarFill icon.
    /// </summary>
    public string? SelectedIconName { get; set; }

    /// <summary>
    /// Size of the rating, which scales the item glyphs, the label and the description together.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitRating.
    /// </summary>
    public BitRatingClassStyles? Styles { get; set; }

    /// <summary>
    /// The icon to display for unselected rating elements using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UnselectedIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? UnselectedIcon { get; set; }

    /// <summary>
    /// Custom icon name for unselected rating elements. If unset, default will be the FavoriteStar icon.
    /// </summary>
    public string? UnselectedIconName { get; set; }

    /// <summary>
    /// The format of the spoken form of the current value, where placeholder {0} is the value and placeholder
    /// {1} is the max: for example "{0} out of {1} stars". The default is "{0} of {1}".
    /// </summary>
    public string? ValueTextFormat { get; set; }

    /// <summary>
    /// Stacks the rating items in a column instead of a row, filling from the bottom up so that "more" is up,
    /// the way the ArrowUp key means more.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitRating"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitRating"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitRating"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitRating"/>.
    /// </remarks>
    /// <param name="bitRating">
    /// The <see cref="BitRating"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitRating bitRating)
    {
        if (bitRating is null) return;

        UpdateInputParameters(bitRating);

        if (AllowClear.HasValue && bitRating.HasNotBeenSet(nameof(AllowClear)))
        {
            bitRating.AllowClear = AllowClear.Value;
        }

        if (AllowZeroStars.HasValue && bitRating.HasNotBeenSet(nameof(AllowZeroStars)))
        {
            bitRating.AllowZeroStars = AllowZeroStars.Value;
        }

        if (AriaLabelFormat.HasValue() && bitRating.HasNotBeenSet(nameof(AriaLabelFormat)))
        {
            bitRating.AriaLabelFormat = AriaLabelFormat;
        }

        if (AriaLabelledBy.HasValue() && bitRating.HasNotBeenSet(nameof(AriaLabelledBy)))
        {
            bitRating.AriaLabelledBy = AriaLabelledBy;
        }

        if (AutoFocus.HasValue && bitRating.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitRating.AutoFocus = AutoFocus.Value;
        }

        if (Classes is not null && bitRating.HasNotBeenSet(nameof(Classes)))
        {
            bitRating.Classes = Classes;

            bitRating.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitRating.HasNotBeenSet(nameof(Color)))
        {
            bitRating.Color = Color.Value;

            bitRating.ClassBuilder.Reset();
        }

        if (Description.HasValue() && bitRating.HasNotBeenSet(nameof(Description)))
        {
            bitRating.Description = Description;
        }

        if (GetAriaLabel is not null && bitRating.HasNotBeenSet(nameof(GetAriaLabel)))
        {
            bitRating.GetAriaLabel = GetAriaLabel;
        }

        if (GetSelectedIcon is not null && bitRating.HasNotBeenSet(nameof(GetSelectedIcon)))
        {
            bitRating.GetSelectedIcon = GetSelectedIcon;
        }

        if (GetUnselectedIcon is not null && bitRating.HasNotBeenSet(nameof(GetUnselectedIcon)))
        {
            bitRating.GetUnselectedIcon = GetUnselectedIcon;
        }

        if (HighlightSelectedOnly.HasValue && bitRating.HasNotBeenSet(nameof(HighlightSelectedOnly)))
        {
            bitRating.HighlightSelectedOnly = HighlightSelectedOnly.Value;
        }

        if (ItemTitles is not null && bitRating.HasNotBeenSet(nameof(ItemTitles)))
        {
            bitRating.ItemTitles = ItemTitles;
        }

        if (Label.HasValue() && bitRating.HasNotBeenSet(nameof(Label)))
        {
            bitRating.Label = Label;
        }

        if (LabelPosition.HasValue && bitRating.HasNotBeenSet(nameof(LabelPosition)))
        {
            bitRating.LabelPosition = LabelPosition.Value;

            bitRating.ClassBuilder.Reset();
        }

        if (Max.HasValue && bitRating.HasNotBeenSet(nameof(Max)))
        {
            bitRating.Max = Max.Value;
        }

        if (NoHoverPreview.HasValue && bitRating.HasNotBeenSet(nameof(NoHoverPreview)))
        {
            bitRating.NoHoverPreview = NoHoverPreview.Value;

            bitRating.ClassBuilder.Reset();
        }

        if (Precision.HasValue && bitRating.HasNotBeenSet(nameof(Precision)))
        {
            bitRating.Precision = Precision.Value;
        }

        if (SelectedIcon is not null && bitRating.HasNotBeenSet(nameof(SelectedIcon)))
        {
            bitRating.SelectedIcon = SelectedIcon;
        }

        if (SelectedIconName.HasValue() && bitRating.HasNotBeenSet(nameof(SelectedIconName)))
        {
            bitRating.SelectedIconName = SelectedIconName;
        }

        if (Size.HasValue && bitRating.HasNotBeenSet(nameof(Size)))
        {
            bitRating.Size = Size.Value;

            bitRating.ClassBuilder.Reset();
        }

        if (Styles is not null && bitRating.HasNotBeenSet(nameof(Styles)))
        {
            bitRating.Styles = Styles;

            bitRating.StyleBuilder.Reset();
        }

        if (UnselectedIcon is not null && bitRating.HasNotBeenSet(nameof(UnselectedIcon)))
        {
            bitRating.UnselectedIcon = UnselectedIcon;
        }

        if (UnselectedIconName.HasValue() && bitRating.HasNotBeenSet(nameof(UnselectedIconName)))
        {
            bitRating.UnselectedIconName = UnselectedIconName;
        }

        if (ValueTextFormat.HasValue() && bitRating.HasNotBeenSet(nameof(ValueTextFormat)))
        {
            bitRating.ValueTextFormat = ValueTextFormat;
        }

        if (Vertical.HasValue && bitRating.HasNotBeenSet(nameof(Vertical)))
        {
            bitRating.Vertical = Vertical.Value;

            bitRating.ClassBuilder.Reset();
        }
    }
}
