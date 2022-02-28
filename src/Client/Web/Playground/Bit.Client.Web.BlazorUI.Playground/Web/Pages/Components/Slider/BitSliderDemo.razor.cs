using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
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

        private void ChangeBitSliderRangedValues()
        {
            sliderRangedLowerValue = 3;
            sliderRangedUpperValue = 7;
        }

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

        private readonly string example1HTMLCode = @"<BitSlider />
<BitSlider Label=""Snapping Slider""
           Min=""0""
           Max=""50""
           Step=""10""
           DefaultValue=""20"" />
<BitSlider Label=""Disabled""
           Min=""50""
           Max=""500""
           Step=""50""
           DefaultValue=""300""
           IsEnabled=""false"" />
<BitSlider Label=""Controlled""
           Max=""10""
           @bind-Value=""sliderHorizontalValue"" />
<BitButton Class=""bit-btn-slider"" OnClick=""() => sliderHorizontalValue = 7"">Change Value</BitButton>
<BitSlider Label=""Formatted value""
           Max=""100""
           DefaultValue=""31""
           ValueFormat=""P00"" />
<BitSlider Label=""Formatted Value""
           Max=""1000""
           DefaultValue=""319""
           ValueFormat=""P01"" />
<BitSlider Label=""Origin From Zero""
           Min=""-5""
           Max=""5""
           Step=""1""
           DefaultValue=""2""
           IsRanged=""true""
           IsOriginFromZero=""true"" />";

        private readonly string example1CSharpCode = @"
private double? sliderHorizontalValue = 2;";

        private readonly string example2HTMLCode = @"<div class=""flex-container"">
    <BitSlider Label=""Basic""
               Min=""1""
               Max=""5""
               Step=""1""
               DefaultValue=""2""
               IsVertical=""true"" />
    <BitSlider Label=""Disabled""
               Min=""50""
               Max=""500""
               Step=""50""
               DefaultValue=""300""
               IsVertical=""true"" />
    <BitSlider Label=""Formatted value""
               Max=""100"" ValueFormat=""P00""
               IsVertical=""true"" />
    <BitSlider Label=""Origin From Zero""
               Min=""-5""
               Max=""15""
               Step=""1""
               DefaultValue=""5""
               IsVertical=""true""
               IsOriginFromZero=""true"" />
</div>

<style>
    .flex-container {
        display: flex;
    }
</style>";

        private readonly string example3HTMLCode = @"<BitSlider Label=""Range slider""
           Min=""0""
           Max=""10""
           DefaultUpperValue=""8""
           DefaultLowerValue=""2""
           IsRanged=""true"" />
<BitSlider Label=""Disabled""
           Min=""50""
           Max=""500""
           Step=""50""
           DefaultUpperValue=""300""
           IsRanged=""true""
           IsEnabled=""false"" />
<BitSlider Label=""Controlled example""
           Max=""10""
           @bind-LowerValue=""sliderRangedLowerValue""
           @bind-UpperValue=""sliderRangedUpperValue""
           IsRanged=""true"" />
<BitButton Class=""bit-btn-slider"" OnClick=""ChangeBitSliderRangedValues"">
    Change Value
</BitButton>
<BitSlider Label=""Formatted value""
           Max=""100""
           IsRanged=""true""
           ValueFormat=""P00"" />
<BitSlider Label=""Origin from zero""
           Min=""-5""
           Max=""5""
           Step=""1""
           DefaultUpperValue=""2""
           IsRanged=""true""
           IsOriginFromZero=""true"" />";

        private readonly string example3CSharpCode = @"
private double? sliderRangedLowerValue = 0;
private double? sliderRangedUpperValue = 0;
private void ChangeBitSliderRangedValues()
{
    sliderRangedLowerValue = 3;
    sliderRangedUpperValue = 7;
}";

        private readonly string example4HTMLCode = @"<BitSlider Label=""Vertical Range Slider""
        Min=""1""
        Max=""5""
        Step=""1""
        DefaultUpperValue=""2""
        DefaultLowerValue=""1""
        IsRanged=""true""
        IsVertical=""true"" />";

        private readonly string example5HTMLCode = @"<BitSlider Max=""10""
        IsRanged=""true""
        SliderBoxHtmlAttributes=""@BitSliderRangedSliderBoxHtmlAttributes"" />";

        private readonly string example5CSharpCode = @"
private Dictionary<string, object> BitSliderRangedSliderBoxHtmlAttributes = new()
{ 
    { ""custom-attribute"", ""demo"" }
};";
    }
}
