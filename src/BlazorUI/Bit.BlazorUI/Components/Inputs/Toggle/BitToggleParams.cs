namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitToggle"/> component.
/// </summary>
public class BitToggleParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitToggle"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitToggle value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitToggle)}";



    public string Name => ParamName;



    /// <summary>
    /// Keeps a disabled toggle focusable and discoverable by assistive technologies, conveying the disabled
    /// state through <c>aria-disabled</c> instead of the native <c>disabled</c> attribute.
    /// </summary>
    public bool? AllowDisabledFocus { get; set; }

    /// <summary>
    /// The id of the element the toggle controls, rendered as <c>aria-controls</c> on the switch.
    /// </summary>
    public string? AriaControls { get; set; }

    /// <summary>
    /// Detailed description of the toggle for the benefit of screen readers, rendered as a visually
    /// hidden element that the switch points to via <c>aria-describedby</c>.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// The id of an existing element that describes the toggle, rendered as part of <c>aria-describedby</c> on the switch.
    /// </summary>
    public string? AriaDescribedby { get; set; }

    /// <summary>
    /// The id of an existing element that labels the toggle, rendered as <c>aria-labelledby</c> on the switch.
    /// </summary>
    public string? AriaLabelledby { get; set; }

    /// <summary>
    /// If true, the toggle automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Turns the toggle busy on its own for as long as the callbacks behind a change are still running,
    /// without a loading flag having to be tracked outside the component.
    /// </summary>
    public bool? AutoLoading { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the toggle.
    /// </summary>
    public BitToggleClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the toggle, applied to the track of the checked state.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// A visible explanation of what the toggle switches, rendered on a line of its own under it and
    /// announced after the name of the switch through <c>aria-describedby</c>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Renders the toggle in full width of its container while putting space between the label and the knob.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Renders the label and the knob in a single line together.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// Label of the toggle.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the knob of the toggle.
    /// Takes precedence over <see cref="Inline"/> and <see cref="Reversed"/> when set.
    /// </summary>
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Renders a spinner in place of the knob's icon and suspends the toggle until the pending
    /// work behind the change is done.
    /// </summary>
    public bool? Loading { get; set; }

    /// <summary>
    /// The icon rendered inside the knob while the toggle is OFF, using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="OffIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? OffIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon rendered inside the knob while the toggle is OFF.
    /// </summary>
    public string? OffIconName { get; set; }

    /// <summary>
    /// Text to display when toggle is OFF.
    /// </summary>
    public string? OffText { get; set; }

    /// <summary>
    /// The icon rendered inside the knob while the toggle is ON, using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="OnIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? OnIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon rendered inside the knob while the toggle is ON.
    /// </summary>
    public string? OnIconName { get; set; }

    /// <summary>
    /// Text to display when toggle is ON.
    /// </summary>
    public string? OnText { get; set; }

    /// <summary>
    /// Reverses the positions of the label and input of the toggle.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// Denotes role of the toggle, default is switch.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The size of the toggle.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    public bool? StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the toggle.
    /// </summary>
    public BitToggleClassStyles? Styles { get; set; }

    /// <summary>
    /// The default text used when the On or Off texts are null.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The native tooltip of the toggle, shown when the pointer rests anywhere on it.
    /// </summary>
    public string? Title { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitToggle"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitToggle"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitToggle"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitToggle"/>.
    /// </remarks>
    /// <param name="bitToggle">
    /// The <see cref="BitToggle"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitToggle bitToggle)
    {
        if (bitToggle is null) return;

        UpdateBaseParameters(bitToggle);

        if (AllowDisabledFocus.HasValue && bitToggle.HasNotBeenSet(nameof(AllowDisabledFocus)))
        {
            bitToggle.AllowDisabledFocus = AllowDisabledFocus.Value;
        }

        if (AriaControls.HasValue() && bitToggle.HasNotBeenSet(nameof(AriaControls)))
        {
            bitToggle.AriaControls = AriaControls;
        }

        if (AriaDescription.HasValue() && bitToggle.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitToggle.AriaDescription = AriaDescription;
        }

        if (AriaDescribedby.HasValue() && bitToggle.HasNotBeenSet(nameof(AriaDescribedby)))
        {
            bitToggle.AriaDescribedby = AriaDescribedby;
        }

        if (AriaLabelledby.HasValue() && bitToggle.HasNotBeenSet(nameof(AriaLabelledby)))
        {
            bitToggle.AriaLabelledby = AriaLabelledby;
        }

        if (AutoFocus.HasValue && bitToggle.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitToggle.AutoFocus = AutoFocus.Value;
        }

        if (AutoLoading.HasValue && bitToggle.HasNotBeenSet(nameof(AutoLoading)))
        {
            bitToggle.AutoLoading = AutoLoading.Value;
        }

        if (Classes is not null && bitToggle.HasNotBeenSet(nameof(Classes)))
        {
            bitToggle.Classes = Classes;

            bitToggle.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitToggle.HasNotBeenSet(nameof(Color)))
        {
            bitToggle.Color = Color.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (Description.HasValue() && bitToggle.HasNotBeenSet(nameof(Description)))
        {
            bitToggle.Description = Description;

            bitToggle.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitToggle.HasNotBeenSet(nameof(FullWidth)))
        {
            bitToggle.FullWidth = FullWidth.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (Inline.HasValue && bitToggle.HasNotBeenSet(nameof(Inline)))
        {
            bitToggle.Inline = Inline.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (Label.HasValue() && bitToggle.HasNotBeenSet(nameof(Label)))
        {
            bitToggle.Label = Label;

            bitToggle.ClassBuilder.Reset();
        }

        if (LabelPosition.HasValue && bitToggle.HasNotBeenSet(nameof(LabelPosition)))
        {
            bitToggle.LabelPosition = LabelPosition.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (Loading.HasValue && bitToggle.HasNotBeenSet(nameof(Loading)))
        {
            bitToggle.Loading = Loading.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (OffIcon is not null && bitToggle.HasNotBeenSet(nameof(OffIcon)))
        {
            bitToggle.OffIcon = OffIcon;

            bitToggle.ClassBuilder.Reset();
        }

        if (OffIconName.HasValue() && bitToggle.HasNotBeenSet(nameof(OffIconName)))
        {
            bitToggle.OffIconName = OffIconName;

            bitToggle.ClassBuilder.Reset();
        }

        if (OffText.HasValue() && bitToggle.HasNotBeenSet(nameof(OffText)))
        {
            bitToggle.OffText = OffText;
        }

        if (OnIcon is not null && bitToggle.HasNotBeenSet(nameof(OnIcon)))
        {
            bitToggle.OnIcon = OnIcon;

            bitToggle.ClassBuilder.Reset();
        }

        if (OnIconName.HasValue() && bitToggle.HasNotBeenSet(nameof(OnIconName)))
        {
            bitToggle.OnIconName = OnIconName;

            bitToggle.ClassBuilder.Reset();
        }

        if (OnText.HasValue() && bitToggle.HasNotBeenSet(nameof(OnText)))
        {
            bitToggle.OnText = OnText;
        }

        if (Reversed.HasValue && bitToggle.HasNotBeenSet(nameof(Reversed)))
        {
            bitToggle.Reversed = Reversed.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (Role.HasValue() && bitToggle.HasNotBeenSet(nameof(Role)))
        {
            bitToggle.Role = Role;
        }

        if (Size.HasValue && bitToggle.HasNotBeenSet(nameof(Size)))
        {
            bitToggle.Size = Size.Value;

            bitToggle.ClassBuilder.Reset();
        }

        if (StopPropagation.HasValue && bitToggle.HasNotBeenSet(nameof(StopPropagation)))
        {
            bitToggle.StopPropagation = StopPropagation.Value;
        }

        if (Styles is not null && bitToggle.HasNotBeenSet(nameof(Styles)))
        {
            bitToggle.Styles = Styles;

            bitToggle.StyleBuilder.Reset();
        }

        if (Text.HasValue() && bitToggle.HasNotBeenSet(nameof(Text)))
        {
            bitToggle.Text = Text;
        }

        if (Title.HasValue() && bitToggle.HasNotBeenSet(nameof(Title)))
        {
            bitToggle.Title = Title;
        }
    }
}
