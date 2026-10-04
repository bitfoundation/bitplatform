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
    public BitLabelPosition? LabelPosition { get; set; }

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

        if (AllowDisabledFocus.HasValue && bitCheckbox.HasNotBeenSet(nameof(AllowDisabledFocus)))
        {
            bitCheckbox.AllowDisabledFocus = AllowDisabledFocus.Value;
        }

        if (AriaControls.HasValue() && bitCheckbox.HasNotBeenSet(nameof(AriaControls)))
        {
            bitCheckbox.AriaControls = AriaControls;
        }

        if (AriaDescribedby.HasValue() && bitCheckbox.HasNotBeenSet(nameof(AriaDescribedby)))
        {
            bitCheckbox.AriaDescribedby = AriaDescribedby;
        }

        if (AriaDescription.HasValue() && bitCheckbox.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitCheckbox.AriaDescription = AriaDescription;
        }

        if (AriaLabelledby.HasValue() && bitCheckbox.HasNotBeenSet(nameof(AriaLabelledby)))
        {
            bitCheckbox.AriaLabelledby = AriaLabelledby;
        }

        if (AriaPositionInSet.HasValue && bitCheckbox.HasNotBeenSet(nameof(AriaPositionInSet)))
        {
            bitCheckbox.AriaPositionInSet = AriaPositionInSet.Value;
        }

        if (AriaSetSize.HasValue && bitCheckbox.HasNotBeenSet(nameof(AriaSetSize)))
        {
            bitCheckbox.AriaSetSize = AriaSetSize.Value;
        }

        if (AutoFocus.HasValue && bitCheckbox.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitCheckbox.AutoFocus = AutoFocus.Value;
        }

        if (AutoLoading.HasValue && bitCheckbox.HasNotBeenSet(nameof(AutoLoading)))
        {
            bitCheckbox.AutoLoading = AutoLoading.Value;
        }

        if (CheckIcon is not null && bitCheckbox.HasNotBeenSet(nameof(CheckIcon)))
        {
            bitCheckbox.CheckIcon = CheckIcon;
        }

        if (CheckIconAriaLabel.HasValue() && bitCheckbox.HasNotBeenSet(nameof(CheckIconAriaLabel)))
        {
            bitCheckbox.CheckIconAriaLabel = CheckIconAriaLabel;
        }

        if (CheckIconName.HasValue() && bitCheckbox.HasNotBeenSet(nameof(CheckIconName)))
        {
            bitCheckbox.CheckIconName = CheckIconName;
        }

        if (Classes is not null && bitCheckbox.HasNotBeenSet(nameof(Classes)))
        {
            bitCheckbox.Classes = Classes;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitCheckbox.HasNotBeenSet(nameof(Color)))
        {
            bitCheckbox.Color = Color.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (DefaultIndeterminate.HasValue && bitCheckbox.HasNotBeenSet(nameof(DefaultIndeterminate)))
        {
            bitCheckbox.DefaultIndeterminate = DefaultIndeterminate.Value;
        }

        if (Description.HasValue() && bitCheckbox.HasNotBeenSet(nameof(Description)))
        {
            bitCheckbox.Description = Description;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitCheckbox.HasNotBeenSet(nameof(FullWidth)))
        {
            bitCheckbox.FullWidth = FullWidth.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (Indeterminate.HasValue && bitCheckbox.HasNotBeenSet(nameof(Indeterminate)))
        {
            var indeterminateChanged = bitCheckbox.Indeterminate != Indeterminate.Value;

            bitCheckbox.Indeterminate = Indeterminate.Value;

            bitCheckbox.ClassBuilder.Reset();

            // The mixed state lives in a DOM property rather than an attribute, so it has to be pushed onto the
            // element the way the setter of a parameter written on the component itself does. Only once there is
            // an element to push it onto: the state the checkbox starts out with is pushed on its first render.
            if (indeterminateChanged) bitCheckbox.OnSetIndeterminateFromParams();
        }

        if (IndeterminateIcon is not null && bitCheckbox.HasNotBeenSet(nameof(IndeterminateIcon)))
        {
            bitCheckbox.IndeterminateIcon = IndeterminateIcon;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (IndeterminateIconName.HasValue() && bitCheckbox.HasNotBeenSet(nameof(IndeterminateIconName)))
        {
            bitCheckbox.IndeterminateIconName = IndeterminateIconName;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (Label.HasValue() && bitCheckbox.HasNotBeenSet(nameof(Label)))
        {
            bitCheckbox.Label = Label;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (LabelPosition.HasValue && bitCheckbox.HasNotBeenSet(nameof(LabelPosition)))
        {
            bitCheckbox.LabelPosition = LabelPosition.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (Loading.HasValue && bitCheckbox.HasNotBeenSet(nameof(Loading)))
        {
            bitCheckbox.Loading = Loading.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (NoWrap.HasValue && bitCheckbox.HasNotBeenSet(nameof(NoWrap)))
        {
            bitCheckbox.NoWrap = NoWrap.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (Reversed.HasValue && bitCheckbox.HasNotBeenSet(nameof(Reversed)))
        {
            bitCheckbox.Reversed = Reversed.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitCheckbox.HasNotBeenSet(nameof(Size)))
        {
            bitCheckbox.Size = Size.Value;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitCheckbox.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitCheckbox.StopPropagation = StopPropagation.Value;
        }

        if (Styles is not null && bitCheckbox.HasNotBeenSet(nameof(Styles)))
        {
            bitCheckbox.Styles = Styles;

            bitCheckbox.StyleBuilder.Reset();
        }

        if (ThreeState.HasValue && bitCheckbox.HasNotBeenSet(nameof(ThreeState)))
        {
            bitCheckbox.ThreeState = ThreeState.Value;
        }

        if (Title.HasValue() && bitCheckbox.HasNotBeenSet(nameof(Title)))
        {
            bitCheckbox.Title = Title;
        }

        if (UncheckedIcon is not null && bitCheckbox.HasNotBeenSet(nameof(UncheckedIcon)))
        {
            bitCheckbox.UncheckedIcon = UncheckedIcon;

            bitCheckbox.ClassBuilder.Reset();
        }

        if (UncheckedIconName.HasValue() && bitCheckbox.HasNotBeenSet(nameof(UncheckedIconName)))
        {
            bitCheckbox.UncheckedIconName = UncheckedIconName;

            bitCheckbox.ClassBuilder.Reset();
        }
    }
}
