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
    /// A line under the toggle saying why its state was rejected, which marks it invalid in the same way
    /// <see cref="Invalid"/> does and is announced the moment it shows up.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Renders the toggle in full width of its container while putting space between the label and the knob.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Renders the label and the knob in a single line together.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// Marks the state of the toggle as invalid, giving it the same look and the same <c>aria-invalid</c>
    /// attribute that a failing data annotation gives it.
    /// </summary>
    public bool? Invalid { get; set; }

    /// <summary>
    /// Label of the toggle.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the knob of the toggle.
    /// Takes precedence over <see cref="Inline"/> and <see cref="Reversed"/> when set.
    /// </summary>
    public BitPlacement? LabelPlacement { get; set; }

    /// <summary>
    /// Renders a spinner in place of everything the knob carries - its icon or <c>ThumbTemplate</c> - and
    /// suspends the toggle until the pending work behind the change is done.
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

        if (AllowDisabledFocus.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(AllowDisabledFocus), AllowDisabledFocus.Value, static t => t.AllowDisabledFocus, static (t, v) => t.AllowDisabledFocus = v);
        }

        if (AriaControls.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(AriaControls), AriaControls, static t => t.AriaControls, static (t, v) => t.AriaControls = v);
        }

        if (AriaDescription.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(AriaDescription), AriaDescription, static t => t.AriaDescription, static (t, v) => t.AriaDescription = v);
        }

        if (AriaDescribedby.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(AriaDescribedby), AriaDescribedby, static t => t.AriaDescribedby, static (t, v) => t.AriaDescribedby = v);
        }

        if (AriaLabelledby.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(AriaLabelledby), AriaLabelledby, static t => t.AriaLabelledby, static (t, v) => t.AriaLabelledby = v);
        }

        if (AutoFocus.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static t => t.AutoFocus, static (t, v) => t.AutoFocus = v);
        }

        if (AutoLoading.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(AutoLoading), AutoLoading.Value, static t => t.AutoLoading, static (t, v) => t.AutoLoading = v);
        }

        if (Classes is not null)
        {
            bitToggle.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Color.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (Description.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(Description), Description, static t => t.Description, static (t, v) => t.Description = v);
        }

        if (ErrorMessage.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(ErrorMessage), ErrorMessage, static t => t.ErrorMessage, static (t, v) => t.ErrorMessage = v);
        }

        if (FullWidth.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static t => t.FullWidth, static (t, v) => t.FullWidth = v);
        }

        if (Inline.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(Inline), Inline.Value, static t => t.Inline, static (t, v) => t.Inline = v);
        }

        if (Invalid.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(Invalid), Invalid.Value, static t => t.Invalid, static (t, v) => t.Invalid = v);
        }

        if (Label.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(Label), Label, static t => t.Label, static (t, v) => t.Label = v);
        }

        if (LabelPlacement.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(LabelPlacement), LabelPlacement.Value, static t => t.LabelPlacement, static (t, v) => t.LabelPlacement = v);
        }

        if (Loading.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(Loading), Loading.Value, static t => t.Loading, static (t, v) => t.Loading = v);
        }

        var ownOffIcon = bitToggle.HasSetAnyOf(nameof(OffIcon), nameof(OffIconName));

        if (OffIcon is not null)
        {
            bitToggle.TakeFromCascade(nameof(OffIcon), OffIcon, static t => t.OffIcon, static (t, v) => t.OffIcon = v, outranked: ownOffIcon);
        }

        if (OffIconName.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(OffIconName), OffIconName, static t => t.OffIconName, static (t, v) => t.OffIconName = v, outranked: ownOffIcon);
        }

        if (OffText.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(OffText), OffText, static t => t.OffText, static (t, v) => t.OffText = v);
        }

        var ownOnIcon = bitToggle.HasSetAnyOf(nameof(OnIcon), nameof(OnIconName));

        if (OnIcon is not null)
        {
            bitToggle.TakeFromCascade(nameof(OnIcon), OnIcon, static t => t.OnIcon, static (t, v) => t.OnIcon = v, outranked: ownOnIcon);
        }

        if (OnIconName.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(OnIconName), OnIconName, static t => t.OnIconName, static (t, v) => t.OnIconName = v, outranked: ownOnIcon);
        }

        if (OnText.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(OnText), OnText, static t => t.OnText, static (t, v) => t.OnText = v);
        }

        if (Reversed.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(Reversed), Reversed.Value, static t => t.Reversed, static (t, v) => t.Reversed = v);
        }

        if (Role.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(Role), Role, static t => t.Role, static (t, v) => t.Role = v);
        }

        if (Size.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(Size), Size.Value, static t => t.Size, static (t, v) => t.Size = v);
        }

        if (StopPropagation.HasValue)
        {
            bitToggle.TakeFromCascade(nameof(StopPropagation), StopPropagation.Value, static t => t.StopPropagation, static (t, v) => t.StopPropagation = v);
        }

        if (Styles is not null)
        {
            bitToggle.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }

        if (Text.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(Text), Text, static t => t.Text, static (t, v) => t.Text = v);
        }

        if (Title.HasValue())
        {
            bitToggle.TakeFromCascade(nameof(Title), Title, static t => t.Title, static (t, v) => t.Title = v);
        }
    }
}
