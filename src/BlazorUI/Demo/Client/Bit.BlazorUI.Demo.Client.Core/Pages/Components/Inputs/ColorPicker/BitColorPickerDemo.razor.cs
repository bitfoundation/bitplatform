namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ColorPicker;

public partial class BitColorPickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alpha",
            Type = "double",
            DefaultValue = "1",
            Description = "Indicates the Alpha value, from 0 (fully transparent) to 1 (fully opaque). The alpha is tracked whether or not ShowAlphaSlider renders a control for it, and a color string that carries its own alpha overrides this parameter.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitColorPickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitColorPicker.",
            LinkType = LinkType.Link,
            Href = "#color-picker-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "string",
            DefaultValue = "rgb(255,255,255)",
            Description = "String describing the color. Hexadecimal in three, four, six or eight digits, rgb() and rgba(), hsl() and hsla(), a CSS color keyword such as \"tomato\", and transparent are all understood, in both the comma-separated and the modern space-separated syntax, as are hsv() and hsva(), which are not CSS notations but the model the picker itself is built on.",
        },
        new()
        {
            Name = "EyeDropperIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the eye dropper button, using custom CSS classes for external icon libraries. Takes precedence over EyeDropperIconName when both are set.",
        },
        new()
        {
            Name = "EyeDropperIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom icon name for the eye dropper button. If unset, default will be the Eyedropper icon.",
        },
        new()
        {
            Name = "Format",
            Type = "BitColorFormat?",
            DefaultValue = "null",
            Description = "The notation the color value is written in, CSS or the non-CSS hsv() and hsva(). When left unset the picker answers in the same notation the Color arrived in.",
            LinkType = LinkType.Link,
            Href = "#color-format-enum",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitColorChangeEventArgs>",
            Description = "Callback for when the value changed. It fires on every step of a drag.",
            LinkType = LinkType.Link,
            Href = "#color-change-event-args",
        },
        new()
        {
            Name = "OnChangeEnd",
            Type = "EventCallback<BitColorChangeEventArgs>",
            Description = "Callback for when the user finishes changing the value: the drag ends, the slider is released, a text field is committed, or a preset is picked.",
            LinkType = LinkType.Link,
            Href = "#color-change-event-args",
        },
        new()
        {
            Name = "Presets",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The colors offered as a row of one-click swatches under the picker, in any of the notations the Color parameter accepts. A swatch that carries its own alpha applies that alpha too.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the color picker read-only: the value is still shown at full contrast, but nothing about it can be changed.",
        },
        new()
        {
            Name = "ShowAlphaSlider",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show a slider for editing alpha value.",
        },
        new()
        {
            Name = "ShowEyeDropper",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the button that opens the browser's eyedropper to sample a color from anywhere on the screen. The button is only rendered where the browser actually provides one.",
        },
        new()
        {
            Name = "ShowInputs",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the hexadecimal and Red-Green-Blue text fields, which is how an exact color is entered or read off without hunting for it on the gradient.",
        },
        new()
        {
            Name = "ShowPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show color preview box.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the color picker.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitColorPickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitColorPicker.",
            LinkType = LinkType.Link,
            Href = "#color-picker-class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "color-change-event-args",
            Title = "BitColorChangeEventArgs",
            Description = "Describes the color the picker has just moved to, in every notation at once.",
            Parameters =
            [
                new()
                {
                    Name = "Color",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The main color value of the changed color in the same format as the Color parameter of the ColorPicker."
                },
                new()
                {
                    Name = "Alpha",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The alpha value of the changed color, from 0 (fully transparent) to 1 (fully opaque)."
                },
                new()
                {
                    Name = "Hex",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The changed color in six-digit hexadecimal notation, e.g. #FF0000."
                },
                new()
                {
                    Name = "HexAlpha",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The changed color in eight-digit hexadecimal notation, whose last pair is the alpha channel, e.g. #FF000080."
                },
                new()
                {
                    Name = "Rgb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The changed color in functional RGB notation, e.g. rgb(255,0,0)."
                },
                new()
                {
                    Name = "Rgba",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The changed color in functional RGB notation with its alpha channel, e.g. rgba(255,0,0,0.5)."
                },
                new()
                {
                    Name = "Hsl",
                    Type = "(double Hue, double Saturation, double Lightness)",
                    Description = "The changed color as hue (0-360), saturation and lightness (both 0-1)."
                },
                new()
                {
                    Name = "Hsv",
                    Type = "(double Hue, double Saturation, double Value)",
                    Description = "The changed color as hue (0-360), saturation and value (both 0-1)."
                },
            ]
        },
        new()
        {
            Id = "color-picker-class-styles",
            Title = "BitColorPickerClassStyles",
            Description = "Custom CSS classes/styles for the parts of the BitColorPicker.",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the color picker." },
                new() { Name = "SaturationPicker", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the saturation-value area of the color picker." },
                new() { Name = "SaturationThumb", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the thumb of the saturation-value area." },
                new() { Name = "Content", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the row that holds the sliders, the eye dropper and the preview." },
                new() { Name = "Sliders", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the column that holds the hue and alpha sliders." },
                new() { Name = "HueSlider", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the track of the hue slider." },
                new() { Name = "AlphaSlider", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the track of the alpha slider." },
                new() { Name = "SliderInput", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the range inputs of both sliders." },
                new() { Name = "EyeDropper", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the eye dropper button." },
                new() { Name = "EyeDropperIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the eye dropper button." },
                new() { Name = "Preview", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the color preview box." },
                new() { Name = "Inputs", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the row of the hex and channel text fields." },
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for a single field of the inputs row, label included." },
                new() { Name = "FieldInput", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text input of a single field." },
                new() { Name = "FieldLabel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the caption of a single field." },
                new() { Name = "Presets", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the preset swatches." },
                new() { Name = "Preset", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for a single preset swatch." },
                new() { Name = "SelectedPreset", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the preset swatch of the current color, applied on top of Preset." },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-format-enum",
            Name = "BitColorFormat",
            Description = "The notation the color value is written in: the CSS ones, plus hsv() and hsva(), which no browser understands but the picker itself is built on. Every notation has an alpha-carrying twin: the plain ones drop the alpha from the string while the twins fold it in.",
            Items =
            [
                new() { Name = "Hex", Description = "Six-digit hexadecimal notation: #RRGGBB.", Value = "0" },
                new() { Name = "HexAlpha", Description = "Eight-digit hexadecimal notation, whose last pair is the alpha channel: #RRGGBBAA.", Value = "1" },
                new() { Name = "Rgb", Description = "Functional RGB notation: rgb(255,0,0).", Value = "2" },
                new() { Name = "Rgba", Description = "Functional RGB notation with an alpha channel: rgba(255,0,0,0.5).", Value = "3" },
                new() { Name = "Hsl", Description = "Functional HSL notation: hsl(0,100%,50%).", Value = "4" },
                new() { Name = "Hsla", Description = "Functional HSL notation with an alpha channel: hsla(0,100%,50%,0.5).", Value = "5" },
                new() { Name = "Hsv", Description = "Functional HSV notation: hsv(0,100%,100%). It is the model the picker itself is built on, but unlike the others it is not a notation any browser understands.", Value = "6" },
                new() { Name = "Hsva", Description = "Functional HSV notation with an alpha channel: hsva(0,100%,100%,0.5).", Value = "7" },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the size of the color picker.",
            Items =
            [
                new() { Name = "Small", Description = "Display the color picker using small size.", Value = "0" },
                new() { Name = "Medium", Description = "Display the color picker using medium size.", Value = "1" },
                new() { Name = "Large", Description = "Display the color picker using large size.", Value = "2" },
            ]
        }
    ];



    private string alphaColor = "#4D8CB3";
    private double alphaValue = 0.5;
    private string previewColor = "#5B8C5A";

    private string inputsColor = "#B3804D";
    private string inputsAlphaColor = "#4DB39980";

    private static readonly string[] brandPresets =
    [
        "#E24A4A", "#E2934A", "#E2D24A", "#7EE24A", "#4AE2C0",
        "#4A9BE2", "#7E4AE2", "#E24AC0", "#FFFFFF", "#8A8886", "#201F1E"
    ];
    private string presetColor = "#4A9BE2";

    private static readonly string[] alphaPresets =
    [
        "rgba(74,155,226,1)", "rgba(74,155,226,0.75)", "rgba(74,155,226,0.5)", "rgba(74,155,226,0.25)"
    ];
    private string alphaPresetColor = "rgba(74,155,226,0.5)";

    private string eyeDropperColor = "#5B8C5A";

    private BitColorFormat selectedFormat = BitColorFormat.Hex;
    private string formatColor = "#B34D8C";

    private readonly (string Label, string Color)[] oneWayOptions =
    [
        ("Red", "#E24A4A"), ("Green", "#4AE27E"), ("Blue", "#4A7FE2")
    ];
    private string oneWayColor = "#E24A4A";
    private string twoWayColor = "#4A9BE2";
    private double twoWayAlpha = 1;

    private int changeCount;
    private int changeEndCount;
    private string? changedColor;
    private string? changedHex;
    private string? changedRgba;

    private string boundColor = "#B3B34D";
    private BitColorPicker? colorPickerRef;

    private string accessibilityColor = "#4DB3B3";

    private string smallColor = "#C25E5E";
    private string mediumColor = "#5EC27A";
    private string largeColor = "#5E7AC2";

    private string rtlColor = "#B34D4D";



    private void HandleOnChange(BitColorChangeEventArgs args)
    {
        changeCount++;
        changedColor = args.Color;
    }

    private void HandleOnChangeEnd(BitColorChangeEventArgs args)
    {
        changeEndCount++;
        changedHex = args.Hex;
        changedRgba = args.Rgba;
    }



    private readonly string example1RazorCode = @"
<BitColorPicker />

<BitColorPicker IsEnabled=""false"" Color=""#B34D4D"" />

<BitColorPicker ReadOnly Color=""#4D7FB3"" />";

    private readonly string example2RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowPreview @bind-Color=""alphaColor"" @bind-Alpha=""alphaValue"" />
<div>Color: @alphaColor &nbsp; Alpha: @alphaValue</div>

<BitColorPicker ShowPreview @bind-Color=""previewColor"" />
<div>Color: @previewColor</div>";
    private readonly string example2CsharpCode = @"
private string alphaColor = ""#4D8CB3"";
private double alphaValue = 0.5;
private string previewColor = ""#5B8C5A"";";

    private readonly string example3RazorCode = @"
<BitColorPicker ShowInputs ShowPreview @bind-Color=""inputsColor"" />
<div>Color: @inputsColor</div>

<BitColorPicker ShowInputs ShowPreview ShowAlphaSlider
                Format=""BitColorFormat.HexAlpha""
                @bind-Color=""inputsAlphaColor"" />
<div>Color: @inputsAlphaColor</div>";
    private readonly string example3CsharpCode = @"
private string inputsColor = ""#B3804D"";
private string inputsAlphaColor = ""#4DB39980"";";

    private readonly string example4RazorCode = @"
<BitColorPicker ShowPreview Presets=""brandPresets"" @bind-Color=""presetColor"" />
<div>Color: @presetColor</div>

<BitColorPicker ShowPreview ShowAlphaSlider
                Presets=""alphaPresets""
                Format=""BitColorFormat.Rgba""
                @bind-Color=""alphaPresetColor"" />
<div>Color: @alphaPresetColor</div>";
    private readonly string example4CsharpCode = @"
private static readonly string[] brandPresets =
[
    ""#E24A4A"", ""#E2934A"", ""#E2D24A"", ""#7EE24A"", ""#4AE2C0"",
    ""#4A9BE2"", ""#7E4AE2"", ""#E24AC0"", ""#FFFFFF"", ""#8A8886"", ""#201F1E""
];
private string presetColor = ""#4A9BE2"";

private static readonly string[] alphaPresets =
[
    ""rgba(74,155,226,1)"", ""rgba(74,155,226,0.75)"", ""rgba(74,155,226,0.5)"", ""rgba(74,155,226,0.25)""
];
private string alphaPresetColor = ""rgba(74,155,226,0.5)"";";

    private readonly string example5RazorCode = @"
<BitColorPicker ShowEyeDropper ShowPreview ShowInputs @bind-Color=""eyeDropperColor"" />
<div>Color: @eyeDropperColor</div>";
    private readonly string example5CsharpCode = @"
private string eyeDropperColor = ""#5B8C5A"";";

    private readonly string example6RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowPreview Format=""selectedFormat"" @bind-Color=""formatColor"" />
<div>Color: @formatColor</div>

<BitChoiceGroup Horizontal
                Label=""Format""
                @bind-Value=""selectedFormat""
                TItem=""BitChoiceGroupOption<BitColorFormat>"" TValue=""BitColorFormat"">
    <BitChoiceGroupOption Text=""Hex"" Value=""BitColorFormat.Hex"" />
    <BitChoiceGroupOption Text=""HexAlpha"" Value=""BitColorFormat.HexAlpha"" />
    <BitChoiceGroupOption Text=""Rgb"" Value=""BitColorFormat.Rgb"" />
    <BitChoiceGroupOption Text=""Rgba"" Value=""BitColorFormat.Rgba"" />
    <BitChoiceGroupOption Text=""Hsl"" Value=""BitColorFormat.Hsl"" />
    <BitChoiceGroupOption Text=""Hsla"" Value=""BitColorFormat.Hsla"" />
    <BitChoiceGroupOption Text=""Hsv"" Value=""BitColorFormat.Hsv"" />
    <BitChoiceGroupOption Text=""Hsva"" Value=""BitColorFormat.Hsva"" />
</BitChoiceGroup>";
    private readonly string example6CsharpCode = @"
private BitColorFormat selectedFormat = BitColorFormat.Hex;
private string formatColor = ""#B34D8C"";";

    private readonly string example7RazorCode = @"
<BitColorPicker Color=""oneWayColor"" ShowPreview />
@foreach (var (label, color) in oneWayOptions)
{
    <BitButton OnClick=""() => oneWayColor = color"">@label</BitButton>
}

<BitColorPicker @bind-Color=""twoWayColor"" @bind-Alpha=""twoWayAlpha"" ShowAlphaSlider ShowPreview />
<BitTextField Label=""Enter a color"" @bind-Value=""twoWayColor"" Style=""width: 220px;"" />
<div>Alpha: @twoWayAlpha</div>";
    private readonly string example7CsharpCode = @"
private readonly (string Label, string Color)[] oneWayOptions =
[
    (""Red"", ""#E24A4A""), (""Green"", ""#4AE27E""), (""Blue"", ""#4A7FE2"")
];
private string oneWayColor = ""#E24A4A"";
private string twoWayColor = ""#4A9BE2"";
private double twoWayAlpha = 1;";

    private readonly string example8RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowPreview
                OnChange=""HandleOnChange""
                OnChangeEnd=""HandleOnChangeEnd"" />
<div>OnChange: @changeCount times, last @changedColor</div>
<div>OnChangeEnd: @changeEndCount times, last @changedHex / @changedRgba</div>";
    private readonly string example8CsharpCode = @"
private int changeCount;
private int changeEndCount;
private string? changedColor;
private string? changedHex;
private string? changedRgba;

private void HandleOnChange(BitColorChangeEventArgs args)
{
    changeCount++;
    changedColor = args.Color;
}

private void HandleOnChangeEnd(BitColorChangeEventArgs args)
{
    changeEndCount++;
    changedHex = args.Hex;
    changedRgba = args.Rgba;
}";

    private readonly string example9RazorCode = @"
<BitColorPicker @ref=""colorPickerRef"" @bind-Color=""boundColor"" ShowAlphaSlider ShowPreview />
<div>Color: @boundColor</div>
<div>Hex: @colorPickerRef?.Hex</div>
<div>HexAlpha: @colorPickerRef?.HexAlpha</div>
<div>Rgb: @colorPickerRef?.Rgb</div>
<div>Rgba: @colorPickerRef?.Rgba</div>
<div>Hsl: @colorPickerRef?.Hsl</div>
<div>Hsv: @colorPickerRef?.Hsv</div>";
    private readonly string example9CsharpCode = @"
private string boundColor = ""#B3B34D"";
private BitColorPicker? colorPickerRef;";

    private readonly string example10RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowInputs ShowPreview
                AriaLabel=""Choose the brand color""
                @bind-Color=""accessibilityColor"" />
<div>Color: @accessibilityColor</div>";
    private readonly string example10CsharpCode = @"
private string accessibilityColor = ""#4DB3B3"";";

    private readonly string example11RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitColorPicker ShowEyeDropper EyeDropperIconName=""@BitIconName.Color"" />

<BitColorPicker ShowEyeDropper EyeDropperIcon=""@BitIconInfo.Fa(""solid eye-dropper"")"" />

<BitColorPicker ShowEyeDropper EyeDropperIcon=""@BitIconInfo.Bi(""eyedropper"")"" />";

    private readonly string example12RazorCode = @"
<BitColorPicker Size=""BitSize.Small"" ShowAlphaSlider ShowPreview @bind-Color=""smallColor"" />

<BitColorPicker Size=""BitSize.Medium"" ShowAlphaSlider ShowPreview @bind-Color=""mediumColor"" />

<BitColorPicker Size=""BitSize.Large"" ShowAlphaSlider ShowPreview @bind-Color=""largeColor"" />";
    private readonly string example12CsharpCode = @"
private string smallColor = ""#C25E5E"";
private string mediumColor = ""#5EC27A"";
private string largeColor = ""#5E7AC2"";";

    private readonly string example13RazorCode = @"
<style>
    .custom-class {
        width: 100px;
        height: 250px;
    }

    .custom-field {
        color: blueviolet;
        border-color: blueviolet;
    }

    .custom-preset {
        border-radius: 50%;
    }

    .custom-preset-selected {
        --bit-clp-prt-sel-clr: blueviolet;
    }
</style>


<BitColorPicker ShowAlphaSlider Style=""width: 230px; height: 150px;"" />

<BitColorPicker ShowAlphaSlider Class=""custom-class"" />

<BitColorPicker ShowAlphaSlider ShowPreview ShowInputs
                Styles=""@(new() { SaturationPicker = ""border-radius: 1rem;"",
                                  SaturationThumb = ""width: 1.5rem; height: 1.5rem;"",
                                  Preview = ""border-radius: 50%;"" })"" />

<BitColorPicker ShowAlphaSlider ShowInputs Presets=""brandPresets""
                Classes=""@(new() { FieldInput = ""custom-field"",
                                   Preset = ""custom-preset"",
                                   SelectedPreset = ""custom-preset-selected"" })"" />";

    private readonly string example14RazorCode = @"
<BitColorPicker Dir=""BitDir.Rtl"" ShowAlphaSlider ShowPreview ShowInputs
                Presets=""brandPresets"" @bind-Color=""rtlColor"" />
<div>@rtlColor</div>";
}
