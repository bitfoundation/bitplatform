namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitColorPicker"/> component.
/// </summary>
/// <remarks>
/// It carries the shape of the picker - which controls it shows, how it is laid out, which palette it offers -
/// and not the color it is on: <see cref="BitColorPicker.Color"/> and <see cref="BitColorPicker.Alpha"/> are the
/// value of one picker rather than a setting shared by every picker under a <see cref="BitParams"/>. The two
/// change callbacks are left off for the same reason, since what a page does with a new color is the one thing
/// that belongs to the picker it came from.
/// </remarks>
public class BitColorPickerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitColorPicker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitColorPicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitColorPicker)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether the picker takes the focus on the first render, landing on the saturation-brightness area - or, on
    /// a picker built without it, on whichever of its controls comes first.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitColorPicker.
    /// </summary>
    public BitColorPickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The color the contrast readout measures the picked color against - the background it is going to be
    /// read on. It accepts any of the notations the Color parameter does, and defaults to white.
    /// </summary>
    public string? ContrastColor { get; set; }

    /// <summary>
    /// The set of channels the text fields start in. It decides how the color is typed, not how it is
    /// published - a picker edited in HSL still answers in whatever <see cref="Format"/> says. Only the
    /// starting mode is cascaded, so the inputs mode switch keeps whatever the user moves it to.
    /// </summary>
    public BitColorInputsMode? DefaultInputsMode { get; set; }

    /// <summary>
    /// Gets or sets the icon of the eye dropper button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="EyeDropperIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? EyeDropperIcon { get; set; }

    /// <summary>
    /// Custom icon name for the eye dropper button. If unset, default will be the Eyedropper icon.
    /// </summary>
    public string? EyeDropperIconName { get; set; }

    /// <summary>
    /// The CSS notation the color value is written in. When left unset the picker answers in the same
    /// notation the Color arrived in.
    /// </summary>
    public BitColorFormat? Format { get; set; }

    /// <summary>
    /// Gets or sets the icon of the inputs mode switch button using custom CSS classes for external icon
    /// libraries. Takes precedence over <see cref="InputsModeSwitchIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? InputsModeSwitchIcon { get; set; }

    /// <summary>
    /// Custom icon name for the inputs mode switch button. If unset, default will be the Sort icon.
    /// </summary>
    public string? InputsModeSwitchIconName { get; set; }

    /// <summary>
    /// The text that names the picker.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom markup in place of the plain <see cref="Label"/> text, for when the name needs more than a
    /// string - an icon beside it, a required marker, a link.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The colors offered as a row of one-click swatches under the picker, in any of the notations the
    /// Color parameter accepts. This is the parameter a brand palette is usually cascaded through, so that
    /// every picker on a page offers the same colors without repeating the list.
    /// </summary>
    public IEnumerable<string>? Presets { get; set; }

    /// <summary>
    /// How many preset swatches are laid out per row. Left unset they simply wrap; setting it lays them out
    /// on a grid instead.
    /// </summary>
    public int? PresetsPerRow { get; set; }

    /// <summary>
    /// Makes the color picker read-only: the value is still shown at full contrast, but nothing about it
    /// can be changed.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Whether to show a slider for editing alpha value.
    /// </summary>
    public bool? ShowAlphaSlider { get; set; }

    /// <summary>
    /// Whether to show the contrast readout: how far the picked color stands from the
    /// <see cref="ContrastColor"/> it will be read on, and whether that clears the WCAG bar for text.
    /// </summary>
    public bool? ShowContrast { get; set; }

    /// <summary>
    /// Whether to show the button that opens the browser's eyedropper to sample a color from anywhere on
    /// the screen.
    /// </summary>
    public bool? ShowEyeDropper { get; set; }

    /// <summary>
    /// Whether to show the hue slider. It is on by default; turning it off pins the picker to one hue.
    /// </summary>
    public bool? ShowHueSlider { get; set; }

    /// <summary>
    /// Whether to show the hexadecimal and Red-Green-Blue text fields.
    /// </summary>
    public bool? ShowInputs { get; set; }

    /// <summary>
    /// Whether to show the button that moves the text fields from one set of channels to the next.
    /// </summary>
    public bool? ShowInputsModeSwitch { get; set; }

    /// <summary>
    /// Whether to show color preview box.
    /// </summary>
    public bool? ShowPreview { get; set; }

    /// <summary>
    /// Whether to show the saturation-brightness area. It is on by default; turning it off is what makes a
    /// palette picker out of the presets, the text fields, or both.
    /// </summary>
    public bool? ShowSaturationArea { get; set; }

    /// <summary>
    /// The size of the color picker.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitColorPicker.
    /// </summary>
    public BitColorPickerClassStyles? Styles { get; set; }

    /// <summary>
    /// Every piece of text the picker writes for itself. This is the parameter that translates the
    /// component, and the one most worth cascading: a translation belongs to the application rather than
    /// to one picker in it.
    /// </summary>
    public BitColorPickerTexts? Texts { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitColorPicker"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitColorPicker"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitColorPicker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitColorPicker"/>.
    /// </remarks>
    /// <param name="bitColorPicker">
    /// The <see cref="BitColorPicker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitColorPicker bitColorPicker)
    {
        if (bitColorPicker is null) return;

        UpdateBaseParameters(bitColorPicker);

        if (AutoFocus.HasValue && bitColorPicker.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitColorPicker.AutoFocus = AutoFocus.Value;
        }

        if (Classes is not null && bitColorPicker.HasNotBeenSet(nameof(Classes)))
        {
            bitColorPicker.Classes = Classes;

            bitColorPicker.ClassBuilder.Reset();
        }

        if (ContrastColor.HasValue() && bitColorPicker.HasNotBeenSet(nameof(ContrastColor)))
        {
            bitColorPicker.ContrastColor = ContrastColor;
        }

        if (DefaultInputsMode.HasValue && bitColorPicker.HasNotBeenSet(nameof(DefaultInputsMode)))
        {
            bitColorPicker.DefaultInputsMode = DefaultInputsMode.Value;
        }

        if (EyeDropperIcon is not null && bitColorPicker.HasNotBeenSet(nameof(EyeDropperIcon)))
        {
            bitColorPicker.EyeDropperIcon = EyeDropperIcon;
        }

        if (EyeDropperIconName.HasValue() && bitColorPicker.HasNotBeenSet(nameof(EyeDropperIconName)))
        {
            bitColorPicker.EyeDropperIconName = EyeDropperIconName;
        }

        if (Format.HasValue && bitColorPicker.HasNotBeenSet(nameof(Format)))
        {
            bitColorPicker.Format = Format.Value;
        }

        if (InputsModeSwitchIcon is not null && bitColorPicker.HasNotBeenSet(nameof(InputsModeSwitchIcon)))
        {
            bitColorPicker.InputsModeSwitchIcon = InputsModeSwitchIcon;
        }

        if (InputsModeSwitchIconName.HasValue() && bitColorPicker.HasNotBeenSet(nameof(InputsModeSwitchIconName)))
        {
            bitColorPicker.InputsModeSwitchIconName = InputsModeSwitchIconName;
        }

        if (Label.HasValue() && bitColorPicker.HasNotBeenSet(nameof(Label)))
        {
            bitColorPicker.Label = Label;
        }

        if (LabelTemplate is not null && bitColorPicker.HasNotBeenSet(nameof(LabelTemplate)))
        {
            bitColorPicker.LabelTemplate = LabelTemplate;
        }

        if (Presets is not null && bitColorPicker.HasNotBeenSet(nameof(Presets)))
        {
            bitColorPicker.Presets = Presets;
        }

        if (PresetsPerRow.HasValue && bitColorPicker.HasNotBeenSet(nameof(PresetsPerRow)))
        {
            bitColorPicker.PresetsPerRow = PresetsPerRow.Value;
        }

        if (ReadOnly.HasValue && bitColorPicker.HasNotBeenSet(nameof(ReadOnly)))
        {
            bitColorPicker.ReadOnly = ReadOnly.Value;

            bitColorPicker.ClassBuilder.Reset();
        }

        if (ShowAlphaSlider.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowAlphaSlider)))
        {
            bitColorPicker.ShowAlphaSlider = ShowAlphaSlider.Value;
        }

        if (ShowContrast.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowContrast)))
        {
            bitColorPicker.ShowContrast = ShowContrast.Value;
        }

        if (ShowEyeDropper.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowEyeDropper)))
        {
            bitColorPicker.ShowEyeDropper = ShowEyeDropper.Value;
        }

        if (ShowHueSlider.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowHueSlider)))
        {
            bitColorPicker.ShowHueSlider = ShowHueSlider.Value;
        }

        if (ShowInputs.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowInputs)))
        {
            bitColorPicker.ShowInputs = ShowInputs.Value;
        }

        if (ShowInputsModeSwitch.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowInputsModeSwitch)))
        {
            bitColorPicker.ShowInputsModeSwitch = ShowInputsModeSwitch.Value;
        }

        if (ShowPreview.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowPreview)))
        {
            bitColorPicker.ShowPreview = ShowPreview.Value;
        }

        if (ShowSaturationArea.HasValue && bitColorPicker.HasNotBeenSet(nameof(ShowSaturationArea)))
        {
            bitColorPicker.ShowSaturationArea = ShowSaturationArea.Value;
        }

        if (Size.HasValue && bitColorPicker.HasNotBeenSet(nameof(Size)))
        {
            bitColorPicker.Size = Size.Value;

            bitColorPicker.ClassBuilder.Reset();
        }

        if (Styles is not null && bitColorPicker.HasNotBeenSet(nameof(Styles)))
        {
            bitColorPicker.Styles = Styles;

            bitColorPicker.StyleBuilder.Reset();
        }

        if (Texts is not null && bitColorPicker.HasNotBeenSet(nameof(Texts)))
        {
            bitColorPicker.Texts = Texts;
        }
    }
}
