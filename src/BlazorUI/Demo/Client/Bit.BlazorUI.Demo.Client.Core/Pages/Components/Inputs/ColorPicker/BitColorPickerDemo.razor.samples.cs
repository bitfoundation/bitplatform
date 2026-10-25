namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ColorPicker;

public partial class BitColorPickerDemo
{
    private readonly string example1RazorCode = @"
<BitColorPicker />

<BitColorPicker Label=""Brand color"" />

<BitColorPicker>
    <LabelTemplate>
        <BitIcon IconName=""@BitIconName.Color"" /> <b>Accent color</b>
    </LabelTemplate>
</BitColorPicker>

<BitColorPicker Disabled Value=""#B34D4D"" />

<BitColorPicker ReadOnly ShowInputs @bind-Value=""readOnlyColor"" />";
    private readonly string example1CsharpCode = @"
private string readOnlyColor = ""#4D7FB3"";";

    private readonly string example2RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowPreview @bind-Value=""alphaColor"" @bind-Alpha=""alphaValue"" />
<div>Color: @alphaColor &nbsp; Alpha: @alphaValue</div>

<BitColorPicker ShowPreview @bind-Value=""previewColor"" />
<div>Color: @previewColor</div>";
    private readonly string example2CsharpCode = @"
private string alphaColor = ""#4D8CB3"";
private double alphaValue = 0.5;
private string previewColor = ""#5B8C5A"";";

    private readonly string example3RazorCode = @"
<BitColorPicker ShowInputs ShowPreview ShowAlphaSlider
                Format=""BitColorFormat.HexAlpha""
                @bind-Value=""inputsAlphaColor"" />
<div>Color: @inputsAlphaColor</div>

<BitColorPicker ShowInputs ShowPreview ShowInputsModeSwitch
                @bind-InputsMode=""inputsMode"" @bind-Value=""inputsModeColor"" />
<div>Mode: @inputsMode &nbsp; Color: @inputsModeColor</div>

<BitColorPicker ShowInputs ShowPreview
                InputsMode=""BitColorInputsMode.Hsl""
                @bind-Value=""hslInputsColor"" />
<div>Color: @hslInputsColor</div>";
    private readonly string example3CsharpCode = @"
private string inputsAlphaColor = ""#4DB39980"";
private BitColorInputsMode inputsMode = BitColorInputsMode.HexRgb;
private string inputsModeColor = ""#B34D6B"";
private string hslInputsColor = ""hsl(150,45%,45%)"";";

    private readonly string example4RazorCode = @"
<BitColorPicker ShowPreview Presets=""brandPresets"" @bind-Value=""presetColor"" />
<div>Color: @presetColor</div>

<BitColorPicker ShowPreview ShowAlphaSlider
                Presets=""alphaPresets""
                Format=""BitColorFormat.Rgba""
                @bind-Value=""alphaPresetColor"" />
<div>Color: @alphaPresetColor</div>

<BitColorPicker ShowPreview Presets=""rampPresets"" PresetsPerRow=""5"" @bind-Value=""rampPresetColor"" />
<div>Color: @rampPresetColor</div>";
    private readonly string example4CsharpCode = @"
private static readonly string[] brandPresets =
[
    ""#E24A4A"", ""#E2934A"", ""#E2D24A"", ""#7EE24A"", ""#4AE2C0"",
    ""#4A9BE2"", ""#7E4AE2"", ""#E24AC0"", ""#FFFFFF"", ""#8A8886"", ""#201F1E""
];
private string presetColor = ""#4A9BE2"";

private static readonly string[] alphaPresets =
[
    ""rgba(74,155,226,1)"", ""rgba(74,155,226,0.75)"", ""rgba(74,155,226,0.5)"", ""rgba(74,155,226,0.25)"", ""transparent""
];
private string alphaPresetColor = ""rgba(74,155,226,0.5)"";

private static readonly string[] rampPresets =
[
    ""#FDE7E7"", ""#F7B9B9"", ""#EE8080"", ""#E24A4A"", ""#B02F2F"",
    ""#E7F0FB"", ""#B9D3F2"", ""#80B0E8"", ""#4A9BE2"", ""#2F6BB0""
];
private string rampPresetColor = ""#4A9BE2"";";

    private readonly string example5RazorCode = @"
<BitColorPicker ShowEyeDropper ShowPreview ShowInputs @bind-Value=""eyeDropperColor"" />
<div>Color: @eyeDropperColor</div>";
    private readonly string example5CsharpCode = @"
private string eyeDropperColor = ""#5B8C5A"";";

