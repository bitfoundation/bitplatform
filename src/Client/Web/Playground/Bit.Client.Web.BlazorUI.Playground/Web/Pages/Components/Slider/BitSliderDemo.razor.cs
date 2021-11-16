using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Slider
{
    public partial class BitSliderDemo
    {
        private double? sliderHorizontalValue = 2;
        private double? sliderRangedLowerValue = 0;
        private double? sliderRangedUpperValue = 0;

        private Dictionary<string, object> BitSliderRangedSliderBoxHtmlAttributes = new()
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

        private readonly string sliderSampleCode = $"<BitSlider/>{Environment.NewLine}" +
              $"<BitSlider Label='Snapping Slider'{Environment.NewLine}" +
              $"Min='0'{Environment.NewLine}" +
              $"Max='50'{Environment.NewLine}" +
              $"Step='10'{Environment.NewLine}" +
              $"DefaultValue='20'/>{Environment.NewLine}" +
              $"<BitSlider Label='Disabled'{Environment.NewLine}" +
              $"Min='50'{Environment.NewLine}" +
              $"Max='500'{Environment.NewLine}" +
              $"Step='50'{Environment.NewLine}" +
              $"DefaultValue='300'{Environment.NewLine}" +
              $"IsEnabled='false'/>{Environment.NewLine}" +
              $"<BitSlider Label='Controlled'{Environment.NewLine}" +
              $"Max='10'{Environment.NewLine}" +
              $"ValueFormat='P00'{Environment.NewLine}" +
              $"DefaultValue='31'/>{Environment.NewLine}" +
              $"<BitSlider Label='Formatted Value'{Environment.NewLine}" +
              $"Max='1000'{Environment.NewLine}" +
              $"DefaultValue='319'{Environment.NewLine}" +
              $"ValueFormat='P01/>'{Environment.NewLine}" +
              $"<BitSlider Label='Origin From Zero'{Environment.NewLine}" +
              $"Min='-5'{Environment.NewLine}" +
              $"Max='5'{Environment.NewLine}" +
              $"Step='1'{Environment.NewLine}" +
              $"DefaultValue='2'{Environment.NewLine}" +
              $"IsOriginFromZero='true'/>{Environment.NewLine}" +
              $"@ code {{ {Environment.NewLine}" +
              $"private double? sliderHorizontalValue = 2; {Environment.NewLine}" +
              $"}}";

        private readonly string verticalSliderSampleCode = $"<BitSlider Label='Basic'{Environment.NewLine}" +
               $"Min='1'{Environment.NewLine}" +
               $"Max='5'{Environment.NewLine}" +
               $"Step='1'{Environment.NewLine}" +
               $"DefaultValue='2'{Environment.NewLine}" +
               $"IsVertical='true'/>{Environment.NewLine}" +
               $"<BitSlider Label='Disabled'{Environment.NewLine}" +
               $"Min='50'{Environment.NewLine}" +
               $"Max='500'{Environment.NewLine}" +
               $"Step='50'{Environment.NewLine}" +
               $"DefaultValue='300'{Environment.NewLine}" +
               $"IsEnabled='false'{Environment.NewLine}" +
               $"IsVertical='true'/>{Environment.NewLine}" +
               $"<BitSlider Label='Formatted Value'{Environment.NewLine}" +
               $"Max='100'{Environment.NewLine}" +
               $"ValueFormat='P00'{Environment.NewLine}" +
               $"IsVertical='true'/>{Environment.NewLine}" +
               $"<BitSlider Label='Origin From Zero'{Environment.NewLine}" +
               $"Min='-5'{Environment.NewLine}" +
               $"Max='5'{Environment.NewLine}" +
               $"Step='1'{Environment.NewLine}" +
               $"IsVertical='true'{Environment.NewLine}" +
               $"DefaultValue='2'{Environment.NewLine}" +
               $"IsOriginFromZero='true'/>";

        private readonly string rangeSliderSampleCode = $"<BitSlider Label='Range Slider'{Environment.NewLine}" +
               $"Min='0'{Environment.NewLine}" +
               $"Max='10'{Environment.NewLine}" +
               $"DefaultUpperValue='8'{Environment.NewLine}" +
               $"DefaultLowerValue='2'{Environment.NewLine}" +
               $"IsRanged='true'/>{Environment.NewLine}" +
               $"<BitSlider Label='Controlled'{Environment.NewLine}" +
               $"Max='10'{Environment.NewLine}" +
               $"@bind-LowerValue='sliderRangedLowerValue'{Environment.NewLine}" +
               $"@bind-UpperValue='sliderRangedUpperValue'{Environment.NewLine}" +
               $"IsRanged='true'/>{Environment.NewLine}" +
               $"<BitSlider Label='Formatted Value'{Environment.NewLine}" +
               $"Max='100'{Environment.NewLine}" +
               $"ValueFormat='P00'{Environment.NewLine}" +
               $"IsRanged='true'/>{Environment.NewLine}" +
               $"<BitSlider Label='Origin From Zero'{Environment.NewLine}" +
               $"Min='-5'{Environment.NewLine}" +
               $"Max='5'{Environment.NewLine}" +
               $"Step='1'{Environment.NewLine}" +
               $"DefaultUpperValue='2'{Environment.NewLine}" +
               $"IsOriginFromZero='true'{Environment.NewLine}" +
               $"IsRanged='true'/>{Environment.NewLine}" +
               $"@ code {{ {Environment.NewLine}" +
               $"private double? sliderRangedLowerValue = 0; {Environment.NewLine}" +
               $"private double? sliderRangedUpperValue = 0; {Environment.NewLine}" +
               $"private void ChangeBitSliderRangedValues() {{ {Environment.NewLine}" +
               $"{{ {Environment.NewLine}" +
               $"sliderRangedLowerValue = 3; {Environment.NewLine}" +
               $"sliderRangedUpperValue = 7; {Environment.NewLine}" +
               $"}} {Environment.NewLine}" +
               $"}}";

        private readonly string verticalSampleCode = $"<BitSlider Label='Vertical Range Slider'{Environment.NewLine}" +
               $"Min='1'{Environment.NewLine}" +
               $"Max='5'{Environment.NewLine}" +
               $"Step='1'{Environment.NewLine}" +
               $"DefaultUpperValue='2'{Environment.NewLine}" +
               $"DefaultLowerValue='1'{Environment.NewLine}" +
               $"IsRanged='true'{Environment.NewLine}" +
               $"IsVertical='true'/>";

        private readonly string sliderBoxHtmlAttributesSampleCode = $"<BitSlider Max='10'{Environment.NewLine}" +
              $"IsRanged='true'{Environment.NewLine}" +
              $"SliderBoxHtmlAttributes='@BitSliderRangedSliderBoxHtmlAttributes' /> {Environment.NewLine}" +
              $"@ code {{ {Environment.NewLine}" +
              $"private double? sliderRangedLowerValue = 0; {Environment.NewLine}" +
              $"private double? sliderRangedUpperValue = 0; {Environment.NewLine}" +
              $"private void ChangeBitSliderRangedValues() {{ {Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $" private Dictionary<string, object> BitSliderRangedSliderBoxHtmlAttributes = new() {Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $" {{ 'custom-attribute','demo' }} {Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $"}}";

        private void ChangeBitSliderRangedValues()
        {
            sliderRangedLowerValue = 3;
            sliderRangedUpperValue = 7;
        }
    }
}
