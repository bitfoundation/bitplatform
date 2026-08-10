namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for the parts of the <see cref="BitColorPicker"/>.
/// </summary>
public class BitColorPickerClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the color picker.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element the label is rendered into.
    /// </summary>
    public string? LabelContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label text of the color picker.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the saturation-value area of the color picker.
    /// </summary>
    public string? SaturationPicker { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the thumb of the saturation-value area.
    /// </summary>
    public string? SaturationThumb { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row that holds the sliders, the eye dropper and the preview.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the column that holds the hue and alpha sliders.
    /// </summary>
    public string? Sliders { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the track of the hue slider.
    /// </summary>
    public string? HueSlider { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the track of the alpha slider.
    /// </summary>
    public string? AlphaSlider { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the range inputs of both sliders.
    /// </summary>
    public string? SliderInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the eye dropper button.
    /// </summary>
    public string? EyeDropper { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the eye dropper button.
    /// </summary>
    public string? EyeDropperIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the color preview box.
    /// </summary>
    public string? Preview { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row of the hex and channel text fields.
    /// </summary>
    public string? Inputs { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for a single field of the inputs row, label included.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text input of a single field.
    /// </summary>
    public string? FieldInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the caption of a single field.
    /// </summary>
    public string? FieldLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button that moves the text fields to the next set of channels.
    /// </summary>
    public string? InputsModeSwitch { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the inputs mode switch button.
    /// </summary>
    public string? InputsModeSwitchIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row that holds the contrast readout.
    /// </summary>
    public string? Contrast { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the contrast ratio itself.
    /// </summary>
    public string? ContrastRatio { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for a pass/fail badge of the contrast readout.
    /// </summary>
    public string? ContrastBadge { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the preset swatches.
    /// </summary>
    public string? Presets { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for a single preset swatch.
    /// </summary>
    public string? Preset { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the preset swatch of the current color, applied on top of <see cref="Preset"/>.
    /// </summary>
    public string? SelectedPreset { get; set; }
}
