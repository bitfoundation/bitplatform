namespace Bit.BlazorUI;

/// <summary>
/// Every piece of text the <see cref="BitColorPicker"/> writes for itself: the accessible names of its
/// controls, the captions of its fields, and the sentences it announces the color with.
/// </summary>
/// <remarks>
/// A color picker is mostly gradient, so nearly everything it tells a screen reader is text it writes
/// rather than text a consumer hands it. This is where that text is translated. The defaults are English,
/// an unset property keeps its default, and the whole object cascades through
/// <see cref="BitColorPickerParams"/>, so an application usually sets it once rather than per picker.
/// <br />
/// The <c>Format</c> properties are <see cref="string.Format(string, object?[])"/> templates, so a
/// translation is free to reorder what they interpolate - or to leave a placeholder out, which is the way
/// to drop <see cref="BitColorPicker.ColorDescription"/> from an announcement, since the color is named
/// from an English vocabulary.
/// </remarks>
public class BitColorPickerTexts
{
    /// <summary>
    /// The accessible name of the saturation-brightness area.
    /// </summary>
    public string SaturationAreaLabel { get; set; } = "Saturation and brightness";

    /// <summary>
    /// What the saturation-brightness area calls itself, through <c>aria-roledescription</c>, so that the
    /// two axes it is driven on are announced rather than the one axis its ARIA role has.
    /// </summary>
    public string SaturationAreaRoleDescription { get; set; } = "2D slider";

    /// <summary>
    /// What the saturation-brightness area announces its value as: the color said in words, the saturation
    /// and the brightness as percentages, and the hexadecimal value.
    /// <br />
    /// <c>{0}</c> the color description, <c>{1}</c> the saturation, <c>{2}</c> the brightness, <c>{3}</c> the hex.
    /// </summary>
    public string SaturationValueFormat { get; set; } = "{0}, Saturation {1}%, Brightness {2}%, {3}";

    /// <summary>
    /// The accessible name of the hue slider.
    /// </summary>
    public string HueLabel { get; set; } = "Hue";

    /// <summary>
    /// What the hue slider announces its value as. A bare number would be read as a position on an unnamed
    /// scale, so the unit the scale is in - degrees around the color wheel - is spelled out with it.
    /// <br />
    /// <c>{0}</c> the hue in degrees.
    /// </summary>
    public string HueValueFormat { get; set; } = "Hue {0} degrees";

    /// <summary>
    /// The accessible name of the alpha slider, and the tooltip of the alpha field.
    /// </summary>
    public string AlphaLabel { get; set; } = "Alpha";

    /// <summary>
    /// What the alpha slider announces its value as.
    /// <br />
    /// <c>{0}</c> the alpha as a percentage.
    /// </summary>
    public string AlphaValueFormat { get; set; } = "Alpha {0}%";

    /// <summary>
    /// The caption under the alpha percentage field, which has to fit the width of one field.
    /// </summary>
    public string AlphaFieldLabel { get; set; } = "A%";

    /// <summary>
    /// The caption under the hexadecimal field.
    /// </summary>
    public string HexFieldLabel { get; set; } = "Hex";

    /// <summary>
    /// The tooltip and accessible name of the red channel field.
    /// </summary>
    public string RedLabel { get; set; } = "Red";

    /// <summary>
    /// The caption under the red channel field, which has to fit the width of one field.
    /// </summary>
    public string RedFieldLabel { get; set; } = "R";

    /// <summary>
    /// The tooltip and accessible name of the green channel field.
    /// </summary>
    public string GreenLabel { get; set; } = "Green";

    /// <summary>
    /// The caption under the green channel field.
    /// </summary>
    public string GreenFieldLabel { get; set; } = "G";

    /// <summary>
    /// The tooltip and accessible name of the blue channel field.
    /// </summary>
    public string BlueLabel { get; set; } = "Blue";

    /// <summary>
    /// The caption under the blue channel field.
    /// </summary>
    public string BlueFieldLabel { get; set; } = "B";

    /// <summary>
    /// The caption under the hue channel field.
    /// </summary>
    public string HueFieldLabel { get; set; } = "H";

    /// <summary>
    /// The tooltip and accessible name of the saturation channel field.
    /// </summary>
    public string SaturationLabel { get; set; } = "Saturation";

    /// <summary>
    /// The caption under the saturation channel field.
    /// </summary>
    public string SaturationFieldLabel { get; set; } = "S";

    /// <summary>
    /// The tooltip and accessible name of the lightness channel field of the HSL mode.
    /// </summary>
    public string LightnessLabel { get; set; } = "Lightness";

    /// <summary>
    /// The caption under the lightness channel field.
    /// </summary>
    public string LightnessFieldLabel { get; set; } = "L";

    /// <summary>
    /// The tooltip and accessible name of the brightness channel field of the HSV mode.
    /// </summary>
    public string BrightnessLabel { get; set; } = "Brightness";

