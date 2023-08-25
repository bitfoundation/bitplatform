namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Slider;

public partial class BitSliderDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaValueText",
            Type = "Func<double, string>?",
            DefaultValue = "null",
            Description = "A text description of the Slider number value for the benefit of screen readers. This should be used when the Slider number value is not accurately represented by a number.",
        },
        new()
        {
            Name = "DefaultLowerValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The default lower value of the ranged Slider.",
        },
        new()
        {
            Name = "DefaultUpperValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The default upper value of the ranged Slider.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The default value of the Slider.",
        },
        new()
        {
            Name = "IsOriginFromZero",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to attach the origin of slider to zero.",
        },
        new()
        {
            Name = "IsRanged",
            Type = "bool",
            DefaultValue = "false",
            Description = "If ranged is true, display two thumbs that allow the lower and upper bounds of a range to be selected.",
        },
        new()
        {
            Name = "IsReadonly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to render the Slider as readonly.",
        },
        new()
        {
            Name = "IsVertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to render the slider vertically.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Description label of the Slider.",
        },
        new()
        {
            Name = "LowerValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The lower value of the ranged Slider.",
        },
        new()
        {
            Name = "Min",
            Type = "double",
            DefaultValue = "0",
            Description = "The min value of the Slider.",
        },
        new()
        {
            Name = "Max",
            Type = "double",
            DefaultValue = "10",
            Description = "The max value of the Slider.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<ChangeEventArgs>",
            Description = "Callback when the value has been changed. This will be called on every individual step.",
        },
        new()
        {
            Name = "RangeValue",
            Type = "BitSliderRangeValue?",
            DefaultValue = "null",
            Description = "The initial range value of the Slider. Use this parameter to set value for both LowerValue and UpperValue.",
        },
        new()
        {
            Name = "ShowValue",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether to show the value on the right of the Slider.",
        },
        new()
        {
            Name = "SliderBoxHtmlAttributes",
            Type = "Dictionary<string, object>?",
            DefaultValue = "null",
            Description = "Additional parameter for the Slider box.",
        },
        new()
        {
            Name = "Step",
            Type = "double",
            DefaultValue = "1",
            Description = "The difference between the two adjacent values of the Slider.",
        },
        new()
        {
            Name = "UpperValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "The upper value of the ranged Slider.",
        },
        new()
        {
            Name = "Value",
            Type = "double?",
            DefaultValue = "null",
            Description = "The value of the Slider.",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom formatter for the Slider value.",
        }
    };



    private readonly string example1HtmlCode = @"
<BitSlider Label=""Basic slider"" />
<br />
<br />
<BitSlider Label=""Disabled slider""
            Min=""50""
            Max=""500""
            Step=""50""
            DefaultValue=""300""
            IsEnabled=""false"" />
<br />
<br />
<BitSlider Label=""Snapping slider""
            Min=""0""
            Max=""50""
            Step=""10""
            DefaultValue=""20"" />
<br />
<br />
<BitSlider @bind-Value=""sliderHorizontalValue""
            Label=""Controlled slider""
            Max=""10"" />
<BitButton Class=""bit-btn-slider"" OnClick=""() => sliderHorizontalValue = 2"">Reset value</BitButton>
<br />
<br />
<br />
<BitSlider Label=""Formatted value""
            Max=""100""
            DefaultValue=""31""
            ValueFormat=""P00"" />
<br />
<br />
<BitSlider Label=""Formatted value""
            Max=""1000""
            DefaultValue=""319""
            ValueFormat=""P01"" />
<br />
<br />
<BitSlider Label=""Origin from zero""
            Min=""-5""
            Max=""5""
            Step=""1""
            DefaultValue=""2""
            IsOriginFromZero=""true"" />";
    private readonly string example1CsharpCode = @"
private double? sliderHorizontalValue = 2;";

    private readonly string example2HtmlCode = @"
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
           IsVertical=""true""
           IsEnabled=""false"" />

<BitSlider Label=""Formatted value""
           Max=""100"" ValueFormat=""P00""
           IsVertical=""true"" />

<BitSlider Label=""Origin from zero""
           Min=""-5""
           Max=""15""
           Step=""1""
           DefaultValue=""5""
           IsVertical=""true""
           IsOriginFromZero=""true"" />";

    private readonly string example3HtmlCode = @"
<BitSlider Label=""Basic""
           Min=""0""
           Max=""10""
           DefaultLowerValue=""2""
           DefaultUpperValue=""8""
           IsRanged=""true"" />

<BitSlider Label=""Disabled""
           Min=""50""
           Max=""500""
           Step=""50""
           DefaultLowerValue=""200""
           DefaultUpperValue=""300""
           IsRanged=""true""
           IsEnabled=""false"" />

<BitSlider Label=""Controlled example""
           Max=""10""
           @bind-LowerValue=""sliderRangedLowerValue""
           @bind-UpperValue=""sliderRangedUpperValue""
           IsRanged=""true"" />
<BitButton Class=""bit-btn-slider"" OnClick=""ResetBitSliderRangedValues"">Reset value</BitButton>

<BitSlider Label=""Formatted value""
           Max=""100""
           ValueFormat=""P00""
           IsRanged=""true""
           DefaultLowerValue=""20""
           DefaultUpperValue=""70"" />

<BitSlider Label=""Origin from zero""
           Min=""-5""
           Max=""5""
           Step=""1""
           DefaultUpperValue=""2""
           IsRanged=""true""
           IsOriginFromZero=""true"" />";
    private readonly string example3CsharpCode = @"
private double? sliderRangedLowerValue = 3;
private double? sliderRangedUpperValue = 7;
private void ResetBitSliderRangedValues()
{
    sliderRangedLowerValue = 3;
    sliderRangedUpperValue = 7;
}";

    private readonly string example4HtmlCode = @"
<BitSlider Label=""Basic""
           Min=""1""
           Max=""5""
           Step=""1""
           DefaultUpperValue=""2""
           DefaultLowerValue=""1""
           IsRanged=""true""
           IsVertical=""true"" />

<BitSlider Label=""Disabled""
           Min=""50""
           Max=""500""
           Step=""50""
           DefaultUpperValue=""100""
           DefaultLowerValue=""300""
           IsRanged=""true""
           IsVertical=""true""
           IsEnabled=""false"" />

<BitSlider Label=""Formatted value""
           Max=""100""
           ValueFormat=""P00""
           DefaultUpperValue=""30""
           DefaultLowerValue=""60""
           IsRanged=""true""
           IsVertical=""true"" />

<BitSlider Label=""Origin from zero""
           Min=""-5""
           Max=""15""
           Step=""1""
           DefaultUpperValue=""7""
           DefaultLowerValue=""3""
           IsRanged=""true""
           IsVertical=""true""
           IsOriginFromZero=""true"" />";

    private readonly string example5HtmlCode = @"
<BitSlider Max=""10""
           DefaultUpperValue=""3""
           DefaultLowerValue=""5""
           IsRanged=""true""
           SliderBoxHtmlAttributes=""@(new() { { ""custom-attribute"", ""demo"" } })"" />";
}
