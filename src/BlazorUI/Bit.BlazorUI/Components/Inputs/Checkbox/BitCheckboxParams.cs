namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCheckbox"/> component.
/// </summary>
public class BitCheckboxParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCheckbox"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCheckbox value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCheckbox)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps the disabled checkbox focusable and discoverable by assistive technologies.
    /// When enabled, the disabled state is conveyed using the <c>aria-disabled</c> attribute instead of the
    /// native <c>disabled</c> attribute, so the checkbox remains in the tab order while its toggling is suppressed.
    /// </summary>
    public bool? AllowDisabledFocus { get; set; }

    /// <summary>
    /// The id of the element the checkbox controls, rendered as <c>aria-controls</c> on the checkbox input.
    /// </summary>
    public string? AriaControls { get; set; }

    /// <summary>
    /// The ids of the elements that describe the checkbox, rendered into <c>aria-describedby</c> beside the
    /// ids the component contributes itself.
    /// </summary>
    public string? AriaDescribedby { get; set; }

    /// <summary>
    /// Detailed description of the checkbox for the benefit of screen readers, rendered as a visually
    /// hidden element that the checkbox input points to via <c>aria-describedby</c>.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// ID for element that contains label information for the checkbox.
    /// </summary>
    public string? AriaLabelledby { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set) for aria-posinset.
    /// </summary>
    public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set) for aria-setsize.
    /// </summary>
    public int? AriaSetSize { get; set; }

    /// <summary>
    /// Moves the focus onto the checkbox when it first renders.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Turns the checkbox busy by itself for as long as the callbacks behind a change are still running.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// Gets or sets the check icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CheckIcon { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    public string? CheckIconAriaLabel { get; set; }

    /// <summary>
    /// The name of the built-in icon to render as the check mark inside the checkbox.
    /// </summary>
    public string? CheckIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the checkbox.
    /// </summary>
    public BitCheckboxClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the checkbox.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default indeterminate visual state for the checkbox.
    /// </summary>
    public bool? DefaultIndeterminate { get; set; }

    /// <summary>
    /// A visible explanation of what checking the box means, rendered on a line of its own under it and
    /// announced after the name of the checkbox through <c>aria-describedby</c>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Stretches the checkbox across the full available width, pushing the box and the label to opposite edges.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// An indeterminate visual state for the checkbox.
    /// The indeterminate state takes visual precedence over the checked state but does not affect the Value.
    /// </summary>
    public bool? Indeterminate { get; set; }

    /// <summary>
    /// Gets or sets the icon to render in the indeterminate state using custom CSS classes for external icon libraries,
    /// replacing the default filled square. Takes precedence over <see cref="IndeterminateIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? IndeterminateIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render in the indeterminate state, replacing the default filled square.
    /// </summary>
    public string? IndeterminateIconName { get; set; }

    /// <summary>
    /// Descriptive label for the checkbox.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the checkbox box.
    /// Takes precedence over <see cref="Reversed"/> when both are set.
    /// </summary>
    public BitPlacement? LabelPlacement { get; set; }

    /// <summary>
    /// Turns the checkbox busy while the change it has just accepted is still being carried out.
    /// </summary>
    public bool? Loading { get; set; }

    /// <summary>
    /// Keeps the label of the checkbox on a single line and ends it with an ellipsis where it does not fit.
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// Reverses the label and checkbox location.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The size of the checkbox.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the checkbox.
    /// </summary>
    public BitCheckboxClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables cycling through the unchecked, checked and indeterminate states on each click,
    /// instead of the indeterminate state being reachable only programmatically.
    /// </summary>
    public bool? ThreeState { get; set; }

    /// <summary>
    /// Title text applied to the label container of the checkbox.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the icon to render in the unchecked state using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UncheckedIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? UncheckedIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render in the unchecked state.
    /// By default the unchecked box is empty and previews the check icon on hover.
    /// </summary>
    public string? UncheckedIconName { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCheckbox"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCheckbox"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCheckbox"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCheckbox"/>.
    /// </remarks>
    /// <param name="bitCheckbox">
    /// The <see cref="BitCheckbox"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCheckbox bitCheckbox)
    {
        if (bitCheckbox is null) return;

        UpdateBaseParameters(bitCheckbox);

        if (AllowDisabledFocus.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(AllowDisabledFocus), AllowDisabledFocus.Value, static c => c.AllowDisabledFocus, static (c, v) => c.AllowDisabledFocus = v);
        }

        if (AriaControls.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(AriaControls), AriaControls, static c => c.AriaControls, static (c, v) => c.AriaControls = v);
        }

        if (AriaDescribedby.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(AriaDescribedby), AriaDescribedby, static c => c.AriaDescribedby, static (c, v) => c.AriaDescribedby = v);
        }

        if (AriaDescription.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(AriaDescription), AriaDescription, static c => c.AriaDescription, static (c, v) => c.AriaDescription = v);
        }

        if (AriaLabelledby.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(AriaLabelledby), AriaLabelledby, static c => c.AriaLabelledby, static (c, v) => c.AriaLabelledby = v);
        }

        if (AriaPositionInSet.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(AriaPositionInSet), AriaPositionInSet.Value, static c => c.AriaPositionInSet, static (c, v) => c.AriaPositionInSet = v);
        }

        if (AriaSetSize.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(AriaSetSize), AriaSetSize.Value, static c => c.AriaSetSize, static (c, v) => c.AriaSetSize = v);
        }

        if (AutoFocus.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static c => c.AutoFocus, static (c, v) => c.AutoFocus = v);
        }

        if (AutoLoading.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(AutoLoading), AutoLoading.Value, static c => c.AutoLoading, static (c, v) => c.AutoLoading = v);
        }

        var ownCheckIcon = bitCheckbox.HasSetAnyOf(nameof(CheckIcon), nameof(CheckIconName));

        if (CheckIcon is not null)
        {
            bitCheckbox.TakeFromCascade(nameof(CheckIcon), CheckIcon, static c => c.CheckIcon, static (c, v) => c.CheckIcon = v, outranked: ownCheckIcon);
        }

        if (CheckIconAriaLabel.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(CheckIconAriaLabel), CheckIconAriaLabel, static c => c.CheckIconAriaLabel, static (c, v) => c.CheckIconAriaLabel = v);
        }

        if (CheckIconName.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(CheckIconName), CheckIconName, static c => c.CheckIconName, static (c, v) => c.CheckIconName = v, outranked: ownCheckIcon);
        }

        if (Classes is not null)
        {
            bitCheckbox.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Color.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(Color), Color.Value, static c => c.Color, static (c, v) => c.Color = v);
        }

        if (DefaultIndeterminate.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(DefaultIndeterminate), DefaultIndeterminate.Value, static c => c.DefaultIndeterminate, static (c, v) => c.DefaultIndeterminate = v);
        }

        if (Description.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(Description), Description, static c => c.Description, static (c, v) => c.Description = v);
        }

        if (FullWidth.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static c => c.FullWidth, static (c, v) => c.FullWidth = v);
        }

        if (Indeterminate.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(Indeterminate), Indeterminate.Value, static c => c.Indeterminate, static (c, v) => c.Indeterminate = v);
        }

        var ownIndeterminateIcon = bitCheckbox.HasSetAnyOf(nameof(IndeterminateIcon), nameof(IndeterminateIconName));

        if (IndeterminateIcon is not null)
        {
            bitCheckbox.TakeFromCascade(nameof(IndeterminateIcon), IndeterminateIcon, static c => c.IndeterminateIcon, static (c, v) => c.IndeterminateIcon = v, outranked: ownIndeterminateIcon);
        }

        if (IndeterminateIconName.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(IndeterminateIconName), IndeterminateIconName, static c => c.IndeterminateIconName, static (c, v) => c.IndeterminateIconName = v, outranked: ownIndeterminateIcon);
        }

        if (Label.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(Label), Label, static c => c.Label, static (c, v) => c.Label = v);
        }

        if (LabelPlacement.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(LabelPlacement), LabelPlacement.Value, static c => c.LabelPlacement, static (c, v) => c.LabelPlacement = v);
        }

        if (Loading.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(Loading), Loading.Value, static c => c.Loading, static (c, v) => c.Loading = v);
        }

        if (NoWrap.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static c => c.NoWrap, static (c, v) => c.NoWrap = v);
        }

        if (Reversed.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(Reversed), Reversed.Value, static c => c.Reversed, static (c, v) => c.Reversed = v);
        }

        if (Size.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (StopPropagation.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static c => c.StopPropagation, static (c, v) => c.StopPropagation = v);
        }

        if (Styles is not null)
        {
            bitCheckbox.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (ThreeState.HasValue)
        {
            bitCheckbox.TakeFromCascade(nameof(ThreeState), ThreeState.Value, static c => c.ThreeState, static (c, v) => c.ThreeState = v);
        }

        if (Title.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(Title), Title, static c => c.Title, static (c, v) => c.Title = v);
        }

        var ownUncheckedIcon = bitCheckbox.HasSetAnyOf(nameof(UncheckedIcon), nameof(UncheckedIconName));

        if (UncheckedIcon is not null)
        {
            bitCheckbox.TakeFromCascade(nameof(UncheckedIcon), UncheckedIcon, static c => c.UncheckedIcon, static (c, v) => c.UncheckedIcon = v, outranked: ownUncheckedIcon);
        }

        if (UncheckedIconName.HasValue())
        {
            bitCheckbox.TakeFromCascade(nameof(UncheckedIconName), UncheckedIconName, static c => c.UncheckedIconName, static (c, v) => c.UncheckedIconName = v, outranked: ownUncheckedIcon);
        }
    }
}
