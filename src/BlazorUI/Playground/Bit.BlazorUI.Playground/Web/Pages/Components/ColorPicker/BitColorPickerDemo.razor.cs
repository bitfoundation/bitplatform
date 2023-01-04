using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ColorPicker;

public partial class BitColorPickerDemo
{
    private BitColorPicker ColorPicker;
    private string Color = "#FFFFFF";
    private string ColorRgb = "rgb(255,255,255)";
    private double Alpha = 1;
    private bool IsToggleChecked = false;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "Alpha",
            Type = "double",
            DefaultValue = "",
            Description = "Indicates the Alpha value.",
        },
        new ComponentParameter()
        {
            Name = "Color",
            Type = "string",
            DefaultValue = "",
            Description = "CSS-compatible string to describe the color.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<BitColorEventArgs>",
            DefaultValue = "",
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

    private readonly string example1HTMLCode = @"
<div>
    <BitColorPicker ShowPreview=""@IsToggleChecked"" @ref=""ColorPicker"" @bind-Color=""@Color"" ShowAlphaSlider=""false"" />
</div>
<div class=""operators"">
    <BitToggle Label=""Show Preview Box"" @bind-Value=""@IsToggleChecked"" IsEnabled=""true"" />
    <BitTextField Label=""Hex Code"" Value=""@Color"" />
    <BitTextField Label=""RGB"" Value=""@(ColorPicker?.Rgb ?? ""rgb(255,255,255)"")"" />
</div>
";

    private readonly string example1CSharpCode = @"
private BitColorPicker ColorPicker;
private string Color = ""#FFFFFF"";
private bool IsToggleChecked = false;
";

    private readonly string example2HTMLCode = @"
<div>
    <BitColorPicker ShowPreview=""true"" @bind-Alpha=""@Alpha"" @bind-Color=""@ColorRgb"" />
</div>
<div class=""operators"">
    <BitTextField Label=""RGB"" Value=""@ColorRgb"" />
    <BitTextField Label=""Alpha"" Value=""@(Alpha.ToString())"" />
</div>
";

    private readonly string example2CSharpCode = @"
private BitColorPicker ColorPicker;
private string ColorRgb = ""rgb(255,255,255)"";
private double Alpha = 1;
";
}
