namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPivot"/> component.
/// </summary>
/// <remarks>
/// The selection (SelectedKey and DefaultSelectedKey), the id that labels the header (AriaLabelledBy), the
/// templates and the event callbacks are left out on purpose: they belong to a single pivot rather than to a
/// group of them.
/// </remarks>
public class BitPivotParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPivot"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPivot value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPivot)}";



    public string Name => ParamName;



    /// <summary>
    /// Renders a button at the end of the pivot items that reports a request for a new tab through the OnAdd callback.
    /// </summary>
    public bool? Addable { get; set; }

    /// <summary>
    /// The aria-label of the add button of the pivot (default: Add).
    /// </summary>
    public string? AddAriaLabel { get; set; }

    /// <summary>
    /// The icon of the add button of the pivot using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="AddIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? AddIcon { get; set; }

    /// <summary>
    /// The name of the icon of the add button of the pivot from the built-in Fluent UI icons (default: Add).
    /// </summary>
    public string? AddIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the add button of the pivot (default: Add).
    /// </summary>
    public string? AddTitle { get; set; }

    /// <summary>
    /// Determines the alignment of the header section of the pivot.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Renders the next and previous buttons of the Slide overflow behavior only while there is something to slide to.
    /// </summary>
    public bool? AutoHideSlideButtons { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the pivot.
    /// </summary>
    public BitPivotClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the pivot.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Renders a dismiss button on every pivot item, which reports the item to dismiss through the OnItemDismiss callback.
    /// </summary>
    public bool? Dismissible { get; set; }

    /// <summary>
    /// The format of the aria-label of the dismiss button of the pivot items (default: "Remove {0}").
    /// </summary>
    public string? DismissAriaLabelFormat { get; set; }

    /// <summary>
    /// The icon of the dismiss button of the pivot items using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// The name of the icon of the dismiss button of the pivot items from the built-in Fluent UI icons (default: Cancel).
    /// </summary>
    public string? DismissIconName { get; set; }

    /// <summary>
    /// The title (tooltip) of the dismiss button of the pivot items (default: Remove).
    /// </summary>
    public string? DismissTitle { get; set; }

    /// <summary>
    /// Stretches the pivot items to share the whole width (or the whole height in a vertical pivot) of the header.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The gap between the pivot items of the header.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// Whether to skip rendering the tabpanel with the content of the selected tab.
    /// </summary>
    public bool? HeaderOnly { get; set; }

    /// <summary>
    /// The type of the pivot header items.
    /// </summary>
    public BitPivotHeaderType? HeaderType { get; set; }

    /// <summary>
    /// Keeps the content of every tab that has been shown at least once mounted.
    /// </summary>
    public bool? KeepMounted { get; set; }

    /// <summary>
    /// Wraps the keyboard navigation of the header around at both of its ends.
    /// </summary>
    public bool? Loop { get; set; }

    /// <summary>
    /// Mounts all tabs at render time and hides the non-selected ones instead of not rendering them.
    /// </summary>
    public bool? MountAll { get; set; }

    /// <summary>
    /// Enables the roving tabindex behavior, which turns the whole header into a single tab stop.
    /// </summary>
    public bool? Navigable { get; set; }

    /// <summary>
    /// The aria-label of the next button in the Slide overflow behavior (default: Next).
    /// </summary>
    public string? NextAriaLabel { get; set; }

    /// <summary>
    /// The icon of the next button in the Slide overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextIcon { get; set; }

    /// <summary>
    /// The name of the icon of the next button in the Slide overflow behavior from the built-in Fluent UI icons.
    /// </summary>
    public string? NextIconName { get; set; }

    /// <summary>
    /// The aria-label of the overflow menu button in the Menu overflow behavior (default: More).
    /// </summary>
    public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Overflow behavior when there is not enough room to display all of the tabs.
    /// </summary>
    public BitPivotOverflowBehavior? OverflowBehavior { get; set; }

    /// <summary>
    /// The icon of the overflow menu button in the Menu overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OverflowIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? OverflowIcon { get; set; }

    /// <summary>
    /// The name of the icon of the overflow menu button in the Menu overflow behavior from the built-in Fluent UI icons (default: More).
    /// </summary>
    public string? OverflowIconName { get; set; }

    /// <summary>
    /// Position of the pivot header.
    /// </summary>
    public BitPivotPosition? Position { get; set; }

    /// <summary>
    /// The aria-label of the previous button in the Slide overflow behavior (default: Previous).
    /// </summary>
    public string? PreviousAriaLabel { get; set; }

    /// <summary>
    /// The icon of the previous button in the Slide overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PreviousIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PreviousIcon { get; set; }

    /// <summary>
    /// The name of the icon of the previous button in the Slide overflow behavior from the built-in Fluent UI icons.
    /// </summary>
    public string? PreviousIconName { get; set; }

    /// <summary>
    /// Lets the pivot items be dragged onto one another, and the focused one be moved with the Ctrl+Arrow keys.
    /// </summary>
    public bool? Reorderable { get; set; }

    /// <summary>
    /// Selects the focused pivot item while the header is navigated with the keyboard.
    /// </summary>
    public bool? SelectOnFocus { get; set; }

    /// <summary>
    /// The size of the pivot header items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Stacks the icon of the pivot items on top of their text instead of putting the two side by side.
    /// </summary>
    public bool? Stacked { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the pivot.
    /// </summary>
    public BitPivotClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPivot"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPivot"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPivot"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPivot"/>.
    /// </remarks>
    /// <param name="bitPivot">
    /// The <see cref="BitPivot"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPivot bitPivot)
    {
        if (bitPivot is null) return;

        UpdateBaseParameters(bitPivot);

        if (Addable.HasValue && bitPivot.HasNotBeenSet(nameof(Addable)))
        {
            bitPivot.Addable = Addable.Value;
        }

        if (AddAriaLabel.HasValue() && bitPivot.HasNotBeenSet(nameof(AddAriaLabel)))
        {
            bitPivot.AddAriaLabel = AddAriaLabel;
        }

        if (AddIcon is not null && bitPivot.HasNotBeenSet(nameof(AddIcon)))
        {
            bitPivot.AddIcon = AddIcon;
        }

        if (AddIconName.HasValue() && bitPivot.HasNotBeenSet(nameof(AddIconName)))
        {
            bitPivot.AddIconName = AddIconName;
        }

        if (AddTitle.HasValue() && bitPivot.HasNotBeenSet(nameof(AddTitle)))
        {
            bitPivot.AddTitle = AddTitle;
        }

        if (Alignment.HasValue && bitPivot.HasNotBeenSet(nameof(Alignment)))
        {
            bitPivot.Alignment = Alignment.Value;

            bitPivot.StyleBuilder.Reset();
        }

        if (AutoHideSlideButtons.HasValue && bitPivot.HasNotBeenSet(nameof(AutoHideSlideButtons)))
        {
            bitPivot.AutoHideSlideButtons = AutoHideSlideButtons.Value;
        }

        if (Classes is not null && bitPivot.HasNotBeenSet(nameof(Classes)))
        {
            bitPivot.Classes = Classes;

            bitPivot.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitPivot.HasNotBeenSet(nameof(Color)))
        {
            bitPivot.Color = Color.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (Dismissible.HasValue && bitPivot.HasNotBeenSet(nameof(Dismissible)))
        {
            bitPivot.Dismissible = Dismissible.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (DismissAriaLabelFormat.HasValue() && bitPivot.HasNotBeenSet(nameof(DismissAriaLabelFormat)))
        {
            bitPivot.DismissAriaLabelFormat = DismissAriaLabelFormat;
        }

        if (DismissIcon is not null && bitPivot.HasNotBeenSet(nameof(DismissIcon)))
        {
            bitPivot.DismissIcon = DismissIcon;
        }

        if (DismissIconName.HasValue() && bitPivot.HasNotBeenSet(nameof(DismissIconName)))
        {
            bitPivot.DismissIconName = DismissIconName;
        }

        if (DismissTitle.HasValue() && bitPivot.HasNotBeenSet(nameof(DismissTitle)))
        {
            bitPivot.DismissTitle = DismissTitle;
        }

        if (FullWidth.HasValue && bitPivot.HasNotBeenSet(nameof(FullWidth)))
        {
            bitPivot.FullWidth = FullWidth.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (Gap.HasValue() && bitPivot.HasNotBeenSet(nameof(Gap)))
        {
            bitPivot.Gap = Gap;

            bitPivot.StyleBuilder.Reset();
        }

        if (HeaderOnly.HasValue && bitPivot.HasNotBeenSet(nameof(HeaderOnly)))
        {
            bitPivot.HeaderOnly = HeaderOnly.Value;
        }

        if (HeaderType.HasValue && bitPivot.HasNotBeenSet(nameof(HeaderType)))
        {
            bitPivot.HeaderType = HeaderType.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (KeepMounted.HasValue && bitPivot.HasNotBeenSet(nameof(KeepMounted)))
        {
            bitPivot.KeepMounted = KeepMounted.Value;
        }

        if (Loop.HasValue && bitPivot.HasNotBeenSet(nameof(Loop)))
        {
            bitPivot.Loop = Loop.Value;
        }

        if (MountAll.HasValue && bitPivot.HasNotBeenSet(nameof(MountAll)))
        {
            bitPivot.MountAll = MountAll.Value;
        }

        if (Navigable.HasValue && bitPivot.HasNotBeenSet(nameof(Navigable)))
        {
            bitPivot.Navigable = Navigable.Value;
        }

        if (NextAriaLabel.HasValue() && bitPivot.HasNotBeenSet(nameof(NextAriaLabel)))
        {
            bitPivot.NextAriaLabel = NextAriaLabel;
        }

        if (NextIcon is not null && bitPivot.HasNotBeenSet(nameof(NextIcon)))
        {
            bitPivot.NextIcon = NextIcon;
        }

        if (NextIconName.HasValue() && bitPivot.HasNotBeenSet(nameof(NextIconName)))
        {
            bitPivot.NextIconName = NextIconName;
        }

        if (OverflowAriaLabel.HasValue() && bitPivot.HasNotBeenSet(nameof(OverflowAriaLabel)))
        {
            bitPivot.OverflowAriaLabel = OverflowAriaLabel;
        }

        if (OverflowBehavior.HasValue && bitPivot.HasNotBeenSet(nameof(OverflowBehavior)))
        {
            bitPivot.OverflowBehavior = OverflowBehavior.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (OverflowIcon is not null && bitPivot.HasNotBeenSet(nameof(OverflowIcon)))
        {
            bitPivot.OverflowIcon = OverflowIcon;
        }

        if (OverflowIconName.HasValue() && bitPivot.HasNotBeenSet(nameof(OverflowIconName)))
        {
            bitPivot.OverflowIconName = OverflowIconName;
        }

        if (Position.HasValue && bitPivot.HasNotBeenSet(nameof(Position)))
        {
            bitPivot.Position = Position.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (PreviousAriaLabel.HasValue() && bitPivot.HasNotBeenSet(nameof(PreviousAriaLabel)))
        {
            bitPivot.PreviousAriaLabel = PreviousAriaLabel;
        }

        if (PreviousIcon is not null && bitPivot.HasNotBeenSet(nameof(PreviousIcon)))
        {
            bitPivot.PreviousIcon = PreviousIcon;
        }

        if (PreviousIconName.HasValue() && bitPivot.HasNotBeenSet(nameof(PreviousIconName)))
        {
            bitPivot.PreviousIconName = PreviousIconName;
        }

        if (Reorderable.HasValue && bitPivot.HasNotBeenSet(nameof(Reorderable)))
        {
            bitPivot.Reorderable = Reorderable.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (SelectOnFocus.HasValue && bitPivot.HasNotBeenSet(nameof(SelectOnFocus)))
        {
            bitPivot.SelectOnFocus = SelectOnFocus.Value;
        }

        if (Size.HasValue && bitPivot.HasNotBeenSet(nameof(Size)))
        {
            bitPivot.Size = Size.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (Stacked.HasValue && bitPivot.HasNotBeenSet(nameof(Stacked)))
        {
            bitPivot.Stacked = Stacked.Value;

            bitPivot.ClassBuilder.Reset();
        }

        if (Styles is not null && bitPivot.HasNotBeenSet(nameof(Styles)))
        {
            bitPivot.Styles = Styles;

            bitPivot.StyleBuilder.Reset();
        }
    }
}
