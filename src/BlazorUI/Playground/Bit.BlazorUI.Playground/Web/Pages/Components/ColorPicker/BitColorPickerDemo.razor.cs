using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ColorPicker;

public partial class BitColorPickerDemo
{
    private string BasicRgbColor = "rgb(255,255,255)";
    private string BasicHexColor = "#FFFFFF";

    private string AlphaRgbColor = "rgb(255,255,255)";
    private double Alpha = 1;

    private string TwoWayColor = "#FFFFFF";

    private BitColorValue ColorValue;

    private string BoundColor = "#FFFFFF";
    private BitColorPicker ColorPicker;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "Alpha",
            Type = "double",
            Description = "Indicates the Alpha value.",
        },
        new ComponentParameter()
        {
            Name = "Color",
            Type = "string",
            Description = "CSS-compatible string to describe the color.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<BitColorEventArgs>",
            Description = "Callback for when the value changed.",
        },
        new ComponentParameter()
        {
            Name = "ShowAlphaSlider",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show a slider for editing alpha value.",
        },
        new ComponentParameter()
        {
            Name = "ShowPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show color preview box.",
        },
    };

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<div class=""example"">
    <BitLabel>Rgb</BitLabel>
    <BitColorPicker @bind-Color=""BasicRgbColor"" />
    <span>Color: @BasicRgbColor</span>
</div>
<div class=""example"">
    <BitLabel>Hex</BitLabel>
    <BitColorPicker @bind-Color=""BasicHexColor"" />
    <span>Color: @BasicHexColor</span>
</div>
";

    private readonly string example1CSharpCode = @"
private string BasicRgbColor = ""rgb(255,255,255)"";
private string BasicHexColor = ""#FFFFFF"";
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<BitColorPicker @bind-Color=""AlphaRgbColor"" @bind-Alpha=""Alpha"" ShowAlphaSlider=""true"" />
<span>Color: @AlphaRgbColor</span>
<span>Alpha: @Alpha</span>
";

    private readonly string example2CSharpCode = @"
private string AlphaRgbColor = ""rgb(255,255,255)"";
private double Alpha = 1;
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<BitColorPicker ShowAlphaSlider=""true"" ShowPreview=""true"" />
";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"
<BitColorPicker @bind-Color=""TwoWayColor"" ShowPreview=""true"" />
<BitTextField Label=""Enter Color (Hex or Rgb)"" @bind-Value=""TwoWayColor"" Style=""width: 200px;"" />
";

    private readonly string example4CSharpCode = @"
private string TwoWayColor = ""#FFFFFF"";
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<BitColorPicker OnChange=""(value) => ColorValue = value"" ShowAlphaSlider=""true"" ShowPreview=""true"" />
<span>Color (Hex): @ColorValue?.Color</span>
<span>Alpha: @ColorValue?.Alpha</span>
";

    private readonly string example5CSharpCode = @"
private BitColorValue ColorValue;
";

    #endregion

    #region Sample Code 6

    private readonly string example6HTMLCode = @"
<BitColorPicker @ref=""ColorPicker"" @bind-Color=""BoundColor"" ShowAlphaSlider=""true"" ShowPreview=""true"" />
<span>Color Value: @BoundColor</span>
<span>Hex: @ColorPicker?.Hex</span>
<span>Rgb: @ColorPicker?.Rgb</span>
<span>Rgba: @ColorPicker?.Rgba</span>
<span>Hsv: @ColorPicker?.Hsv</span>
";

    private readonly string example6CSharpCode = @"
private string BoundColor = ""#FFFFFF"";
private BitColorPicker ColorPicker;
";

    #endregion

    #region Sample Code 7

    private readonly string example7HTMLCode = @"
<style>
    .custom-color-picker {
        width: 100px;
        height: 250px;
    }
</style>

<div>
    <BitLabel>Class</BitLabel>
    <BitColorPicker ShowAlphaSlider=""true"" Class=""custom-color-picker"" />
</div>
<div>
    <BitLabel>Style</BitLabel>
    <BitColorPicker ShowAlphaSlider=""true"" Style=""width: 250px; height: 150px;"" />
</div>
";

    #endregion
}
