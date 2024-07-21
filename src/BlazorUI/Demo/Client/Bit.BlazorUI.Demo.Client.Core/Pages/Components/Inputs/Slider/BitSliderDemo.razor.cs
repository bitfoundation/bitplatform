namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Slider;

public partial class BitSliderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaValueText",
            Type = "Func<double, string>?",
            DefaultValue = "null",
            Description = "A text description of the Slider number value for the benefit of screen readers. This should be used when the Slider number value is not accurately represented by a number.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSliderClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitSlider.",
            LinkType = LinkType.Link,
            Href = "#slider-class-styles",
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
            Name = "IsReadOnly",
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
            Type = "double",
            DefaultValue = "0",
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
            LinkType = LinkType.Link,
            Href = "#slider-range-value",
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
            Name = "Styles",
            Type = "BitSliderClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitSlider.",
            LinkType = LinkType.Link,
            Href = "#slider-class-styles",
        },
        new()
        {
            Name = "UpperValue",
            Type = "double",
            DefaultValue = "0",
            Description = "The upper value of the ranged Slider.",
        },
        new()
        {
            Name = "Value",
            Type = "double",
            DefaultValue = "0",
            Description = "The value of the Slider.",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom formatter for the Slider value.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "slider-class-styles",
            Title = "BitSliderClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's root element."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's label."
                },
                new()
                {
                    Name = "UpperValueLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's upper value label."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's container."
                },
                new()
                {
                    Name = "LowerValueLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's lower value label."
                },
                new()
                {
                    Name = "SliderBox",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's box."
                },
                new()
                {
                    Name = "LowerValueInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's lower value input."
                },
                new()
                {
                    Name = "UpperValueInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's upper value input."
                },
                new()
                {
                    Name = "ValueInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's value input."
                },
                new()
                {
                    Name = "OriginFromZero",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's origin from zero."
                },
                new()
                {
                    Name = "ValueLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitSlider's value label."
                }
            }
        },
        new()
        {
            Id="slider-range-value",
            Title="BitSliderRangeValue",
            Parameters = new()
            {
                new()
                {
                    Name = "Lower",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The lower value of the ranged Slider."
                },
                new()
                {
                    Name = "Upper",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The upper value of the ranged Slider."
                }
            }
        }
    ];

    

    private double oneWayBinding = 1;
    private double twoWayBinding = 1;
    private object? onChangeValue;



    private readonly string example1RazorCode = @"
<BitSlider Label=""Basic slider"" />

<BitSlider Label=""Disabled slider"" DefaultValue=""5"" IsEnabled=""false"" />

<BitSlider Label=""Snapping slider"" Min=""0"" Max=""50"" Step=""10"" />

<BitSlider Label=""Formatted value"" Max=""1"" Step=""0.01"" DefaultValue=""0.69"" ValueFormat=""0 %"" />

<BitSlider Label=""Origin from zero"" Min=""-5"" Max=""5"" DefaultValue=""0"" IsOriginFromZero=""true"" />";

    private readonly string example2RazorCode = @"
Visible: [ <BitSlider Visibility=""BitVisibility.Visible"" /> ]
Hidden: [ <BitSlider Visibility=""BitVisibility.Hidden"" /> ]
Collapsed: [ <BitSlider Visibility=""BitVisibility.Collapsed"" /> ]";

    private readonly string example3RazorCode = @"
<BitSlider Label=""Basic"" IsVertical=""true"" />

<BitSlider Label=""Disabled"" IsVertical=""true"" IsEnabled=""false"" />

<BitSlider Label=""Formatted value"" IsVertical=""true"" DefaultValue=""2"" ValueFormat=""0 cm"" />

<BitSlider Label=""Origin from zero"" IsVertical=""true"" Min=""-5"" Max=""5"" DefaultValue=""0"" IsOriginFromZero=""true"" />";

    private readonly string example4RazorCode = @"
<BitSlider Label=""Basic"" IsRanged=""true"" />

<BitSlider Label=""Disabled"" IsRanged=""true"" DefaultLowerValue=""2"" DefaultUpperValue=""5"" IsEnabled=""false"" />

<BitSlider Label=""Formatted value"" IsRanged=""true""
           Step=""0.1""
           ValueFormat=""0.0 px""
           DefaultLowerValue=""4.2""
           DefaultUpperValue=""8.5"" />

