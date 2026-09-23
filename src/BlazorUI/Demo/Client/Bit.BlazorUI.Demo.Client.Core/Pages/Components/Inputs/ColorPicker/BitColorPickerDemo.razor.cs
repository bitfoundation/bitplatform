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
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the picker takes the focus on the first render, landing on the saturation-brightness area - or, on a picker built without it, on whichever of its controls comes first.",
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
            Description = "String describing the color. Hexadecimal in three, four, six or eight digits, rgb() and rgba(), hsl() and hsla(), hwb(), lab() and lch(), oklab() and oklch(), color(srgb ...), a CSS color keyword such as \"tomato\", and transparent are all understood, in both the comma-separated and the modern space-separated syntax, as are hsv() and hsva(), which are not CSS notations but the model the picker itself is built on.",
        },
        new()
        {
            Name = "ContrastColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The color the contrast readout measures the picked color against - the background it is going to be read on. It accepts any of the notations Color does, and defaults to white.",
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
            Name = "InputsMode",
            Type = "BitColorInputsMode",
            DefaultValue = "BitColorInputsMode.HexRgb",
            Description = "Which channels the text fields are written in. It decides how the color is typed, not how it is published - a picker edited in HSL still answers in whatever Format says.",
            LinkType = LinkType.Link,
            Href = "#color-inputs-mode-enum",
        },
        new()
        {
            Name = "InputsModeSwitchIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the inputs mode switch button, using custom CSS classes for external icon libraries. Takes precedence over InputsModeSwitchIconName when both are set.",
        },
        new()
        {
            Name = "InputsModeSwitchIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom icon name for the inputs mode switch button. If unset, default will be the Sort icon.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text that names the picker. It is not a label element: with no single input to point a \"for\" at, one would label nothing, so the panel is named through aria-labelledby instead.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom markup in place of the plain Label text, for when the name needs more than a string.",
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
            Name = "PresetsPerRow",
            Type = "int?",
            DefaultValue = "null",
            Description = "How many preset swatches are laid out per row. Left unset they simply wrap; setting it lays them out on a grid instead, which keeps a palette meant to be read in columns in the arrangement it was written in.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the color picker read-only: the value is still shown at full contrast and every control stays in the tab order, so the color can be read out, but nothing about it can be changed. The widgets that hold a value declare aria-readonly or the native readonly, and the buttons that would change the color declare aria-disabled.",
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
            Name = "ShowContrast",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the contrast readout: how far the picked color stands from the ContrastColor it will be read on, and whether that clears the WCAG bar for text.",
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
            Name = "ShowHueSlider",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether to show the hue slider. Turning it off pins the picker to one hue, which is what a tint picker for a brand color is.",
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
            Name = "ShowInputsModeSwitch",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the button that moves the text fields from one set of channels to the next, so the user can type the color in whichever model they are thinking in.",
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
            Name = "ShowSaturationArea",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether to show the saturation-brightness area. Turning it off is what makes a palette picker out of the presets, the text fields, or both; AutoFocus and FocusAsync then land on whichever control comes first.",
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
        new()
        {
            Name = "Texts",
            Type = "BitColorPickerTexts?",
            DefaultValue = "null",
            Description = "Every piece of text the picker writes for itself: the accessible names of its controls, the captions of its fields, and the sentences it announces the color with. This is the parameter that translates the component; an unset property keeps its English default.",
            LinkType = LinkType.Link,
            Href = "#color-picker-texts",
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
                new()
                {
                    Name = "Hwb",
                    Type = "(double Hue, double Whiteness, double Blackness)",
                    Description = "The changed color as hue (0-360), whiteness and blackness (both 0-1)."
                },
                new()
                {
                    Name = "Oklch",
                    Type = "(double Lightness, double Chroma, double Hue)",
                    Description = "The changed color as Oklab lightness (0-1), chroma (0 to about 0.4) and hue (0-360)."
                },
                new()
                {
                    Name = "ColorDescription",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The changed color said in words, e.g. \"light vibrant blue\"."
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
                new() { Name = "LabelContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the element the label is rendered into." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label text of the color picker." },
                new() { Name = "SaturationPicker", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the saturation-value area of the color picker." },
                new() { Name = "SaturationThumb", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the thumb of the saturation-value area." },
                new() { Name = "Content", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the row that holds the sliders, the eye dropper and the preview." },
                new() { Name = "Sliders", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the column that holds the hue and alpha sliders." },
                new() { Name = "HueSlider", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the track of the hue slider." },
                new() { Name = "AlphaSlider", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the track of the alpha slider." },
                new() { Name = "SliderInput", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the range inputs of both sliders." },
                new() { Name = "EyeDropper", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the eye dropper button." },
                new() { Name = "EyeDropperIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the eye dropper button." },
                new() { Name = "InputsModeSwitch", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the button that moves the text fields to the next set of channels." },
                new() { Name = "InputsModeSwitchIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the inputs mode switch button." },
                new() { Name = "Preview", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the color preview box." },
                new() { Name = "Inputs", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the row of the hex and channel text fields." },
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for a single field of the inputs row, label included." },
                new() { Name = "FieldInput", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text input of a single field." },
                new() { Name = "FieldLabel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the caption of a single field." },
                new() { Name = "Contrast", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the row that holds the contrast readout." },
                new() { Name = "ContrastRatio", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the contrast ratio itself." },
                new() { Name = "ContrastBadge", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for a pass/fail badge of the contrast readout." },
                new() { Name = "Presets", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the preset swatches." },
                new() { Name = "Preset", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for a single preset swatch." },
                new() { Name = "SelectedPreset", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the preset swatch of the current color, applied on top of Preset." },
            ]
        },
        new()
        {
            Id = "color-picker-texts",
            Title = "BitColorPickerTexts",
            Description = "Every piece of text the BitColorPicker writes for itself. The Format properties are string.Format templates, so a translation is free to reorder what they interpolate - or to leave a placeholder out, which is the way to drop the English ColorDescription from an announcement.",
            Parameters =
            [
                new() { Name = "SaturationAreaLabel", Type = "string", DefaultValue = "Saturation and brightness", Description = "The accessible name of the saturation-brightness area." },
                new() { Name = "SaturationAreaRoleDescription", Type = "string", DefaultValue = "2D slider", Description = "What that area calls itself through aria-roledescription, so that both axes it is driven on are announced." },
                new() { Name = "SaturationValueFormat", Type = "string", DefaultValue = "{0}, Saturation {1}%, Brightness {2}%, {3}", Description = "What that area announces its value as. {0} the color description, {1} the saturation, {2} the brightness, {3} the hex." },
                new() { Name = "HueLabel", Type = "string", DefaultValue = "Hue", Description = "The accessible name of the hue slider, and the tooltip of the hue channel field." },
                new() { Name = "HueValueFormat", Type = "string", DefaultValue = "Hue {0} degrees", Description = "What the hue slider announces its value as, unit included. {0} the hue in degrees." },
                new() { Name = "HueFieldLabel", Type = "string", DefaultValue = "H", Description = "The caption under the hue channel field, which has to fit the width of one field." },
                new() { Name = "AlphaLabel", Type = "string", DefaultValue = "Alpha", Description = "The accessible name of the alpha slider, and the tooltip of the alpha field." },
                new() { Name = "AlphaValueFormat", Type = "string", DefaultValue = "Alpha {0}%", Description = "What the alpha slider announces its value as. {0} the alpha as a percentage." },
                new() { Name = "AlphaFieldLabel", Type = "string", DefaultValue = "A%", Description = "The caption under the alpha percentage field." },
                new() { Name = "HexFieldLabel", Type = "string", DefaultValue = "Hex", Description = "The caption under the hexadecimal field." },
                new() { Name = "RedLabel", Type = "string", DefaultValue = "Red", Description = "The tooltip and accessible name of the red channel field." },
                new() { Name = "RedFieldLabel", Type = "string", DefaultValue = "R", Description = "The caption under the red channel field." },
                new() { Name = "GreenLabel", Type = "string", DefaultValue = "Green", Description = "The tooltip and accessible name of the green channel field." },
                new() { Name = "GreenFieldLabel", Type = "string", DefaultValue = "G", Description = "The caption under the green channel field." },
                new() { Name = "BlueLabel", Type = "string", DefaultValue = "Blue", Description = "The tooltip and accessible name of the blue channel field." },
                new() { Name = "BlueFieldLabel", Type = "string", DefaultValue = "B", Description = "The caption under the blue channel field." },
                new() { Name = "SaturationLabel", Type = "string", DefaultValue = "Saturation", Description = "The tooltip and accessible name of the saturation channel field." },
                new() { Name = "SaturationFieldLabel", Type = "string", DefaultValue = "S", Description = "The caption under the saturation channel field." },
                new() { Name = "LightnessLabel", Type = "string", DefaultValue = "Lightness", Description = "The tooltip and accessible name of the lightness channel field of the HSL mode." },
                new() { Name = "LightnessFieldLabel", Type = "string", DefaultValue = "L", Description = "The caption under the lightness channel field." },
                new() { Name = "BrightnessLabel", Type = "string", DefaultValue = "Brightness", Description = "The tooltip and accessible name of the brightness channel field of the HSV mode." },
                new() { Name = "BrightnessFieldLabel", Type = "string", DefaultValue = "V", Description = "The caption under the brightness channel field." },
                new() { Name = "PickerLabelFormat", Type = "string", DefaultValue = "Color picker, {0}, Red {1} Green {2} Blue {3} selected.", Description = "What the picker calls itself when it has neither an AriaLabel nor a Label. {0} the color description, {1} red, {2} green, {3} blue." },
                new() { Name = "PickerLabelWithAlphaFormat", Type = "string", DefaultValue = "Color picker, {0}, Red {1} Green {2} Blue {3} and Alpha {4}% selected.", Description = "The same, for a picker whose alpha slider is shown. {4} is the alpha as a percentage." },
                new() { Name = "EyeDropperLabel", Type = "string", DefaultValue = "Pick a color from the screen", Description = "The tooltip and accessible name of the eye dropper button." },
                new() { Name = "InputsModeFormat", Type = "string", DefaultValue = "Color inputs: {0}", Description = "The tooltip of the inputs mode switch. {0} the current BitColorInputsMode." },
                new() { Name = "InputsModeSwitchFormat", Type = "string", DefaultValue = "Color inputs: {0}. Switch to the next set.", Description = "The accessible name of the inputs mode switch, which says what pressing it will do as well." },
                new() { Name = "PresetsLabel", Type = "string", DefaultValue = "Color presets", Description = "The accessible name of the palette of preset swatches." },
                new() { Name = "PresetLabelFormat", Type = "string", DefaultValue = "{0}, {1}", Description = "The accessible name of one swatch. {0} the color description, {1} the preset value as it was written." },
                new() { Name = "ContrastRatioFormat", Type = "string", DefaultValue = "Contrast ratio {0} to 1", Description = "How the contrast ratio is read out, since the bare pair of numbers carries no unit. {0} the ratio." },
                new() { Name = "ContrastAaBadge", Type = "string", DefaultValue = "AA", Description = "The caption of the badge for normal text." },
                new() { Name = "ContrastAaFormat", Type = "string", DefaultValue = "WCAG AA for normal text: {0}", Description = "How that badge is read out. {0} the pass or fail label." },
                new() { Name = "ContrastAaLargeBadge", Type = "string", DefaultValue = "AA Large", Description = "The caption of the badge for large text." },
                new() { Name = "ContrastAaLargeFormat", Type = "string", DefaultValue = "WCAG AA for large text: {0}", Description = "How that badge is read out. {0} the pass or fail label." },
                new() { Name = "ContrastPassLabel", Type = "string", DefaultValue = "pass", Description = "The verdict of a badge whose color clears the bar." },
                new() { Name = "ContrastFailLabel", Type = "string", DefaultValue = "fail", Description = "The verdict of a badge whose color does not." },
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
                new() { Name = "Hwb", Description = "Functional HWB notation: hwb(0 0% 0%). CSS only defines the space-separated syntax for it, so that is the one written.", Value = "8" },
                new() { Name = "Hwba", Description = "Functional HWB notation with an alpha channel: hwb(0 0% 0% / 0.5). CSS has no hwba() function - the alpha is written into hwb() itself, after a slash.", Value = "9" },
                new() { Name = "Oklab", Description = "Functional Oklab notation: oklab(0.6279 0.2249 0.1258). Oklab is a perceptually uniform color space, so the same numeric step covers the same visual difference wherever it is taken.", Value = "10" },
                new() { Name = "Oklaba", Description = "Functional Oklab notation with an alpha channel: oklab(0.6279 0.2249 0.1258 / 0.5).", Value = "11" },
                new() { Name = "Oklch", Description = "Functional Oklch notation: oklch(0.6279 0.2577 29.23). It is the polar form of Oklab, and the notation modern design tokens are increasingly written in.", Value = "12" },
                new() { Name = "Oklcha", Description = "Functional Oklch notation with an alpha channel: oklch(0.6279 0.2577 29.23 / 0.5).", Value = "13" },
            ]
        },
        new()
        {
            Id = "color-inputs-mode-enum",
            Name = "BitColorInputsMode",
            Description = "Which channels the text fields of the color picker are written in. The mode only decides how the color is typed and read off, not the value the picker publishes - that is what BitColorFormat is for.",
            Items =
            [
                new() { Name = "HexRgb", Description = "The hexadecimal field alongside the three Red-Green-Blue channels, which is the pair most color pickers show together.", Value = "0" },
                new() { Name = "Hex", Description = "The hexadecimal field on its own.", Value = "1" },
                new() { Name = "Rgb", Description = "The Red, Green and Blue channels, each from 0 to 255.", Value = "2" },
                new() { Name = "Hsl", Description = "Hue in degrees, saturation and lightness as percentages.", Value = "3" },
                new() { Name = "Hsv", Description = "Hue in degrees, saturation and brightness as percentages - the model the picker itself is driven in.", Value = "4" },
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



    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Hex",
            Type = "string",
            Description = "The current color in six-digit hexadecimal notation, e.g. #FF0000.",
        },
        new()
        {
            Name = "HexAlpha",
            Type = "string",
            Description = "The current color in eight-digit hexadecimal notation, whose last pair is the alpha channel, e.g. #FF000080.",
        },
        new()
        {
            Name = "Rgb",
            Type = "string",
            Description = "The current color in functional RGB notation, e.g. rgb(255,0,0).",
        },
        new()
        {
            Name = "Rgba",
            Type = "string",
            Description = "The current color in functional RGB notation with its alpha channel, e.g. rgba(255,0,0,0.5).",
        },
        new()
        {
            Name = "Hsl",
            Type = "(double Hue, double Saturation, double Lightness)",
            Description = "The current color as hue (0-360), saturation and lightness (both 0-1).",
        },
        new()
        {
            Name = "Hsv",
            Type = "(double Hue, double Saturation, double Value)",
            Description = "The current color as hue (0-360), saturation and value (both 0-1).",
        },
        new()
        {
            Name = "Hwb",
            Type = "(double Hue, double Whiteness, double Blackness)",
            Description = "The current color as hue (0-360), whiteness and blackness (both 0-1).",
        },
        new()
        {
            Name = "Oklch",
            Type = "(double Lightness, double Chroma, double Hue)",
            Description = "The current color as Oklab lightness (0-1), chroma (0 to about 0.4) and hue (0-360).",
        },
        new()
        {
            Name = "ColorDescription",
            Type = "string",
            Description = "The current color said in words, e.g. light vibrant blue. It is what the picker announces to a screen reader, and what a page showing the color elsewhere usually names it with.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Moves the focus to the picker, landing on the saturation-brightness area - or, on a picker built without it, on whichever control comes first.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-ColorPicker-width",
            DefaultValue = "Per Size (33.5 spacing units at Medium)",
            Description = "Width of the whole panel. A size class never overrides it, so this is how a panel is fitted to a popover or a sidebar the Size presets do not suit.",
        },
        new()
        {
            Name = "--bit-ColorPicker-background",
            DefaultValue = "transparent",
            Description = "Background behind the panel, for a picker that has to read as a surface of its own rather than as part of the one it sits on.",
        },
        new()
        {
            Name = "--bit-ColorPicker-padding",
            DefaultValue = "0",
            Description = "Padding around the panel, usually set together with a background and a radius.",
        },
        new()
        {
            Name = "--bit-ColorPicker-radius",
            DefaultValue = "0",
            Description = "Corner radius of the panel itself. The parts inside it keep their own radii.",
        },
        new()
        {
            Name = "--bit-ColorPicker-gap",
            DefaultValue = "1 spacing unit",
            Description = "Vertical rhythm between the rows of the panel: under the gradient, under each slider, above the contrast readout and above the palette.",
        },
        new()
        {
            Name = "--bit-ColorPicker-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of the panel, which the label and the text fields inherit.",
        },
        new()
        {
            Name = "--bit-ColorPicker-caption-font-size",
            DefaultValue = "One step below the panel, per Size",
            Description = "Text size of the captions under the fields and of the contrast readout.",
        },
        new()
        {
            Name = "--bit-ColorPicker-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Color of every border the panel draws: the gradient, both tracks, the thumbs, the preview, the fields, the buttons and the swatches.",
        },
        new()
        {
            Name = "--bit-ColorPicker-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Color of the keyboard focus ring on every focusable part of the picker.",
        },
        new()
        {
            Name = "--bit-ColorPicker-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the Label text.",
        },
        new()
        {
            Name = "--bit-ColorPicker-saturation-height",
            DefaultValue = "Per Size (29.5 spacing units at Medium)",
            Description = "Height of the saturation-brightness area. It is also the row that gives way when the panel is pinned to a height smaller than its content.",
        },
        new()
        {
            Name = "--bit-ColorPicker-saturation-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the saturation-brightness area.",
        },
        new()
        {
            Name = "--bit-ColorPicker-thumb-size",
            DefaultValue = "Per Size (2.5 spacing units at Medium)",
            Description = "Diameter of the saturation thumb and of the thumbs of both sliders, which is also what reserves the room they overhang their tracks by.",
        },
        new()
        {
            Name = "--bit-ColorPicker-thumb-color",
            DefaultValue = "--bit-clr-ntr-white",
            Description = "Fill of the hue and alpha slider thumbs.",
        },
        new()
        {
            Name = "--bit-ColorPicker-thumb-ring-color",
            DefaultValue = "--bit-clr-ntr-white",
            Description = "Inner ring inside the saturation thumb, which is what keeps it readable over a dark area of the gradient the way the shadow keeps it readable over a light one.",
        },
        new()
        {
            Name = "--bit-ColorPicker-thumb-shadow",
            DefaultValue = "--bit-shd-nm",
            Description = "Elevation under the saturation thumb, which is what keeps it visible over a light area of the gradient.",
        },
        new()
        {
            Name = "--bit-ColorPicker-track-height",
            DefaultValue = "Per Size (2.5 spacing units at Medium)",
            Description = "Height of the hue and alpha tracks.",
        },
        new()
        {
            Name = "--bit-ColorPicker-track-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of both tracks.",
        },
        new()
        {
            Name = "--bit-ColorPicker-preview-size",
            DefaultValue = "Per Size (6 spacing units at Medium)",
            Description = "Width and height of the preview box.",
        },
        new()
        {
            Name = "--bit-ColorPicker-preview-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the preview box.",
        },
        new()
        {
            Name = "--bit-ColorPicker-button-size",
            DefaultValue = "Per Size (3 spacing units at Medium)",
            Description = "Square of the eye dropper and inputs-mode buttons. Keep it at or above 24px, which is the minimum pointer target WCAG 2.2 asks for.",
        },
        new()
        {
            Name = "--bit-ColorPicker-button-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Glyph color of those two buttons.",
        },
        new()
        {
            Name = "--bit-ColorPicker-button-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of those two buttons.",
        },
        new()
        {
            Name = "--bit-ColorPicker-button-hover-background",
            DefaultValue = "--bit-clr-bg-sec-hover",
            Description = "Background of those buttons on hover, on pointer devices only.",
        },
        new()
        {
            Name = "--bit-ColorPicker-button-active-background",
            DefaultValue = "--bit-clr-bg-sec-active",
            Description = "Background of those buttons while pressed.",
        },
        new()
        {
            Name = "--bit-ColorPicker-icon-size",
            DefaultValue = "Per Size (--bit-siz-icon-md at Medium)",
            Description = "Glyph size inside the eye dropper and inputs-mode buttons.",
        },
        new()
        {
            Name = "--bit-ColorPicker-field-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the hexadecimal and channel fields.",
        },
        new()
        {
            Name = "--bit-ColorPicker-field-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of those fields.",
        },
        new()
        {
            Name = "--bit-ColorPicker-field-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of those fields.",
        },
        new()
        {
            Name = "--bit-ColorPicker-field-label-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the caption under a field.",
        },
        new()
        {
            Name = "--bit-ColorPicker-swatch-size",
            DefaultValue = "Per Size (3 spacing units at Medium)",
            Description = "Square of a preset swatch, and the column width a PresetsPerRow grid is laid out on.",
        },
        new()
        {
            Name = "--bit-ColorPicker-swatch-radius",
            DefaultValue = "--bit-shp-radius-sm",
            Description = "Corner radius of a preset swatch. The ring marking the selected one inherits it, so round swatches get a round ring.",
        },
        new()
        {
            Name = "--bit-ColorPicker-swatch-gap",
            DefaultValue = "0.5 spacing units",
            Description = "Space between preset swatches, in both directions.",
        },
        new()
        {
            Name = "--bit-ColorPicker-swatch-ring-color",
            DefaultValue = "--bit-clr-ntr-white",
            Description = "Inner ring that marks the swatch the picker is currently on.",
        },
        new()
        {
            Name = "--bit-ColorPicker-swatch-ring-shadow",
            DefaultValue = "--bit-clr-ntr-black",
            Description = "Outer ring drawn around the inner one, which is what keeps the mark readable on a swatch of the inner ring's own color.",
        },
        new()
        {
            Name = "--bit-ColorPicker-checkerboard-size",
            DefaultValue = "0.75 spacing units (0.5 on a swatch)",
            Description = "Square of the grid that transparency is read against, under the alpha track, the preview box and the swatches.",
        },
        new()
        {
            Name = "--bit-ColorPicker-checkerboard-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Darker square of that grid.",
        },
        new()
        {
            Name = "--bit-ColorPicker-checkerboard-background",
            DefaultValue = "--bit-clr-ntr-white",
            Description = "Lighter square of that grid.",
        },
        new()
        {
            Name = "--bit-ColorPicker-contrast-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the contrast ratio in the readout.",
        },
        new()
        {
            Name = "--bit-ColorPicker-contrast-pass-color",
            DefaultValue = "--bit-clr-suc",
            Description = "Color of a badge whose color clears the WCAG bar. The badge also carries a check mark, so the verdict never rests on color alone.",
        },
        new()
        {
            Name = "--bit-ColorPicker-contrast-fail-color",
            DefaultValue = "--bit-clr-err",
            Description = "Color of a badge that does not clear it, which also carries a cross.",
        },
    ];



    private string alphaColor = "#4D8CB3";
    private double alphaValue = 0.5;
    private string previewColor = "#5B8C5A";

    private string inputsAlphaColor = "#4DB39980";
    private BitColorInputsMode inputsMode = BitColorInputsMode.HexRgb;
    private string inputsModeColor = "#B34D6B";
    private string hslInputsColor = "hsl(150,45%,45%)";

    private static readonly string[] brandPresets =
    [
        "#E24A4A", "#E2934A", "#E2D24A", "#7EE24A", "#4AE2C0",
        "#4A9BE2", "#7E4AE2", "#E24AC0", "#FFFFFF", "#8A8886", "#201F1E"
    ];
    private string presetColor = "#4A9BE2";

    private static readonly string[] alphaPresets =
    [
        "rgba(74,155,226,1)", "rgba(74,155,226,0.75)", "rgba(74,155,226,0.5)", "rgba(74,155,226,0.25)", "transparent"
    ];
    private string alphaPresetColor = "rgba(74,155,226,0.5)";

    private static readonly string[] rampPresets =
    [
        "#FDE7E7", "#F7B9B9", "#EE8080", "#E24A4A", "#B02F2F",
        "#E7F0FB", "#B9D3F2", "#80B0E8", "#4A9BE2", "#2F6BB0"
    ];
    private string rampPresetColor = "#4A9BE2";

    private string eyeDropperColor = "#5B8C5A";

    private string contrastColor = "#767676";
    private string contrastBackground = "#FFFFFF";
    private string contrastOnDarkColor = "rgba(122,200,255,1)";

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
    private BitColorPicker? colorPickerRef;

    private string accessibilityColor = "#4DB3B3";

    private string localizedColor = "#4DB3B3";
    private static readonly BitColorPickerTexts persianTexts = new()
    {
        SaturationAreaLabel = "اشباع و روشنایی",
        SaturationAreaRoleDescription = "لغزنده دو بعدی",
        SaturationValueFormat = "اشباع {1}٪، روشنایی {2}٪، {3}",
        HueLabel = "فام",
        HueValueFormat = "فام {0} درجه",
        HueFieldLabel = "ف",
        AlphaLabel = "شفافیت",
        AlphaValueFormat = "شفافیت {0}٪",
        AlphaFieldLabel = "ش٪",
        HexFieldLabel = "هگز",
        RedLabel = "قرمز",
        RedFieldLabel = "ق",
        GreenLabel = "سبز",
        GreenFieldLabel = "س",
        BlueLabel = "آبی",
        BlueFieldLabel = "آ",
        SaturationLabel = "اشباع",
        SaturationFieldLabel = "ا",
        LightnessLabel = "روشنی",
        LightnessFieldLabel = "ر",
        BrightnessLabel = "روشنایی",
        BrightnessFieldLabel = "ر",
        PickerLabelFormat = "انتخابگر رنگ، قرمز {1} سبز {2} آبی {3} انتخاب شد.",
        PickerLabelWithAlphaFormat = "انتخابگر رنگ، قرمز {1} سبز {2} آبی {3} و شفافیت {4}٪ انتخاب شد.",
        EyeDropperLabel = "برداشتن رنگ از صفحه",
        InputsModeFormat = "ورودی رنگ: {0}",
        InputsModeSwitchFormat = "ورودی رنگ: {0}. رفتن به مجموعه بعدی.",
        PresetsLabel = "رنگ های آماده",
        PresetLabelFormat = "{1}",
    };

    private string paletteOnlyColor = "#E24A4A";
    private string tintColor = "#4A9BE2";
    private string fieldsOnlyColor = "rgba(126,74,226,1)";

    private readonly BitColorPickerParams[] colorPickerParams =
    [
        new()
        {
            ShowInputs = true,
            ShowPreview = true,
            ShowAlphaSlider = true,
            Presets = brandPresets,
            PresetsPerRow = 6,
            Format = BitColorFormat.Rgba,
        }
    ];
    private string cascadedColor = "rgba(226,74,74,1)";
    private string cascadedOtherColor = "rgba(74,155,226,1)";
    private string cascadedSmallColor = "rgba(126,74,226,1)";

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
}