    private readonly string example6RazorCode = @"
<BitColorPicker ShowContrast ShowInputs ShowPreview
                ContrastColor=""@contrastBackground""
                @bind-Value=""contrastColor"" />
<div>@contrastColor on @contrastBackground</div>

<BitColorPicker ShowPreview ShowInputs @bind-Value=""contrastBackground"" />

<BitColorPicker ShowContrast ShowAlphaSlider ShowPreview
                ContrastColor=""#1B1A19""
                Format=""BitColorFormat.Rgba""
                @bind-Value=""contrastOnDarkColor"" />
<div>Color: @contrastOnDarkColor</div>";
    private readonly string example6CsharpCode = @"
private string contrastColor = ""#767676"";
private string contrastBackground = ""#FFFFFF"";
private string contrastOnDarkColor = ""rgba(122,200,255,1)"";";

    private readonly string example7RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowPreview Format=""selectedFormat"" @bind-Value=""formatColor"" />
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
    <BitChoiceGroupOption Text=""Hwb"" Value=""BitColorFormat.Hwb"" />
    <BitChoiceGroupOption Text=""Hwba"" Value=""BitColorFormat.Hwba"" />
    <BitChoiceGroupOption Text=""Oklab"" Value=""BitColorFormat.Oklab"" />
    <BitChoiceGroupOption Text=""Oklaba"" Value=""BitColorFormat.Oklaba"" />
    <BitChoiceGroupOption Text=""Oklch"" Value=""BitColorFormat.Oklch"" />
    <BitChoiceGroupOption Text=""Oklcha"" Value=""BitColorFormat.Oklcha"" />
</BitChoiceGroup>";
    private readonly string example7CsharpCode = @"
private BitColorFormat selectedFormat = BitColorFormat.Hex;
private string formatColor = ""#B34D8C"";";