<BitSlider Label=""Origin from zero"" IsRanged=""true""
           Min=""-5""
           Max=""5""
           DefaultUpperValue=""2""
           IsOriginFromZero=""true"" />";

    private readonly string example5RazorCode = @"
<BitSlider Label=""Basic"" IsVertical=""true"" IsRanged=""true""
           DefaultLowerValue=""1""
           DefaultUpperValue=""2"" />

<BitSlider Label=""Disabled"" IsVertical=""true"" IsRanged=""true""
           DefaultUpperValue=""1""
           DefaultLowerValue=""3""
           IsEnabled=""false"" />

<BitSlider Label=""Formatted value"" IsVertical=""true"" IsRanged=""true""
           Step=""0.01""
           ValueFormat=""0.00 rem""
           DefaultLowerValue=""4.20""
           DefaultUpperValue=""6.9"" />

<BitSlider Label=""Origin from zero"" IsVertical=""true"" IsRanged=""true""
           Min=""-5""
           Max=""5""
           DefaultUpperValue=""3""
           IsOriginFromZero=""true"" />";

    private readonly string example6RazorCode = @"
<style>
    .custom-input::-webkit-slider-thumb {
        width: 1.5rem;
        height: 1.5rem;
        border-radius: 50%;
        margin-top: -0.75rem;
        border-color: whitesmoke;
        background-color: whitesmoke;
        box-shadow: 0 0 0.5rem 0 lightgray;
    }

    .custom-input:hover::-webkit-slider-thumb {
        border-color: whitesmoke;
        background-color: whitesmoke;
    }

    .custom-input::-webkit-slider-runnable-track {
        height: 0.125rem;
        background: linear-gradient(dodgerblue, dodgerblue) 0/var(--sx) 100% no-repeat, whitesmoke;
    }

    .custom-input:hover::-webkit-slider-runnable-track {
        background: linear-gradient(dodgerblue, dodgerblue) 0/var(--sx) 100% no-repeat, whitesmoke;
    }


    .custom-slider-box {
        background: linear-gradient(0deg, seagreen calc(0.5rem * 0.5), transparent 0);
    }

    .custom-slider-box:hover {
        background: linear-gradient(0deg, seagreen calc(0.5rem * 0.5), transparent 0);
    }

    .custom-slider-box:hover::before {
        background-color: darkgreen;
    }

    .custom-slider-box::before {
        background-color: green;
    }

    .custom-range-input::-webkit-slider-thumb {
        background-color: white;
        border: 0.25rem solid green;
    }

    .custom-range-input:hover::-webkit-slider-thumb {
        background-color: white;
        border: 0.25rem solid darkgreen;
    }
</style>


<BitSlider DefaultValue=""3""
           Label=""Custom styles""
           Styles=""@(new() { Root = ""text-shadow: aqua 0 0 1rem;"",
                             Label = ""font-weight: 900; font-size: 1.25rem;"" } )"" />

<BitSlider DefaultValue=""5""
           Label=""Custom classes""
           Classes=""@(new() { ValueInput = ""custom-input"" } )"" />

<BitSlider IsRanged=""true""
           Max=""100""
           DefaultLowerValue=""54""
           DefaultUpperValue=""84""
           Classes=""@(new() { LowerValueInput = ""custom-range-input"",
                              UpperValueInput = ""custom-range-input"",
                              SliderBox = ""custom-slider-box"" } )"" />";

    private readonly string example7RazorCode = @"
<BitSlider Label=""One-way"" Value=""oneWayBinding"" />
<BitRating Max=""10"" @bind-Value=""oneWayBinding"" />

<BitSlider Label=""Two-way"" @bind-Value=""twoWayBinding"" />
<BitRating Max=""10"" @bind-Value=""twoWayBinding"" />

<BitSlider Label=""OnChange"" DefaultValue=""2"" OnChange=""v => onChangeValue = v.Value"" />
<BitLabel>OnChange value: @onChangeValue</BitLabel>";
    private readonly string example7CsharpCode = @"
private double oneWayBinding = 1;
private double twoWayBinding = 1;
private object? onChangeValue;";

    private readonly string example8RazorCode = @"
<BitSlider Label=""RTL slider"" Dir=""BitDir.Rtl"" />

<BitSlider IsRanged Label=""RTL ranged slider"" 
           Dir=""BitDir.Rtl""
           DefaultLowerValue=""2""
           DefaultUpperValue=""5"" />";
}
