namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitOtpInput"/> component.
/// </summary>
public class BitOtpInputParams : BitComponentBaseParams, IBitComponentParams
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
    /// Puts the component into the busy state of a code that has been submitted and is being checked.
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

        UpdateBaseParameters(bitOtpInput);

        if (Accent.HasValue && bitOtpInput.HasNotBeenSet(nameof(Accent)))
        {
            bitOtpInput.Accent = Accent.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (AutoFocus.HasValue && bitOtpInput.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitOtpInput.AutoFocus = AutoFocus.Value;
        }

        if (AutoShift.HasValue && bitOtpInput.HasNotBeenSet(nameof(AutoShift)))
        {
            bitOtpInput.AutoShift = AutoShift.Value;
        }

        if (BlurOnFill.HasValue && bitOtpInput.HasNotBeenSet(nameof(BlurOnFill)))
        {
            bitOtpInput.BlurOnFill = BlurOnFill.Value;
        }

        if (Classes is not null && bitOtpInput.HasNotBeenSet(nameof(Classes)))
        {
            bitOtpInput.Classes = Classes;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (Description.HasValue() && bitOtpInput.HasNotBeenSet(nameof(Description)))
        {
            bitOtpInput.Description = Description;
        }

        if (FullWidth.HasValue && bitOtpInput.HasNotBeenSet(nameof(FullWidth)))
        {
            bitOtpInput.FullWidth = FullWidth.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (InputAriaLabelFormat.HasValue() && bitOtpInput.HasNotBeenSet(nameof(InputAriaLabelFormat)))
        {
            bitOtpInput.InputAriaLabelFormat = InputAriaLabelFormat;
        }

        if (InputMode.HasValue && bitOtpInput.HasNotBeenSet(nameof(InputMode)))
        {
            bitOtpInput.InputMode = InputMode.Value;
        }

        if (Invalid.HasValue && bitOtpInput.HasNotBeenSet(nameof(Invalid)))
        {
            bitOtpInput.Invalid = Invalid.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (IsLoading.HasValue && bitOtpInput.HasNotBeenSet(nameof(IsLoading)))
        {
            bitOtpInput.IsLoading = IsLoading.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (Label.HasValue() && bitOtpInput.HasNotBeenSet(nameof(Label)))
        {
            bitOtpInput.Label = Label;
        }

        if (Length.HasValue && bitOtpInput.HasNotBeenSet(nameof(Length)))
        {
            bitOtpInput.Length = Length.Value;
        }

        if (Lowercase.HasValue && bitOtpInput.HasNotBeenSet(nameof(Lowercase)))
        {
            bitOtpInput.Lowercase = Lowercase.Value;
        }

        if (Mask.HasValue() && bitOtpInput.HasNotBeenSet(nameof(Mask)))
        {
            bitOtpInput.Mask = Mask;
        }

        if (Merged.HasValue && bitOtpInput.HasNotBeenSet(nameof(Merged)))
        {
            bitOtpInput.Merged = Merged.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (NormalizeDigits.HasValue && bitOtpInput.HasNotBeenSet(nameof(NormalizeDigits)))
        {
            bitOtpInput.NormalizeDigits = NormalizeDigits.Value;
        }

        if (NoSmsAutoFill.HasValue && bitOtpInput.HasNotBeenSet(nameof(NoSmsAutoFill)))
        {
            bitOtpInput.NoSmsAutoFill = NoSmsAutoFill.Value;
        }

        if (PasteTransformer is not null && bitOtpInput.HasNotBeenSet(nameof(PasteTransformer)))
        {
            bitOtpInput.PasteTransformer = PasteTransformer;
        }

        if (Pattern.HasValue() && bitOtpInput.HasNotBeenSet(nameof(Pattern)))
        {
            bitOtpInput.Pattern = Pattern;
        }

        if (Placeholder.HasValue() && bitOtpInput.HasNotBeenSet(nameof(Placeholder)))
        {
            bitOtpInput.Placeholder = Placeholder;
        }

        if (Reversed.HasValue && bitOtpInput.HasNotBeenSet(nameof(Reversed)))
        {
            bitOtpInput.Reversed = Reversed.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (Separator.HasValue() && bitOtpInput.HasNotBeenSet(nameof(Separator)))
        {
            bitOtpInput.Separator = Separator;
        }

        if (SeparatorInterval.HasValue && bitOtpInput.HasNotBeenSet(nameof(SeparatorInterval)))
        {
            bitOtpInput.SeparatorInterval = SeparatorInterval.Value;
        }

        if (Sequential.HasValue && bitOtpInput.HasNotBeenSet(nameof(Sequential)))
        {
            bitOtpInput.Sequential = Sequential.Value;
        }

        if (SingleTabStop.HasValue && bitOtpInput.HasNotBeenSet(nameof(SingleTabStop)))
        {
            bitOtpInput.SingleTabStop = SingleTabStop.Value;
        }

        if (Size.HasValue && bitOtpInput.HasNotBeenSet(nameof(Size)))
        {
            bitOtpInput.Size = Size.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (Styles is not null && bitOtpInput.HasNotBeenSet(nameof(Styles)))
        {
            bitOtpInput.Styles = Styles;

            bitOtpInput.StyleBuilder.Reset();
        }

        if (Type.HasValue && bitOtpInput.HasNotBeenSet(nameof(Type)))
        {
            bitOtpInput.Type = Type.Value;
        }

        if (Uppercase.HasValue && bitOtpInput.HasNotBeenSet(nameof(Uppercase)))
        {
            bitOtpInput.Uppercase = Uppercase.Value;
        }

        if (Variant.HasValue && bitOtpInput.HasNotBeenSet(nameof(Variant)))
        {
            bitOtpInput.Variant = Variant.Value;

            bitOtpInput.ClassBuilder.Reset();
        }

        if (Vertical.HasValue && bitOtpInput.HasNotBeenSet(nameof(Vertical)))
        {
            bitOtpInput.Vertical = Vertical.Value;

            bitOtpInput.ClassBuilder.Reset();
        }
    }
}