    private readonly string example8RazorCode = @"
<BitColorPicker Value=""@oneWayColor"" ShowPreview />
@foreach (var (label, color) in oneWayOptions)
{
    <BitButton OnClick=""() => oneWayColor = color"">@label</BitButton>
}

<BitColorPicker @bind-Value=""twoWayColor"" @bind-Alpha=""twoWayAlpha"" ShowAlphaSlider ShowPreview />
<BitTextField Label=""Enter a color"" @bind-Value=""twoWayColor"" Style=""width: 220px;"" />
<div>Alpha: @twoWayAlpha</div>";
    private readonly string example8CsharpCode = @"
private readonly (string Label, string Color)[] oneWayOptions =
[
    (""Red"", ""#E24A4A""), (""Green"", ""#4AE27E""), (""Blue"", ""#4A7FE2"")
];
private string oneWayColor = ""#E24A4A"";
private string twoWayColor = ""#4A9BE2"";
private double twoWayAlpha = 1;";

    private readonly string example9RazorCode = @"
<BitColorPicker @ref=""colorPickerRef"" ShowAlphaSlider ShowPreview
                OnColorChange=""HandleOnColorChange""
                OnChangeEnd=""HandleOnChangeEnd"" />
<div>OnColorChange: @changeCount times, last @changedColor</div>
<div>OnChangeEnd: @changeEndCount times, last @changedHex / @changedRgba</div>
<BitButton OnClick=""() => colorPickerRef?.FocusAsync()"">Focus the picker</BitButton>

<div>ColorDescription: @colorPickerRef?.ColorDescription</div>
<div>Hex / HexAlpha: @colorPickerRef?.Hex / @colorPickerRef?.HexAlpha</div>
<div>Rgb / Rgba: @colorPickerRef?.Rgb / @colorPickerRef?.Rgba</div>
<div>Hsl: @colorPickerRef?.Hsl</div>
<div>Hsv: @colorPickerRef?.Hsv</div>
<div>Hwb: @colorPickerRef?.Hwb</div>
<div>Oklch: @colorPickerRef?.Oklch</div>";
    private readonly string example9CsharpCode = @"
private int changeCount;
private int changeEndCount;
private string? changedColor;
private string? changedHex;
private string? changedRgba;
private BitColorPicker? colorPickerRef;

private void HandleOnColorChange(BitColorChangeEventArgs args)
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

    private readonly string example10RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowInputs ShowPreview
                AriaLabel=""Choose the brand color""
                @bind-Value=""accessibilityColor"" />
<div>Color: @accessibilityColor</div>

<BitColorPicker ReadOnly ShowAlphaSlider ShowInputs ShowPreview
                Presets=""brandPresets""
                @bind-Value=""accessibilityColor"" />

<BitColorPicker Dir=""BitDir.Rtl"" ShowAlphaSlider ShowInputs ShowPreview ShowInputsModeSwitch
                Texts=""persianTexts""
                Label=""رنگ برند""
                Presets=""brandPresets""
                @bind-Value=""localizedColor"" />";
    private readonly string example10CsharpCode = @"
private string accessibilityColor = ""#4DB3B3"";

private string localizedColor = ""#4DB3B3"";
private static readonly BitColorPickerTexts persianTexts = new()
{
    SaturationAreaLabel = ""اشباع و روشنایی"",
    SaturationAreaRoleDescription = ""لغزنده دو بعدی"",
    // The color description is written from an English vocabulary, so this translation simply
    // leaves the {0} it would have been interpolated into out of the sentence.
    SaturationValueFormat = ""اشباع {1}٪، روشنایی {2}٪، {3}"",
    HueLabel = ""فام"",
    HueValueFormat = ""فام {0} درجه"",
    HueFieldLabel = ""ف"",
    AlphaLabel = ""شفافیت"",
    AlphaValueFormat = ""شفافیت {0}٪"",
    AlphaFieldLabel = ""ش٪"",
    HexFieldLabel = ""هگز"",
    RedLabel = ""قرمز"",
    RedFieldLabel = ""ق"",
    GreenLabel = ""سبز"",
    GreenFieldLabel = ""س"",
    BlueLabel = ""آبی"",
    BlueFieldLabel = ""آ"",
    PickerLabelFormat = ""انتخابگر رنگ، قرمز {1} سبز {2} آبی {3} انتخاب شد."",
    PickerLabelWithAlphaFormat = ""انتخابگر رنگ، قرمز {1} سبز {2} آبی {3} و شفافیت {4}٪ انتخاب شد."",
    EyeDropperLabel = ""برداشتن رنگ از صفحه"",
    InputsModeFormat = ""ورودی رنگ: {0}"",
    InputsModeSwitchFormat = ""ورودی رنگ: {0}. رفتن به مجموعه بعدی."",
    PresetsLabel = ""رنگ های آماده"",
    PresetLabelFormat = ""{1}"",
};";

    private readonly string example11RazorCode = @"
<BitColorPicker ShowSaturationArea=""false"" ShowHueSlider=""false"" ShowPreview
                Presets=""brandPresets""
                PresetsPerRow=""5""
                @bind-Value=""paletteOnlyColor"" />
<div>Color: @paletteOnlyColor</div>

<BitColorPicker ShowHueSlider=""false"" ShowPreview ShowInputs @bind-Value=""tintColor"" />
<div>Color: @tintColor</div>

<BitColorPicker ShowSaturationArea=""false"" ShowHueSlider=""false"" ShowInputs ShowInputsModeSwitch ShowPreview ShowAlphaSlider
                @bind-Value=""fieldsOnlyColor"" />
<div>Color: @fieldsOnlyColor</div>

<BitCallout AutoFocus>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"" AriaLabel=""@($""Text color, {popoverColor}"")"">
            <span style=""display:inline-block;width:1rem;height:1rem;border:1px solid;border-radius:2px;background:@popoverColor""></span>
            &nbsp;@popoverColor
        </BitButton>
    </Anchor>
    <Content>
        <BitColorPicker ShowInputs Presets=""brandPresets""
                        Label=""Text color""
                        Style=""--bit-ColorPicker-padding: 0.75rem;""
                        @bind-Value=""popoverColor"" />
    </Content>
</BitCallout>";
    private readonly string example11CsharpCode = @"
private static readonly string[] brandPresets =
[
    ""#E24A4A"", ""#E2934A"", ""#E2D24A"", ""#7EE24A"", ""#4AE2C0"",
    ""#4A9BE2"", ""#7E4AE2"", ""#E24AC0"", ""#FFFFFF"", ""#8A8886"", ""#201F1E""
];

private string paletteOnlyColor = ""#E24A4A"";
private string tintColor = ""#4A9BE2"";
private string fieldsOnlyColor = ""rgba(126,74,226,1)"";
private string popoverColor = ""#4A9BE2"";";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""@colorPickerParams"">
    <BitColorPicker @bind-Value=""cascadedColor"" />
    <div>Color: @cascadedColor</div>

    <BitColorPicker @bind-Value=""cascadedOtherColor"" />
    <div>Color: @cascadedOtherColor</div>

    <BitColorPicker Size=""BitSize.Small"" @bind-Value=""cascadedSmallColor"" />
</BitParams>


<BitColorPicker @bind-Value=""cascadedSmallColor"" />";
    private readonly string example12CsharpCode = @"
private readonly BitColorPickerParams[] colorPickerParams =
[
    new()
    {
        ShowInputs = true,
        ShowPreview = true,
        ShowAlphaSlider = true,
        ShowInputsModeSwitch = true,
        DefaultInputsMode = BitColorInputsMode.Hsl,
        Presets = brandPresets,
        PresetsPerRow = 6,
        Format = BitColorFormat.Rgba,
    }
];
private string cascadedColor = ""rgba(226,74,74,1)"";
private string cascadedOtherColor = ""rgba(74,155,226,1)"";
private string cascadedSmallColor = ""rgba(126,74,226,1)"";";

    private readonly string example13RazorCode = @"
<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitColorPicker Required ShowInputs ShowPreview
                    Label=""Brand color""
                    @bind-Value=""validationModel.Color"" />
    <ValidationMessage For=""@(() => validationModel.Color)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>
<div>@validationResult</div>";
    private readonly string example13CsharpCode = @"
public class BitColorPickerDemoFormModel
{
    [Required(ErrorMessage = ""Pick a brand color"")]
    public string? Color { get; set; }
}

private BitColorPickerDemoFormModel validationModel = new();
private string? validationResult;

private void HandleValidSubmit()
{
    validationResult = $""Submitted {validationModel.Color}"";
}

private void HandleInvalidSubmit()
{
    validationResult = null;
}";
    private const string example13ScssCode = @"
.validation-message {
    color: $bit-color-error;
}";
    private readonly DemoCodeFile[] example13CodeFiles =
    [
        new("BitColorPickerDemo.razor.scss", example13ScssCode),
    ];

    private readonly string example14RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitColorPicker ShowEyeDropper ShowInputs ShowInputsModeSwitch
                EyeDropperIconName=""@BitIconName.Color""
                InputsModeSwitchIconName=""@BitIconName.Switch"" />

<BitColorPicker ShowEyeDropper ShowInputs ShowInputsModeSwitch
                EyeDropperIcon=""@BitIconInfo.Fa(""solid eye-dropper"")""
                InputsModeSwitchIcon=""@BitIconInfo.Fa(""solid right-left"")"" />

<BitColorPicker ShowEyeDropper ShowInputs ShowInputsModeSwitch
                EyeDropperIcon=""@BitIconInfo.Bi(""eyedropper"")""
                InputsModeSwitchIcon=""@BitIconInfo.Bi(""arrow-left-right"")"" />";

    private readonly string example15RazorCode = @"
<BitColorPicker Size=""BitSize.Small"" ShowAlphaSlider ShowInputs ShowPreview
                Presets=""brandPresets"" @bind-Value=""smallColor"" />

<BitColorPicker Size=""BitSize.Medium"" ShowAlphaSlider ShowInputs ShowPreview
                Presets=""brandPresets"" @bind-Value=""mediumColor"" />

<BitColorPicker Size=""BitSize.Large"" ShowAlphaSlider ShowInputs ShowPreview
                Presets=""brandPresets"" @bind-Value=""largeColor"" />";
    private readonly string example15CsharpCode = @"
private string smallColor = ""#C25E5E"";
private string mediumColor = ""#5EC27A"";
private string largeColor = ""#5E7AC2"";";

    private readonly string example16RazorCode = @"
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
        --bit-ColorPicker-swatch-ring-color: blueviolet;
    }

