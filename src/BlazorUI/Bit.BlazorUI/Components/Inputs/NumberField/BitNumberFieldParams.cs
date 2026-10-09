using System.Globalization;

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
    /// The culture the value is written and read in. Left unset, a NumberFormat renders in the culture of
    /// the current thread while the plain value stays invariant.
    /// </summary>
    public CultureInfo? Culture { get; set; }

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
    /// The action label of the enter key on a virtual keyboard (enterkeyhint), e.g. "done", "next" or "go".
    /// </summary>
    public string? EnterKeyHint { get; set; }

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
    public BitPlacement? LabelPlacement { get; set; }

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

        if (Accent.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Accent), Accent.Value, static n => n.Accent, static (n, v) => n.Accent = v);
        }

        if (AriaDescription.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(AriaDescription), AriaDescription, static n => n.AriaDescription, static (n, v) => n.AriaDescription = v);
        }

        if (AriaPositionInSet.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(AriaPositionInSet), AriaPositionInSet.Value, static n => n.AriaPositionInSet, static (n, v) => n.AriaPositionInSet = v);
        }

        if (AriaSetSize.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(AriaSetSize), AriaSetSize.Value, static n => n.AriaSetSize, static (n, v) => n.AriaSetSize = v);
        }

        if (AriaValueText.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(AriaValueText), AriaValueText, static n => n.AriaValueText, static (n, v) => n.AriaValueText = v);
        }

        if (Background.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Background), Background.Value, static n => n.Background, static (n, v) => n.Background = v);
        }

        if (Border.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Border), Border.Value, static n => n.Border, static (n, v) => n.Border = v);
        }

        if (Classes is not null)
        {
            bitNumberField.TakeFromCascade(nameof(Classes), Classes, static n => n.Classes, static (n, v) => n.Classes = v);
        }

        if (ClearButtonAriaLabel.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(ClearButtonAriaLabel), ClearButtonAriaLabel, static n => n.ClearButtonAriaLabel, static (n, v) => n.ClearButtonAriaLabel = v);
        }


        if (ClearButtonIcon is not null)
        {
            bitNumberField.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static n => n.ClearButtonIcon, static (n, v) => n.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static n => n.ClearButtonIconName, static (n, v) => n.ClearButtonIconName = v);
        }

        if (ContinuousSpinDelay.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(ContinuousSpinDelay), ContinuousSpinDelay.Value, static n => n.ContinuousSpinDelay, static (n, v) => n.ContinuousSpinDelay = v);
        }

        if (ContinuousSpinInterval.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(ContinuousSpinInterval), ContinuousSpinInterval.Value, static n => n.ContinuousSpinInterval, static (n, v) => n.ContinuousSpinInterval = v);
        }

        if (Culture is not null)
        {
            bitNumberField.TakeFromCascade(nameof(Culture), Culture, static n => n.Culture, static (n, v) => n.Culture = v);
        }

        if (DecrementAriaLabel.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(DecrementAriaLabel), DecrementAriaLabel, static n => n.DecrementAriaLabel, static (n, v) => n.DecrementAriaLabel = v);
        }


        if (DecrementIcon is not null)
        {
            bitNumberField.TakeFromCascade(nameof(DecrementIcon), DecrementIcon, static n => n.DecrementIcon, static (n, v) => n.DecrementIcon = v);
        }

        if (DecrementIconName.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(DecrementIconName), DecrementIconName, static n => n.DecrementIconName, static (n, v) => n.DecrementIconName = v);
        }

        if (DecrementTitle.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(DecrementTitle), DecrementTitle, static n => n.DecrementTitle, static (n, v) => n.DecrementTitle = v);
        }

        if (Description.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Description), Description, static n => n.Description, static (n, v) => n.Description = v);
        }

        // The digit normalization is also applied to the Min/Max/Step/PageStep strings, so a normalizer
        // arriving from the cascade has to be in place before those are parsed at the end of this method.
        if (DigitsNormalizer is not null)
        {
            bitNumberField.TakeFromCascade(nameof(DigitsNormalizer), DigitsNormalizer, static n => n.DigitsNormalizer, static (n, v) => n.DigitsNormalizer = v);
        }

        if (EnterKeyHint.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(EnterKeyHint), EnterKeyHint, static n => n.EnterKeyHint, static (n, v) => n.EnterKeyHint = v);
        }

        if (FullWidth.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static n => n.FullWidth, static (n, v) => n.FullWidth = v);
        }

        if (HideInput.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(HideInput), HideInput.Value, static n => n.HideInput, static (n, v) => n.HideInput = v);
        }


        if (Icon is not null)
        {
            bitNumberField.TakeFromCascade(nameof(Icon), Icon, static n => n.Icon, static (n, v) => n.Icon = v);
        }

        if (IconAriaLabel.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(IconAriaLabel), IconAriaLabel, static n => n.IconAriaLabel, static (n, v) => n.IconAriaLabel = v);
        }

        if (IconName.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(IconName), IconName, static n => n.IconName, static (n, v) => n.IconName = v);
        }

        if (IncrementAriaLabel.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(IncrementAriaLabel), IncrementAriaLabel, static n => n.IncrementAriaLabel, static (n, v) => n.IncrementAriaLabel = v);
        }


        if (IncrementIcon is not null)
        {
            bitNumberField.TakeFromCascade(nameof(IncrementIcon), IncrementIcon, static n => n.IncrementIcon, static (n, v) => n.IncrementIcon = v);
        }

        if (IncrementIconName.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(IncrementIconName), IncrementIconName, static n => n.IncrementIconName, static (n, v) => n.IncrementIconName = v);
        }

        if (IncrementTitle.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(IncrementTitle), IncrementTitle, static n => n.IncrementTitle, static (n, v) => n.IncrementTitle = v);
        }

        if (InputMode.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(InputMode), InputMode.Value, static n => n.InputMode, static (n, v) => n.InputMode = v);
        }

        if (InvertMouseWheel.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(InvertMouseWheel), InvertMouseWheel.Value, static n => n.InvertMouseWheel, static (n, v) => n.InvertMouseWheel = v);
        }

        if (IsInputReadOnly.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(IsInputReadOnly), IsInputReadOnly.Value, static n => n.IsInputReadOnly, static (n, v) => n.IsInputReadOnly = v);
        }

        if (LabelPlacement.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(LabelPlacement), LabelPlacement.Value, static n => n.LabelPlacement, static (n, v) => n.LabelPlacement = v);
        }

        if (LoadingAriaLabel.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(LoadingAriaLabel), LoadingAriaLabel, static n => n.LoadingAriaLabel, static (n, v) => n.LoadingAriaLabel = v);
        }

        if (Mode.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Mode), Mode.Value, static n => n.Mode, static (n, v) => n.Mode = v);
        }

        if (NoBorder.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static n => n.NoBorder, static (n, v) => n.NoBorder = v);
        }

        if (NoClamp.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(NoClamp), NoClamp.Value, static n => n.NoClamp, static (n, v) => n.NoClamp = v);
        }

        if (NoMouseWheel.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(NoMouseWheel), NoMouseWheel.Value, static n => n.NoMouseWheel, static (n, v) => n.NoMouseWheel = v);
        }

        if (NoSelectOnFocus.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(NoSelectOnFocus), NoSelectOnFocus.Value, static n => n.NoSelectOnFocus, static (n, v) => n.NoSelectOnFocus = v);
        }

        if (NormalizeDigits.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(NormalizeDigits), NormalizeDigits.Value, static n => n.NormalizeDigits, static (n, v) => n.NormalizeDigits = v);
        }

        if (NumberFormat.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(NumberFormat), NumberFormat, static n => n.NumberFormat, static (n, v) => n.NumberFormat = v);
        }

        if (ParsingErrorMessage.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(ParsingErrorMessage), ParsingErrorMessage!, static n => n.ParsingErrorMessage, static (n, v) => n.ParsingErrorMessage = v);
        }

        if (Placeholder.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Placeholder), Placeholder, static n => n.Placeholder, static (n, v) => n.Placeholder = v);
        }

        if (Prefix.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Prefix), Prefix, static n => n.Prefix, static (n, v) => n.Prefix = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static n => n.ShowClearButton, static (n, v) => n.ShowClearButton = v);
        }

        if (Size.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Size), Size.Value, static n => n.Size, static (n, v) => n.Size = v);
        }

        if (SnapToStep.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(SnapToStep), SnapToStep.Value, static n => n.SnapToStep, static (n, v) => n.SnapToStep = v);
        }

        if (Styles is not null)
        {
            bitNumberField.TakeFromCascade(nameof(Styles), Styles, static n => n.Styles, static (n, v) => n.Styles = v);
        }

        if (Suffix.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Suffix), Suffix, static n => n.Suffix, static (n, v) => n.Suffix = v);
        }

        if (Title.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Title), Title, static n => n.Title, static (n, v) => n.Title = v);
        }

        if (Underlined.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Underlined), Underlined.Value, static n => n.Underlined, static (n, v) => n.Underlined = v);
        }

        // The numeric string parameters are turned into the typed bounds by setters of their own, which the
        // generated parameter assignment calls and a plain property write does not. They also go through the
        // digit normalization assigned above, and the precision is derived from the Step, so they are applied
        // last and in that order.
        if (Min.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Min), Min, static n => n.Min, static (n, v) => n.Min = v);
        }

        if (Max.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Max), Max, static n => n.Max, static (n, v) => n.Max = v);
        }

        if (Step.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(Step), Step, static n => n.Step, static (n, v) => n.Step = v);
        }

        if (PageStep.HasValue())
        {
            bitNumberField.TakeFromCascade(nameof(PageStep), PageStep, static n => n.PageStep, static (n, v) => n.PageStep = v);
        }

        if (Precision.HasValue)
        {
            bitNumberField.TakeFromCascade(nameof(Precision), Precision.Value, static n => n.Precision, static (n, v) => n.Precision = v);
        }
    }
}
