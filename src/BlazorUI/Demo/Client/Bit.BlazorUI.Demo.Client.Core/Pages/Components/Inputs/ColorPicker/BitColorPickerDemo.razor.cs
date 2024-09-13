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
            Description = "Indicates the Alpha value.",
        },
        new()
        {
            Name = "Color",
            Type = "string",
            Description = "CSS-compatible string to describe the color.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitColorChangeEventArgs>",
            Description = "Callback for when the value changed.",
            LinkType = LinkType.Link,
            Href = "#color-change-event-args",
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
            Name = "ShowPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show color preview box.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "color-change-event-args",
            Title = "BitColorChangeEventArgs",
            Parameters = new()
            {
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
                    Description = "The alpha value of the changed color."
                },
            }
        }
    ];



    private string rgbColor = "rgb(255,255,255)";
    private string hexColor = "#FFFFFF";
    private string twoWayColor = "#FFFFFF";

    private string? changedColor;
    private double changedAlpha;

    private string boundColor = "#FFFFFF";
    private BitColorPicker colorPickerRef = default!;



    private readonly string example1RazorCode = @"
<BitColorPicker />";

    private readonly string example2RazorCode = @"
<BitColorPicker ShowAlphaSlider ShowPreview />";

    private readonly string example3RazorCode = @"
<BitColorPicker @bind-Color=""rgbColor"" />
<div>Color: @rgbColor</div>

<BitColorPicker @bind-Color=""hexColor"" />
<div>Color: @hexColor</div>

<BitColorPicker @bind-Color=""twoWayColor"" ShowAlphaSlider ShowPreview />
<BitTextField Label=""Enter Color (Hex or Rgb)"" @bind-Value=""twoWayColor"" Style=""width: 200px;"" />";
    private readonly string example3CsharpCode = @"
private string rgbColor = ""rgb(255,255,255)"";
private string hexColor = ""#FFFFFF"";
private string twoWayColor = ""#FFFFFF"";";

    private readonly string example4RazorCode = @"
<BitColorPicker OnChange=""v => (changedColor, changedAlpha) = v"" ShowAlphaSlider />
<div>Color: @changedColor</div>
<div>Alpha: @changedAlpha</div>";
    private readonly string example4CsharpCode = @"
private string? changedColor;
private double changedAlpha;";

    private readonly string example5RazorCode = @"
<BitColorPicker @ref=""colorPickerRef"" @bind-Color=""boundColor"" ShowAlphaSlider ShowPreview />
<div>Color: @boundColor</div>
<div>Hex: @colorPickerRef?.Hex</div>
<div>Rgb: @colorPickerRef?.Rgb</div>
<div>Rgba: @colorPickerRef?.Rgba</div>
<div>Hsv: @colorPickerRef?.Hsv</div>";
    private readonly string example5CsharpCode = @"
private string boundColor = ""#FFFFFF"";
private BitColorPicker colorPickerRef = default!;";

    private readonly string example6RazorCode = @"
<style>
    .custom-class {
        width: 100px;
        height: 250px;
    }
</style>


<BitColorPicker ShowAlphaSlider Style=""width: 250px; height: 150px;"" />

<BitColorPicker ShowAlphaSlider Class=""custom-class"" />";
}