    .themed-pickers {
        gap: 1rem;
        display: flex;
        flex-wrap: wrap;
        --bit-ColorPicker-radius: 0.75rem;
        --bit-ColorPicker-padding: 0.75rem;
        --bit-ColorPicker-background: var(--bit-clr-bg-sec);
        --bit-ColorPicker-border-color: #7E4AE2;
        --bit-ColorPicker-focus-color: #7E4AE2;
        --bit-ColorPicker-field-background: var(--bit-clr-bg-pri);
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
                                   SelectedPreset = ""custom-preset-selected"" })"" />

<BitColorPicker ShowAlphaSlider ShowInputs ShowPreview Presets=""brandPresets""
                Style=""--bit-ColorPicker-width: 18rem;
                       --bit-ColorPicker-saturation-height: 8rem;
                       --bit-ColorPicker-saturation-radius: 1rem;
                       --bit-ColorPicker-track-height: 0.75rem;
                       --bit-ColorPicker-thumb-size: 1.25rem;
                       --bit-ColorPicker-swatch-size: 1.25rem;
                       --bit-ColorPicker-swatch-radius: 50%;
                       --bit-ColorPicker-border-color: #7E4AE2;
                       --bit-ColorPicker-focus-color: #7E4AE2;"" />

<div class=""themed-pickers"">
    <BitColorPicker ShowInputs ShowPreview Presets=""brandPresets"" />
    <BitColorPicker ShowInputs ShowPreview Size=""BitSize.Small"" />
</div>";

    private readonly string example17RazorCode = @"
<BitColorPicker Dir=""BitDir.Rtl"" ShowAlphaSlider ShowPreview ShowInputs
                Presets=""brandPresets"" @bind-Value=""rtlColor"" />
<div>@rtlColor</div>";
}
