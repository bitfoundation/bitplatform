namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitOtpInput"/> component.
/// </summary>
public class BitOtpInputParams : BitInputBaseParams<string?>, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitOtpInput"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitOtpInput value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitOtpInput)}";



    public string Name => ParamName;



    /// <summary>
    /// The accent color of the inputs, applied to the border and the focus ring of the focused input.
    /// <br />
    /// <see cref="BitOtpInput.Accent"/>.
    /// </summary>
    public BitColor? Accent { get; set; }

    /// <summary>
    /// If true, the first input is auto focused.
    /// <br />
    /// <see cref="BitOtpInput.AutoFocus"/>.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Enables auto shifting the indexes while clearing the inputs using Delete or Backspace.
    /// <br />
    /// <see cref="BitOtpInput.AutoShift"/>.
    /// </summary>
    public bool? AutoShift { get; set; }

    /// <summary>
    /// Submits the form the component sits in as soon as the code is complete, the way pressing Enter would.
    /// <br />
    /// <see cref="BitOtpInput.AutoSubmit"/>.
    /// </summary>
    public bool? AutoSubmit { get; set; }

    /// <summary>
    /// Removes the focus from the inputs as soon as the code is complete, which is what dismisses the
    /// virtual keyboard of a phone once there is nothing left to type.
    /// <br />
    /// <see cref="BitOtpInput.BlurOnFill"/>.
    /// </summary>
    public bool? BlurOnFill { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitOtpInput.
    /// <br />
    /// <see cref="BitOtpInput.Classes"/>.
    /// </summary>
    public BitOtpInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The description (helper text) rendered under the inputs, which the group of the inputs references
    /// through its aria-describedby.
    /// <br />
    /// <see cref="BitOtpInput.Description"/>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Stretches the row of inputs across the available width and lets the inputs share it evenly.
    /// <br />
    /// <see cref="BitOtpInput.FullWidth"/>.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The composite format of the aria-label rendered on each input, where {0} is the one based index
    /// of the input and {1} is the Length.
    /// <br />
    /// <see cref="BitOtpInput.InputAriaLabelFormat"/>.
    /// </summary>
    public string? InputAriaLabelFormat { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the inputs, which is what decides the virtual keyboard that a
    /// phone brings up without changing the element that is rendered.
    /// <br />
    /// <see cref="BitOtpInput.InputMode"/>.
    /// </summary>
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Paints the inputs with the error state without an EditContext taking part in it.
    /// <br />
    /// <see cref="BitOtpInput.Invalid"/>.
    /// </summary>
    public bool? Invalid { get; set; }

    /// <summary>
    /// Puts the component into the busy state of a code that has been submitted and is being checked, which
    /// draws a progress bar under the inputs, holds the code still and announces the wait.
    /// <br />
    /// <see cref="BitOtpInput.IsLoading"/>.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// Label displayed above the inputs.
    /// <br />
    /// <see cref="BitOtpInput.Label"/>.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Length of the OTP or number of the inputs.
    /// <br />
    /// <see cref="BitOtpInput.Length"/>.
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Turns every character of the code into its lower case form as it is typed or pasted.
    /// <br />
    /// <see cref="BitOtpInput.Lowercase"/>.
    /// </summary>
    public bool? Lowercase { get; set; }

    /// <summary>
    /// The text rendered in place of every filled input, which hides the code without turning the inputs
    /// into password inputs.
    /// <br />
    /// <see cref="BitOtpInput.Mask"/>.
    /// </summary>
    public string? Mask { get; set; }

    /// <summary>
    /// Glues the inputs of each group together into a single field instead of leaving them standing next
    /// to each other.
    /// <br />
    /// <see cref="BitOtpInput.Merged"/>.
    /// </summary>
    public bool? Merged { get; set; }

    /// <summary>
    /// Turns the digits of the other numbering systems into their ASCII form as they are typed or pasted.
    /// <br />
    /// <see cref="BitOtpInput.NormalizeDigits"/>.
    /// </summary>
    public bool? NormalizeDigits { get; set; }

    /// <summary>
    /// Disables both the SMS auto fill of the OTP through the WebOTP API of the browser and the
    /// one-time-code autofill of the inputs themselves.
    /// <br />
    /// <see cref="BitOtpInput.NoSmsAutoFill"/>.
    /// </summary>
    public bool? NoSmsAutoFill { get; set; }

    /// <summary>
    /// A function applied to a chunk of characters that reaches the component in one go (a paste, an SMS
    /// auto fill, or a multi character input event) before anything else is done with it. Pulling the code
    /// out of the message it was copied inside of is a rule of the application rather than of one field,
    /// which is what makes it worth cascading.
    /// <br />
    /// <see cref="BitOtpInput.PasteTransformer"/>.
    /// </summary>
    public Func<string, string>? PasteTransformer { get; set; }

    /// <summary>
    /// A regular expression that every single character of the code has to match.
    /// <br />
    /// <see cref="BitOtpInput.Pattern"/>.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// The hint text rendered in the empty inputs.
    /// <br />
    /// <see cref="BitOtpInput.Placeholder"/>.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Defines whether to render inputs in the opposite direction.
    /// <br />
    /// <see cref="BitOtpInput.Reversed"/>.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The text rendered between the inputs, like a dash or a dot, to make a long code easier to read.
    /// <br />
    /// <see cref="BitOtpInput.Separator"/>.
    /// </summary>
    public string? Separator { get; set; }

    /// <summary>
    /// The number of inputs of each group that the Separator is rendered between.
    /// <br />
    /// <see cref="BitOtpInput.SeparatorInterval"/>.
    /// </summary>
    public int? SeparatorInterval { get; set; }

    /// <summary>
    /// Keeps the code free of holes by pulling the focus, and any chunk that arrives at once, back to the
    /// first input left to fill.
    /// <br />
    /// <see cref="BitOtpInput.Sequential"/>.
    /// </summary>
    public bool? Sequential { get; set; }

    /// <summary>
    /// Turns the whole component into a single stop of the tab order.
    /// <br />
    /// <see cref="BitOtpInput.SingleTabStop"/>.
    /// </summary>
    public bool? SingleTabStop { get; set; }

    /// <summary>
    /// The size of the inputs.
    /// <br />
    /// <see cref="BitOtpInput.Size"/>.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitOtpInput.
    /// <br />
    /// <see cref="BitOtpInput.Styles"/>.
    /// </summary>
    public BitOtpInputClassStyles? Styles { get; set; }

    /// <summary>
    /// Type of the inputs.
    /// <br />
    /// <see cref="BitOtpInput.Type"/>.
    /// </summary>
    public BitInputType? Type { get; set; }

    /// <summary>
    /// Turns every character of the code into its upper case form as it is typed or pasted.
    /// <br />
    /// <see cref="BitOtpInput.Uppercase"/>.
    /// </summary>
    public bool? Uppercase { get; set; }

    /// <summary>
    /// The visual variant of the inputs, which decides how much of the frame around each input is painted.
    /// <br />
    /// <see cref="BitOtpInput.Variant"/>.
    /// </summary>
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Defines whether to render inputs vertically.
    /// <br />
    /// <see cref="BitOtpInput.Vertical"/>.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitOtpInput"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitOtpInput"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitOtpInput"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitOtpInput"/>.
    /// </remarks>
    /// <param name="bitOtpInput">
    /// The <see cref="BitOtpInput"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitOtpInput bitOtpInput)
    {
        if (bitOtpInput is null) return;

        UpdateInputBaseParameters(bitOtpInput);

        if (Accent.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Accent), Accent.Value, static o => o.Accent, static (o, v) => o.Accent = v);
        }

        if (AutoFocus.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static o => o.AutoFocus, static (o, v) => o.AutoFocus = v);
        }

        if (AutoShift.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(AutoShift), AutoShift.Value, static o => o.AutoShift, static (o, v) => o.AutoShift = v);
        }

        if (AutoSubmit.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(AutoSubmit), AutoSubmit.Value, static o => o.AutoSubmit, static (o, v) => o.AutoSubmit = v);
        }

        if (BlurOnFill.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(BlurOnFill), BlurOnFill.Value, static o => o.BlurOnFill, static (o, v) => o.BlurOnFill = v);
        }

        if (Classes is not null)
        {
            bitOtpInput.TakeFromCascade(nameof(Classes), Classes, static o => o.Classes, static (o, v) => o.Classes = v);
        }

        if (Description.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(Description), Description, static o => o.Description, static (o, v) => o.Description = v);
        }

        if (FullWidth.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static o => o.FullWidth, static (o, v) => o.FullWidth = v);
        }

        if (InputAriaLabelFormat.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(InputAriaLabelFormat), InputAriaLabelFormat, static o => o.InputAriaLabelFormat, static (o, v) => o.InputAriaLabelFormat = v);
        }

        if (InputMode.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(InputMode), InputMode.Value, static o => o.InputMode, static (o, v) => o.InputMode = v);
        }

        if (Invalid.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Invalid), Invalid.Value, static o => o.Invalid, static (o, v) => o.Invalid = v);
        }

        if (IsLoading.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static o => o.IsLoading, static (o, v) => o.IsLoading = v);
        }

        if (Label.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(Label), Label, static o => o.Label, static (o, v) => o.Label = v);
        }

        if (Length.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Length), Length.Value, static o => o.Length, static (o, v) => o.Length = v);
        }

        if (Lowercase.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Lowercase), Lowercase.Value, static o => o.Lowercase, static (o, v) => o.Lowercase = v);
        }

        if (Mask.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(Mask), Mask, static o => o.Mask, static (o, v) => o.Mask = v);
        }

        if (Merged.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Merged), Merged.Value, static o => o.Merged, static (o, v) => o.Merged = v);
        }

        if (NormalizeDigits.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(NormalizeDigits), NormalizeDigits.Value, static o => o.NormalizeDigits, static (o, v) => o.NormalizeDigits = v);
        }

        if (NoSmsAutoFill.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(NoSmsAutoFill), NoSmsAutoFill.Value, static o => o.NoSmsAutoFill, static (o, v) => o.NoSmsAutoFill = v);
        }

        if (PasteTransformer is not null)
        {
            bitOtpInput.TakeFromCascade(nameof(PasteTransformer), PasteTransformer, static o => o.PasteTransformer, static (o, v) => o.PasteTransformer = v);
        }

        if (Pattern.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(Pattern), Pattern, static o => o.Pattern, static (o, v) => o.Pattern = v);
        }

        if (Placeholder.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(Placeholder), Placeholder, static o => o.Placeholder, static (o, v) => o.Placeholder = v);
        }

        if (Reversed.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Reversed), Reversed.Value, static o => o.Reversed, static (o, v) => o.Reversed = v);
        }

        if (Separator.HasValue())
        {
            bitOtpInput.TakeFromCascade(nameof(Separator), Separator, static o => o.Separator, static (o, v) => o.Separator = v);
        }

        if (SeparatorInterval.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(SeparatorInterval), SeparatorInterval.Value, static o => o.SeparatorInterval, static (o, v) => o.SeparatorInterval = v);
        }

        if (Sequential.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Sequential), Sequential.Value, static o => o.Sequential, static (o, v) => o.Sequential = v);
        }

        if (SingleTabStop.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(SingleTabStop), SingleTabStop.Value, static o => o.SingleTabStop, static (o, v) => o.SingleTabStop = v);
        }

        if (Size.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Size), Size.Value, static o => o.Size, static (o, v) => o.Size = v);
        }

        if (Styles is not null)
        {
            bitOtpInput.TakeFromCascade(nameof(Styles), Styles, static o => o.Styles, static (o, v) => o.Styles = v);
        }

        if (Type.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Type), Type.Value, static o => o.Type, static (o, v) => o.Type = v);
        }

        if (Uppercase.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Uppercase), Uppercase.Value, static o => o.Uppercase, static (o, v) => o.Uppercase = v);
        }

        if (Variant.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Variant), Variant.Value, static o => o.Variant, static (o, v) => o.Variant = v);
        }

        if (Vertical.HasValue)
        {
            bitOtpInput.TakeFromCascade(nameof(Vertical), Vertical.Value, static o => o.Vertical, static (o, v) => o.Vertical = v);
        }
    }
}
