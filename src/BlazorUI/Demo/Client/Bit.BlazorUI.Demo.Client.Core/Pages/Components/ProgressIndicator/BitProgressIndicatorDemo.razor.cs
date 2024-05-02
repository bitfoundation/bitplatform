namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ProgressIndicator;

public partial class BitProgressIndicatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text alternative of the progress status, used by screen readers for reading the value of the progress.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitProgressIndicatorClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressIndicator-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitProgressIndicator.",
        },
        new()
        {
            Name = "Color",
            Type = "string?",
            DefaultValue = "null",
            Description = "Color of the BitProgressIndicator.",
        },
        new()
        {
            Name = "Height",
            Type = "int",
            DefaultValue = "2",
            Description = "Height of the BitProgressIndicator.",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text describing or supplementing the operation.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for describing or supplementing the operation.",
        },
        new()
        {
            Name = "Indeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to show indeterminate progress animation.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label to display above the BitProgressIndicator.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label template to display above the BitProgressIndicator.",
        },
        new()
        {
            Name = "Percent",
            Type = "double",
            DefaultValue = "0",
            Description = "Percentage of the operation's completeness, numerically between 0 and 100.",
        },
        new()
        {
            Name = "PercentNumberFormat",
            Type = "string",
            DefaultValue = "{0:F0}",
            Description = "The format of the percent in percentage display.",
        },
        new()
        {
            Name = "ShowPercentNumber",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to percentage display.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitProgressIndicatorClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressIndicator-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS Styles for different parts of the BitProgressIndicator.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "progressIndicator-class-styles",
            Title = "BitProgressIndicatorClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitProgressIndicator."
               },
               new()
               {
                   Name = "PercentNumber",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the percent number of the BitProgressIndicator."
               },
               new()
               {
                   Name = "BarContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar container of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Track",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the track of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Bar",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitProgressIndicator."
               }
            ]
        }
    ];


    private readonly string example1RazorCode = @"
<BitProgressIndicator Label=""Basic ProgressIndicator""
                      Description=""Example description""
                      Percent=""42"" />";

    private readonly string example2RazorCode = @"
<BitProgressIndicator Percent=""69"" Height=""10"" />";

    private readonly string example3RazorCode = @"
<BitProgressIndicator Label=""Show Percent Number""
                      Percent=""85.69""
                      ShowPercentNumber />

<BitProgressIndicator Label=""Percent Number Format""
                      Percent=""85.69""
                      PercentNumberFormat=""{0:F2} %""
                      ShowPercentNumber />";

    private readonly string example4RazorCode = @"
<BitProgressIndicator Indeterminate />";

    private readonly string example5RazorCode = @"
<BitProgressIndicator Color=""#c10606"" Percent=""69"" />

<BitProgressIndicator Color=""#ffba17"" Indeterminate />";

    private readonly string example6RazorCode = @"
<style>
    .custom-class {
        background-color: darkred;
        border-radius: 0.5rem;
        padding: 0.2rem;
        margin-bottom: 1rem;
    }

    .custom-track {
        background-color: #ff6a00;
    }

    .custom-bar {
        background-color: #ff2700;
    }
</style>

<BitProgressIndicator Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Height=""10"" Indeterminate />

<BitProgressIndicator Class=""custom-class""
                      Percent=""69""
                      Height=""10"" />


<BitProgressIndicator Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"" ,
                                         Track = ""background-color: green;"" })""
                      Height=""10""
                      Indeterminate />

<BitProgressIndicator Classes=""@(new() { Bar = ""custom-bar"",
                                          Track = ""custom-track""})""
                      Percent=""69""
                      Height=""10"" />";

    private readonly string example7RazorCode = @"
<BitProgressIndicator Dir=""BitDir.Rtl""
                      Height=""10""
                      Indeterminate />

<BitProgressIndicator Label=""لیبل تست""
                      Description=""توضیحات تست""
                      Dir=""BitDir.Rtl""
                      Percent=""69""
                      Height=""10""
                      ShowPercent />";
}
