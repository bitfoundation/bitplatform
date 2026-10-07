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
    /// Placement of the pivot header.
    /// </summary>
    public BitPlacement? Placement { get; set; }

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

        if (Addable.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Addable), Addable.Value, static p => p.Addable, static (p, v) => p.Addable = v);
        }

        if (AddAriaLabel.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(AddAriaLabel), AddAriaLabel, static p => p.AddAriaLabel, static (p, v) => p.AddAriaLabel = v);
        }

        if (AddIcon is not null)
        {
            bitPivot.TakeFromCascade(nameof(AddIcon), AddIcon, static p => p.AddIcon, static (p, v) => p.AddIcon = v);
        }

        if (AddIconName.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(AddIconName), AddIconName, static p => p.AddIconName, static (p, v) => p.AddIconName = v);
        }

        if (AddTitle.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(AddTitle), AddTitle, static p => p.AddTitle, static (p, v) => p.AddTitle = v);
        }

        if (Alignment.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Alignment), Alignment.Value, static p => p.Alignment, static (p, v) => p.Alignment = v);
        }

        if (AutoHideSlideButtons.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(AutoHideSlideButtons), AutoHideSlideButtons.Value, static p => p.AutoHideSlideButtons, static (p, v) => p.AutoHideSlideButtons = v);
        }

        if (Classes is not null)
        {
            bitPivot.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (Color.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Color), Color.Value, static p => p.Color, static (p, v) => p.Color = v);
        }

        if (Dismissible.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Dismissible), Dismissible.Value, static p => p.Dismissible, static (p, v) => p.Dismissible = v);
        }

        if (DismissAriaLabelFormat.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(DismissAriaLabelFormat), DismissAriaLabelFormat, static p => p.DismissAriaLabelFormat, static (p, v) => p.DismissAriaLabelFormat = v);
        }

        if (DismissIcon is not null)
        {
            bitPivot.TakeFromCascade(nameof(DismissIcon), DismissIcon, static p => p.DismissIcon, static (p, v) => p.DismissIcon = v);
        }

        if (DismissIconName.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(DismissIconName), DismissIconName, static p => p.DismissIconName, static (p, v) => p.DismissIconName = v);
        }

        if (DismissTitle.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(DismissTitle), DismissTitle, static p => p.DismissTitle, static (p, v) => p.DismissTitle = v);
        }

        if (FullWidth.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static p => p.FullWidth, static (p, v) => p.FullWidth = v);
        }

        if (Gap.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(Gap), Gap, static p => p.Gap, static (p, v) => p.Gap = v);
        }

        if (HeaderOnly.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(HeaderOnly), HeaderOnly.Value, static p => p.HeaderOnly, static (p, v) => p.HeaderOnly = v);
        }

        if (HeaderType.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(HeaderType), HeaderType.Value, static p => p.HeaderType, static (p, v) => p.HeaderType = v);
        }

        if (KeepMounted.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(KeepMounted), KeepMounted.Value, static p => p.KeepMounted, static (p, v) => p.KeepMounted = v);
        }

        if (Loop.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Loop), Loop.Value, static p => p.Loop, static (p, v) => p.Loop = v);
        }

        if (MountAll.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(MountAll), MountAll.Value, static p => p.MountAll, static (p, v) => p.MountAll = v);
        }

        if (Navigable.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Navigable), Navigable.Value, static p => p.Navigable, static (p, v) => p.Navigable = v);
        }

        if (NextAriaLabel.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(NextAriaLabel), NextAriaLabel, static p => p.NextAriaLabel, static (p, v) => p.NextAriaLabel = v);
        }

        if (NextIcon is not null)
        {
            bitPivot.TakeFromCascade(nameof(NextIcon), NextIcon, static p => p.NextIcon, static (p, v) => p.NextIcon = v);
        }

        if (NextIconName.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(NextIconName), NextIconName, static p => p.NextIconName, static (p, v) => p.NextIconName = v);
        }

        if (OverflowAriaLabel.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(OverflowAriaLabel), OverflowAriaLabel, static p => p.OverflowAriaLabel, static (p, v) => p.OverflowAriaLabel = v);
        }

        if (OverflowBehavior.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(OverflowBehavior), OverflowBehavior.Value, static p => p.OverflowBehavior, static (p, v) => p.OverflowBehavior = v);
        }

        if (OverflowIcon is not null)
        {
            bitPivot.TakeFromCascade(nameof(OverflowIcon), OverflowIcon, static p => p.OverflowIcon, static (p, v) => p.OverflowIcon = v);
        }

        if (OverflowIconName.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(OverflowIconName), OverflowIconName, static p => p.OverflowIconName, static (p, v) => p.OverflowIconName = v);
        }

        if (Placement.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Placement), Placement.Value, static p => p.Placement, static (p, v) => p.Placement = v);
        }

        if (PreviousAriaLabel.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(PreviousAriaLabel), PreviousAriaLabel, static p => p.PreviousAriaLabel, static (p, v) => p.PreviousAriaLabel = v);
        }

        if (PreviousIcon is not null)
        {
            bitPivot.TakeFromCascade(nameof(PreviousIcon), PreviousIcon, static p => p.PreviousIcon, static (p, v) => p.PreviousIcon = v);
        }

        if (PreviousIconName.HasValue())
        {
            bitPivot.TakeFromCascade(nameof(PreviousIconName), PreviousIconName, static p => p.PreviousIconName, static (p, v) => p.PreviousIconName = v);
        }

        if (Reorderable.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Reorderable), Reorderable.Value, static p => p.Reorderable, static (p, v) => p.Reorderable = v);
        }

        if (SelectOnFocus.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(SelectOnFocus), SelectOnFocus.Value, static p => p.SelectOnFocus, static (p, v) => p.SelectOnFocus = v);
        }

        if (Size.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Stacked.HasValue)
        {
            bitPivot.TakeFromCascade(nameof(Stacked), Stacked.Value, static p => p.Stacked, static (p, v) => p.Stacked = v);
        }

        if (Styles is not null)
        {
            bitPivot.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }
    }
}
