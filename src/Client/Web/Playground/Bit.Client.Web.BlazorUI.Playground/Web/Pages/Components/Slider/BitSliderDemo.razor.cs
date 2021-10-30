using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Slider
{
    public partial class BitSliderDemo
    {
        private double? BitSliderHorizontalValue = 2;
        private double? BitSliderRangedLowerValue = 0;
        private double? BitSliderRangedUpperValue = 0;

        private Dictionary<string, object>? BitSliderRangedSliderBoxHtmlAttributes = new()
        {
            { "custom-attribute", "demo" }
        };

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "DefaultUpperValue",
                Type = "double",
                DefaultValue = "",
                Description = "The initial upper value of the Slider is ranged is true.",
            },
            new ComponentParameter()
            {
                Name = "DefaultLowerValue",
                Type = "double",
                DefaultValue = "",
                Description = "The initial lower value of the Slider is ranged is true.",
            },
            new ComponentParameter()
            {
                Name = "DefaultValue",
                Type = "double",
                DefaultValue = "",
                Description = "The initial value of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "Min",
                Type = "double",
                DefaultValue = "0",
                Description = "The min value of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "Max",
                Type = "double",
                DefaultValue = "10",
                Description = "The max value of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "Step",
                Type = "double",
                DefaultValue = "",
                Description = "The difference between the two adjacent values of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "UpperValue",
                Type = "double",
                DefaultValue = "",
                Description = "The initial lower value of the Slider is ranged is true.",
            },
            new ComponentParameter()
            {
                Name = "UpperValueChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback for when lower value changed.",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "double",
                DefaultValue = "",
                Description = "The initial value of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback for when the value changed.",
            },
            new ComponentParameter()
            {
                Name = "RangeValue",
                Type = "double",
                DefaultValue = "",
                Description = "The initial range value of the Slider. Use this parameter to set value for both LowerValue and UpperValue.",
            },
            new ComponentParameter()
            {
                Name = "RangeValueChanged",
                Type = "EventCallback<(double Lower, double Upper)>",
                DefaultValue = "",
                Description = "Callback for when range value changed.",
            },
            new ComponentParameter()
            {
                Name = "IsOriginFromZero",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to attach the origin of slider to zero.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "Description label of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "IsRanged",
                Type = "bool",
                DefaultValue = "false",
                Description = "If ranged is true, display two thumbs that allow the lower and upper bounds of a range to be selected.",
            },
            new ComponentParameter()
            {
                Name = "ShowValue",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to show the value on the right of the Slider.",
            },
            new ComponentParameter()
            {
                Name = "IsVertical",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to render the slider vertically.",
            },
            new ComponentParameter()
            {
                Name = "ValueFormat",
                Type = "string",
                DefaultValue = "",
                Description = "Custom formatter for the Slider value.",
            },

            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<ChangeEventArgs>",
                DefaultValue = "",
                Description = "Callback when the value has been changed. This will be called on every individual step.",
            },
            new ComponentParameter()
            {
                Name = "AriaValueText",
                Type = "Func<double, string>",
                DefaultValue = "",
                Description = "A text description of the Slider number value for the benefit of screen readers. This should be used when the Slider number value is not accurately represented by a number.",
            },
            new ComponentParameter()
            {
                Name = "SliderBoxHtmlAttributes",
                Type = "Dictionary<string, object>",
                DefaultValue = "",
                Description = "Additional parameter for the Slider box.",
            },
            new ComponentParameter()
            {
                Name = "IsReadonly",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether to render the Slider as readonly.",
            },
        };

        private void ChangeBitSliderRangedValues()
        {
            BitSliderRangedLowerValue = 3;
            BitSliderRangedUpperValue = 7;
        }
    }
}
