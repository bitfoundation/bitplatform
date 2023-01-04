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
<div class=""column"">
    <BitColorPicker @bind-Color=""BasicRgbColor"" />
    <span>Rgb: @BasicRgbColor</span>
</div>
<div class=""column"">
    <BitColorPicker @bind-Color=""BasicHexColor"" />
    <span>Hex: @BasicHexColor</span>
</div>
";

    private readonly string example1CSharpCode = @"
private string BasicRgbColor = ""rgb(255,255,255)"";
private string BasicHexColor = ""#FFFFFF"";
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<div class=""column"">
    <BitColorPicker @bind-Color=""AlphaRgbColor"" @bind-Alpha=""Alpha"" ShowAlphaSlider=""true"" />
    <span>Rgb: @AlphaRgbColor</span>
    <span>Alpha: @Alpha</span>
</div>
";

    private readonly string example2CSharpCode = @"
private string AlphaRgbColor = ""rgb(255,255,255)"";
private double Alpha = 1;
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<div class=""column"">
    <BitColorPicker ShowPreview=""true"" />
</div>
";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"
<div class=""column"">
    <BitColorPicker @bind-Color=""TwoWayColor"" />
    <BitTextField Label=""Enter Hex or Rgb"" @bind-Value=""TwoWayColor"" />
</div>
";

    private readonly string example4CSharpCode = @"
private string TwoWayColor = ""#FFFFFF"";
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<div class=""column"">
    <BitLabel>OnChange</BitLabel>
    <BitColorPicker OnChange=""(value) => ColorValue = value"" ShowAlphaSlider=""true"" />
    <span>Color (Hex): @ColorValue?.Color</span>
    <span>Alpha: @ColorValue?.Alpha</span>
</div>
<div class=""column"">
    <BitLabel>Component Reference</BitLabel>
    <BitColorPicker @ref=""ColorPicker"" @bind-Color=""BoundColor"" ShowAlphaSlider=""true"" />
    <span>Bound Color Value: @BoundColor</span>
    <span>Reference Hex: @ColorPicker?.Hex</span>
    <span>Reference Rgb: @ColorPicker?.Rgb</span>
    <span>Reference Alpha: @ColorPicker?.Alpha</span>
</div>
";

    private readonly string example5CSharpCode = @"
private BitColorValue ColorValue;
private string BoundColor = ""#FFFFFF"";
private BitColorPicker ColorPicker;
";

    #endregion
}
