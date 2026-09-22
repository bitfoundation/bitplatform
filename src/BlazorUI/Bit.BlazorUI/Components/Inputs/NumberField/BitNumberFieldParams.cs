namespace Bit.BlazorUI;

/// <summary>
/// The parameters for the <see cref="BitNumberField{TValue}"/> component.
/// </summary>
/// <remarks>
/// Only the parameters declared by the number field itself (and the ones of <see cref="BitComponentBase"/>)
/// are carried. What belongs to one field alone - its <c>Label</c>, its value, its templates and its event
/// callbacks - stays on the markup of that field.
/// </remarks>
public class BitNumberFieldParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitNumberField{TValue}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitNumberField value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitNumberField";



    public string Name => ParamName;



    /// <summary>
    /// The general color of the number field, used for its focus indicator and for the icon,
    /// prefix and suffix while the field is focused (Primary by default).
    /// </summary>
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Detailed description of the input for the benefit of screen readers.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set).
    /// </summary>
    public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set).
    /// </summary>
    public int? AriaSetSize { get; set; }

    /// <summary>
    /// Sets the control's aria-valuetext.
    /// </summary>
    public string? AriaValueText { get; set; }

    /// <summary>
    /// The color kind of the number field background (Primary by default).
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the number field border (Primary by default).
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitNumberField.
    /// </summary>
    public BitNumberFieldClassStyles? Classes { get; set; }

    /// <summary>
    /// Accessible label text for the clear button (for screen reader users), useful for localization.
    /// </summary>
    public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear button, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the icon for the clear button from the built-in Fluent UI icons.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The delay in milliseconds before the value starts changing continuously while an
    /// increment/decrement button is held down.
    /// </summary>
    public int? ContinuousSpinDelay { get; set; }

    /// <summary>
    /// The interval in milliseconds between two consecutive value changes while an
    /// increment/decrement button is held down.
    /// </summary>
    public int? ContinuousSpinInterval { get; set; }

    /// <summary>
    /// Accessible label text for the decrement button (for screen reader users).
    /// </summary>
    public string? DecrementAriaLabel { get; set; }

    /// <summary>
    /// The icon of the decrement button, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? DecrementIcon { get; set; }

    /// <summary>
    /// The name of the icon for the decrement button from the built-in Fluent UI icons.
    /// </summary>
    public string? DecrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the decrement button.
    /// </summary>
    public string? DecrementTitle { get; set; }

    /// <summary>
    /// A hint rendered under the field, describing what is expected of it (e.g. the accepted range or
    /// the unit), which the input references through its aria-describedby attribute.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A custom function to normalize the raw input string before it gets parsed into the value.
    /// </summary>
    public Func<string?, string?>? DigitsNormalizer { get; set; }

    /// <summary>
    /// Stretches the number field to the full width of its container.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Hides the text input element while keeping the increment/decrement buttons functional,
    /// turning the component into a stepper-only control.
    /// </summary>
    public bool? HideInput { get; set; }

    /// <summary>
    /// The icon to display alongside the number field, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    public string? IconAriaLabel { get; set; }

    /// <summary>
    /// The name of the icon to display alongside the number field, from the built-in Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users).
    /// </summary>
    public string? IncrementAriaLabel { get; set; }

    /// <summary>
    /// The icon of the increment button, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? IncrementIcon { get; set; }

    /// <summary>
    /// The name of the icon for the increment button from the built-in Fluent UI icons.
    /// </summary>
    public string? IncrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the increment button.
    /// </summary>
    public string? IncrementTitle { get; set; }

    /// <summary>
    /// Overrides the virtual keyboard the browser shows for the input.
    /// </summary>
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Reverses the direction of the value change when the user spins the value using the mouse wheel.
    /// </summary>
    public bool? InvertMouseWheel { get; set; }

    /// <summary>
    /// Makes only the text input part read-only, preventing typing, while the value can still be
    /// changed using the increment/decrement buttons, the arrow keys and the mouse wheel.
    /// </summary>
    public bool? IsInputReadOnly { get; set; }

    /// <summary>
    /// The position of the label in regards to the field (Top by default).
    /// </summary>
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// What a screen reader announces while the field shows its busy indicator, in place of the
    /// default "Loading".
    /// </summary>
    public string? LoadingAriaLabel { get; set; }

    /// <summary>
    /// The maximum value of the number field.
    /// </summary>
    public string? Max { get; set; }

    /// <summary>
    /// The minimum value of the number field.
    /// </summary>
    public string? Min { get; set; }

    /// <summary>
    /// Determines how the increment/decrement buttons render.
    /// </summary>
    public BitSpinButtonMode? Mode { get; set; }

    /// <summary>
    /// Removes the border of the number field.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Keeps values typed outside of the Min/Max range intact instead of clamping them to the nearest bound.
    /// </summary>
    public bool? NoClamp { get; set; }

    /// <summary>
    /// Disables changing the value using the mouse wheel entirely.
    /// </summary>
    public bool? NoMouseWheel { get; set; }

    /// <summary>
    /// Disables the automatic select-all of the input's text when the field receives focus.
    /// </summary>
    public bool? NoSelectOnFocus { get; set; }

    /// <summary>
    /// Normalizes non-Latin decimal digits to their Latin (0-9) equivalents before parsing.
    /// </summary>
    public bool? NormalizeDigits { get; set; }

    /// <summary>
    /// The format of the number in the number field, using the standard or custom .NET numeric format strings.
    /// </summary>
    public string? NumberFormat { get; set; }

    /// <summary>
    /// The amount by which the value changes when the user presses the PageUp/PageDown keys.
    /// </summary>
    public string? PageStep { get; set; }

    /// <summary>
    /// The message format used for invalid values entered in the input.
    /// </summary>
    public string? ParsingErrorMessage { get; set; }

    /// <summary>
    /// Input placeholder text.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// How many decimal places the value should be rounded to.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Prefix displayed before the numeric field contents.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Whether to show the clear button whenever the field is showing something.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Sets the preset size (Small, Medium, Large) of the number field.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Snaps the committed value to the nearest multiple of the Step.
    /// </summary>
    public bool? SnapToStep { get; set; }

    /// <summary>
    /// The difference between two adjacent values of the number field.
    /// </summary>
    public string? Step { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitNumberField.
    /// </summary>
    public BitNumberFieldClassStyles? Styles { get; set; }

    /// <summary>
    /// Suffix displayed after the numeric field contents.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// A more descriptive title for the control, visible on its tooltip.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Renders the number field with a single bottom rule instead of a full border.
    /// </summary>
    public bool? Underlined { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitNumberField{TValue}"/> instance with any values that have been
    /// set on this object, if those properties have not already been set on the <see cref="BitNumberField{TValue}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitNumberField"/> will be
    /// updated. This method does not overwrite existing values on <paramref name="bitNumberField"/>.
    /// </remarks>
    /// <param name="bitNumberField">
    /// The <see cref="BitNumberField{TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TValue>(BitNumberField<TValue> bitNumberField)
    {
        if (bitNumberField is null) return;

        UpdateBaseParameters(bitNumberField);

        if (Accent.HasValue && bitNumberField.HasNotBeenSet(nameof(Accent)))
        {
            bitNumberField.Accent = Accent.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (AriaDescription.HasValue() && bitNumberField.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitNumberField.AriaDescription = AriaDescription;
        }

        if (AriaPositionInSet.HasValue && bitNumberField.HasNotBeenSet(nameof(AriaPositionInSet)))
        {
            bitNumberField.AriaPositionInSet = AriaPositionInSet.Value;
        }

        if (AriaSetSize.HasValue && bitNumberField.HasNotBeenSet(nameof(AriaSetSize)))
        {
            bitNumberField.AriaSetSize = AriaSetSize.Value;
        }

        if (AriaValueText.HasValue() && bitNumberField.HasNotBeenSet(nameof(AriaValueText)))
        {
            bitNumberField.AriaValueText = AriaValueText;
        }

        if (Background.HasValue && bitNumberField.HasNotBeenSet(nameof(Background)))
        {
            bitNumberField.Background = Background.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (Border.HasValue && bitNumberField.HasNotBeenSet(nameof(Border)))
        {
            bitNumberField.Border = Border.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (Classes is not null && bitNumberField.HasNotBeenSet(nameof(Classes)))
        {
            bitNumberField.Classes = Classes;

            bitNumberField.ClassBuilder.Reset();
        }

        if (ClearButtonAriaLabel.HasValue() && bitNumberField.HasNotBeenSet(nameof(ClearButtonAriaLabel)))
        {
            bitNumberField.ClearButtonAriaLabel = ClearButtonAriaLabel;
        }

        if (ClearButtonIcon is not null && bitNumberField.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitNumberField.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitNumberField.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitNumberField.ClearButtonIconName = ClearButtonIconName;
        }

        if (ContinuousSpinDelay.HasValue && bitNumberField.HasNotBeenSet(nameof(ContinuousSpinDelay)))
        {
            bitNumberField.ContinuousSpinDelay = ContinuousSpinDelay.Value;
        }

        if (ContinuousSpinInterval.HasValue && bitNumberField.HasNotBeenSet(nameof(ContinuousSpinInterval)))
        {
            bitNumberField.ContinuousSpinInterval = ContinuousSpinInterval.Value;
        }

        if (DecrementAriaLabel.HasValue() && bitNumberField.HasNotBeenSet(nameof(DecrementAriaLabel)))
        {
            bitNumberField.DecrementAriaLabel = DecrementAriaLabel;
        }

        if (DecrementIcon is not null && bitNumberField.HasNotBeenSet(nameof(DecrementIcon)))
        {
            bitNumberField.DecrementIcon = DecrementIcon;
        }

        if (DecrementIconName.HasValue() && bitNumberField.HasNotBeenSet(nameof(DecrementIconName)))
        {
            bitNumberField.DecrementIconName = DecrementIconName;
        }

        if (DecrementTitle.HasValue() && bitNumberField.HasNotBeenSet(nameof(DecrementTitle)))
        {
            bitNumberField.DecrementTitle = DecrementTitle;
        }

        if (Description.HasValue() && bitNumberField.HasNotBeenSet(nameof(Description)))
        {
            bitNumberField.Description = Description;

            bitNumberField.ClassBuilder.Reset();
        }

        // The digit normalization is also applied to the Min/Max/Step/PageStep strings, so a normalizer
        // arriving from the cascade has to be in place before those are parsed at the end of this method.
        if (DigitsNormalizer is not null && bitNumberField.HasNotBeenSet(nameof(DigitsNormalizer)))
        {
            bitNumberField.DigitsNormalizer = DigitsNormalizer;
        }

        if (FullWidth.HasValue && bitNumberField.HasNotBeenSet(nameof(FullWidth)))
        {
            bitNumberField.FullWidth = FullWidth.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (HideInput.HasValue && bitNumberField.HasNotBeenSet(nameof(HideInput)))
        {
            bitNumberField.HideInput = HideInput.Value;
        }

        if (Icon is not null && bitNumberField.HasNotBeenSet(nameof(Icon)))
        {
            bitNumberField.Icon = Icon;
        }

        if (IconAriaLabel.HasValue() && bitNumberField.HasNotBeenSet(nameof(IconAriaLabel)))
        {
            bitNumberField.IconAriaLabel = IconAriaLabel;
        }

        if (IconName.HasValue() && bitNumberField.HasNotBeenSet(nameof(IconName)))
        {
            bitNumberField.IconName = IconName;
        }

        if (IncrementAriaLabel.HasValue() && bitNumberField.HasNotBeenSet(nameof(IncrementAriaLabel)))
        {
            bitNumberField.IncrementAriaLabel = IncrementAriaLabel;
        }

        if (IncrementIcon is not null && bitNumberField.HasNotBeenSet(nameof(IncrementIcon)))
        {
            bitNumberField.IncrementIcon = IncrementIcon;
        }

        if (IncrementIconName.HasValue() && bitNumberField.HasNotBeenSet(nameof(IncrementIconName)))
        {
            bitNumberField.IncrementIconName = IncrementIconName;
        }

        if (IncrementTitle.HasValue() && bitNumberField.HasNotBeenSet(nameof(IncrementTitle)))
        {
            bitNumberField.IncrementTitle = IncrementTitle;
        }

        if (InputMode.HasValue && bitNumberField.HasNotBeenSet(nameof(InputMode)))
        {
            bitNumberField.InputMode = InputMode.Value;

            bitNumberField.OnSetInputMode();
        }

        if (InvertMouseWheel.HasValue && bitNumberField.HasNotBeenSet(nameof(InvertMouseWheel)))
        {
            bitNumberField.InvertMouseWheel = InvertMouseWheel.Value;
        }

        if (IsInputReadOnly.HasValue && bitNumberField.HasNotBeenSet(nameof(IsInputReadOnly)))
        {
            bitNumberField.IsInputReadOnly = IsInputReadOnly.Value;
        }

        if (LabelPosition.HasValue && bitNumberField.HasNotBeenSet(nameof(LabelPosition)))
        {
            bitNumberField.LabelPosition = LabelPosition.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (LoadingAriaLabel.HasValue() && bitNumberField.HasNotBeenSet(nameof(LoadingAriaLabel)))
        {
            bitNumberField.LoadingAriaLabel = LoadingAriaLabel;
        }

        if (Mode.HasValue && bitNumberField.HasNotBeenSet(nameof(Mode)))
        {
            bitNumberField.Mode = Mode.Value;
        }

        if (NoBorder.HasValue && bitNumberField.HasNotBeenSet(nameof(NoBorder)))
        {
            bitNumberField.NoBorder = NoBorder.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (NoClamp.HasValue && bitNumberField.HasNotBeenSet(nameof(NoClamp)))
        {
            bitNumberField.NoClamp = NoClamp.Value;
        }

        if (NoMouseWheel.HasValue && bitNumberField.HasNotBeenSet(nameof(NoMouseWheel)))
        {
            bitNumberField.NoMouseWheel = NoMouseWheel.Value;
        }

        if (NoSelectOnFocus.HasValue && bitNumberField.HasNotBeenSet(nameof(NoSelectOnFocus)))
        {
            bitNumberField.NoSelectOnFocus = NoSelectOnFocus.Value;
        }

        if (NormalizeDigits.HasValue && bitNumberField.HasNotBeenSet(nameof(NormalizeDigits)))
        {
            bitNumberField.NormalizeDigits = NormalizeDigits.Value;
        }

        if (NumberFormat.HasValue() && bitNumberField.HasNotBeenSet(nameof(NumberFormat)))
        {
            bitNumberField.NumberFormat = NumberFormat;
        }

        if (ParsingErrorMessage.HasValue() && bitNumberField.HasNotBeenSet(nameof(ParsingErrorMessage)))
        {
            bitNumberField.ParsingErrorMessage = ParsingErrorMessage!;
        }

        if (Placeholder.HasValue() && bitNumberField.HasNotBeenSet(nameof(Placeholder)))
        {
            bitNumberField.Placeholder = Placeholder;
        }

        if (Prefix.HasValue() && bitNumberField.HasNotBeenSet(nameof(Prefix)))
        {
            bitNumberField.Prefix = Prefix;
        }

        if (ShowClearButton.HasValue && bitNumberField.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitNumberField.ShowClearButton = ShowClearButton.Value;
        }

        if (Size.HasValue && bitNumberField.HasNotBeenSet(nameof(Size)))
        {
            bitNumberField.Size = Size.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        if (SnapToStep.HasValue && bitNumberField.HasNotBeenSet(nameof(SnapToStep)))
        {
            bitNumberField.SnapToStep = SnapToStep.Value;
        }

        if (Styles is not null && bitNumberField.HasNotBeenSet(nameof(Styles)))
        {
            bitNumberField.Styles = Styles;

            bitNumberField.StyleBuilder.Reset();
        }

        if (Suffix.HasValue() && bitNumberField.HasNotBeenSet(nameof(Suffix)))
        {
            bitNumberField.Suffix = Suffix;
        }

        if (Title.HasValue() && bitNumberField.HasNotBeenSet(nameof(Title)))
        {
            bitNumberField.Title = Title;
        }

        if (Underlined.HasValue && bitNumberField.HasNotBeenSet(nameof(Underlined)))
        {
            bitNumberField.Underlined = Underlined.Value;

            bitNumberField.ClassBuilder.Reset();
        }

        // The numeric string parameters are turned into the typed bounds by setters of their own, which the
        // generated parameter assignment calls and a plain property write does not. They also go through the
        // digit normalization assigned above, and the precision is derived from the Step, so they are applied
        // last and in that order.
        if (Min.HasValue() && bitNumberField.HasNotBeenSet(nameof(Min)))
        {
            bitNumberField.Min = Min;

            bitNumberField.OnSetMin();
        }

        if (Max.HasValue() && bitNumberField.HasNotBeenSet(nameof(Max)))
        {
            bitNumberField.Max = Max;

            bitNumberField.OnSetMax();
        }

        if (Step.HasValue() && bitNumberField.HasNotBeenSet(nameof(Step)))
        {
            bitNumberField.Step = Step;

            bitNumberField.OnSetStep();
        }

        if (PageStep.HasValue() && bitNumberField.HasNotBeenSet(nameof(PageStep)))
        {
            bitNumberField.PageStep = PageStep;

            bitNumberField.OnSetPageStep();
        }

        if (Precision.HasValue && bitNumberField.HasNotBeenSet(nameof(Precision)))
        {
            bitNumberField.Precision = Precision.Value;

            bitNumberField.OnSetPrecision();
        }
    }
}