    /// <summary>
    /// The caption under the brightness channel field.
    /// </summary>
    public string BrightnessFieldLabel { get; set; } = "V";

    /// <summary>
    /// What the picker calls itself when it has neither an <see cref="BitComponentBase.AriaLabel"/> nor a
    /// <see cref="BitColorPicker.Label"/> to be named by: the color, named and then spelled out in channels.
    /// <br />
    /// <c>{0}</c> the color description, <c>{1}</c> red, <c>{2}</c> green, <c>{3}</c> blue.
    /// </summary>
    public string PickerLabelFormat { get; set; } = "Color picker, {0}, Red {1} Green {2} Blue {3} selected.";

    /// <summary>
    /// The same, for a picker whose alpha slider is shown.
    /// <br />
    /// <c>{0}</c> the color description, <c>{1}</c> red, <c>{2}</c> green, <c>{3}</c> blue, <c>{4}</c> the alpha as a percentage.
    /// </summary>
    public string PickerLabelWithAlphaFormat { get; set; } = "Color picker, {0}, Red {1} Green {2} Blue {3} and Alpha {4}% selected.";

    /// <summary>
    /// The tooltip and accessible name of the eye dropper button.
    /// </summary>
    public string EyeDropperLabel { get; set; } = "Pick a color from the screen";

    /// <summary>
    /// What the inputs mode switch calls the <see cref="BitColorInputsMode.HexRgb"/> set of fields.
    /// </summary>
    public string HexRgbModeLabel { get; set; } = "HEX & RGB";

    /// <summary>
    /// What the inputs mode switch calls the <see cref="BitColorInputsMode.Hex"/> set of fields.
    /// </summary>
    public string HexModeLabel { get; set; } = "HEX";

    /// <summary>
    /// What the inputs mode switch calls the <see cref="BitColorInputsMode.Rgb"/> set of fields.
    /// </summary>
    public string RgbModeLabel { get; set; } = "RGB";

    /// <summary>
    /// What the inputs mode switch calls the <see cref="BitColorInputsMode.Hsl"/> set of fields.
    /// </summary>
    public string HslModeLabel { get; set; } = "HSL";

    /// <summary>
    /// What the inputs mode switch calls the <see cref="BitColorInputsMode.Hsv"/> set of fields.
    /// </summary>
    public string HsvModeLabel { get; set; } = "HSV";

    /// <summary>
    /// The tooltip of the inputs mode switch, which says which set of channels the fields are currently in.
    /// <br />
    /// <c>{0}</c> the name of the current mode, which is the matching <c>...ModeLabel</c>.
    /// </summary>
    public string InputsModeFormat { get; set; } = "Color inputs: {0}";

    /// <summary>
    /// The accessible name of the inputs mode switch, which says what pressing it will do as well.
    /// <br />
    /// <c>{0}</c> the name of the current mode, which is the matching <c>...ModeLabel</c>.
    /// </summary>
    public string InputsModeSwitchFormat { get; set; } = "Color inputs: {0}. Switch to the next set.";

    /// <summary>
    /// The accessible name of the palette of preset swatches.
    /// </summary>
    public string PresetsLabel { get; set; } = "Color presets";

    /// <summary>
    /// The accessible name of one preset swatch: the color said in words, then the value it was written as.
    /// <br />
    /// <c>{0}</c> the color description, <c>{1}</c> the preset value as the consumer wrote it.
    /// </summary>
    public string PresetLabelFormat { get; set; } = "{0}, {1}";

    /// <summary>
    /// How the contrast ratio is read out, since "4.54:1" on its own is a pair of numbers with no unit.
    /// <br />
    /// <c>{0}</c> the ratio.
    /// </summary>
    public string ContrastRatioFormat { get; set; } = "Contrast ratio {0} to 1";

    /// <summary>
    /// The caption of the badge for normal text, which has to stay short enough to sit in the readout row.
    /// </summary>
    public string ContrastAaBadge { get; set; } = "AA";

    /// <summary>
    /// How that badge is read out.
    /// <br />
    /// <c>{0}</c> <see cref="ContrastPassLabel"/> or <see cref="ContrastFailLabel"/>.
    /// </summary>
    public string ContrastAaFormat { get; set; } = "WCAG AA for normal text: {0}";

    /// <summary>
    /// The caption of the badge for large text.
    /// </summary>
    public string ContrastAaLargeBadge { get; set; } = "AA Large";

    /// <summary>
    /// How that badge is read out.
    /// <br />
    /// <c>{0}</c> <see cref="ContrastPassLabel"/> or <see cref="ContrastFailLabel"/>.
    /// </summary>
    public string ContrastAaLargeFormat { get; set; } = "WCAG AA for large text: {0}";

    /// <summary>
    /// The verdict of a badge whose color clears the bar.
    /// </summary>
    public string ContrastPassLabel { get; set; } = "pass";

    /// <summary>
    /// The verdict of a badge whose color does not.
    /// </summary>
    public string ContrastFailLabel { get; set; } = "fail";
}
