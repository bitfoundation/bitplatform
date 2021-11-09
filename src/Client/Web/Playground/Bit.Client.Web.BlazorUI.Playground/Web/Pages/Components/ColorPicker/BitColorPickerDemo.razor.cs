using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ColorPicker
{
    public partial class BitColorPickerDemo
    {
        private BitColorPicker ColorPicker;
        private string Color = "#ffffff";
        private string ColorRgb = "rgb(255,255,255)";
        private double Alpha = 1;
        private bool IsToggleChecked = false;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "alpha",
                Type = "double",
                DefaultValue = "",
                Description = "Indicates the Alpha value.",
            },
            new ComponentParameter()
            {
                Name = "alphaChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback for when the alpha value changed.",
            },
            new ComponentParameter()
            {
                Name = "color",
                Type = "string",
                DefaultValue = "",
                Description = "CSS-compatible string to describe the color.",
            },
            new ComponentParameter()
            {
                Name = "colorChanged",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the color value changed.",
            },
            new ComponentParameter()
            {
                Name = "hex",
                Type = "string",
                DefaultValue = "",
                Description = "Indicates the Hex value.",
            },
            new ComponentParameter()
            {
                Name = "hexChanged",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the hex value changed.",
            },
            new ComponentParameter()
            {
                Name = "rgb",
                Type = "string",
                DefaultValue = "",
                Description = "Indicates the Rgb value.",
            },
            new ComponentParameter()
            {
                Name = "rgbChanged",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the rgb value changed.",
            },
            new ComponentParameter()
            {
                Name = "showAlphaSlider",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to show a slider for editing alpha value.",
            },
            new ComponentParameter()
            {
                Name = "showPreview",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to show color preview box.",
            },
        };
    }
}
