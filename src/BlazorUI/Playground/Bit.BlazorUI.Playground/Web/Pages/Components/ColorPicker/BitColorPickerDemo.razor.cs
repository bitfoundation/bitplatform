using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
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
            Name = "AlphaChanged",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "Callback for when the alpha value changed.",
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
            Name = "ColorChanged",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Callback for when the color value changed.",
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
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"<BitColorPicker ShowPreview=""@IsToggleChecked"" @ref=""ColorPicker"" @bind-Color=""@Color"" ShowAlphaSlider=""false"">Default ColorPicker</BitColorPicker>
<div>
    <BitToggle Label=""Show Preview Box"" @bind-Value=""@IsToggleChecked"" IsEnabled=""true"" />
</div>
<div>
    <BitTextField Label=""Hex Code"" Value=""@Color""></BitTextField>
    <BitTextField Label=""RGB"" Value=""@(ColorPicker?.Rgb ?? ""rgb(255,255,255)"")""></BitTextField>
</div>";

    private readonly string example1CSharpCode = @"
private BitColorPicker ColorPicker;
private string Color = ""#FFFFFF"";
private bool IsToggleChecked = false;";

    private readonly string example2HTMLCode = @"<BitColorPicker ShowPreview=""true"" @bind-Alpha=""@Alpha"" @bind-Color=""@ColorRgb"">Default ColorPicker</BitColorPicker>
<div>
    <BitTextField Label=""RGB"" Value=""@ColorRgb""></BitTextField>
    <BitTextField Label=""Alpha"" Value=""@(Alpha.ToString())""></BitTextField>
</div>";

    private readonly string example2CSharpCode = @"
private BitColorPicker ColorPicker;
private string ColorRgb = ""rgb(255,255,255)"";
private double Alpha = 1;";
}
