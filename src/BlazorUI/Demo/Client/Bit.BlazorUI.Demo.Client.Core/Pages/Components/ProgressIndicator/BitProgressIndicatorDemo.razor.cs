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
            Name = "BarColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "Color of the BitProgressIndicator.",
        },
        new()
        {
            Name = "BarHeight",
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
            Name = "IsProgressHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to hide the progress state.",
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
            Name = "PercentageFormat",
            Type = "string",
            DefaultValue = "{0:P0}",
            Description = "The format of the percent in percentage display.",
        },
        new()
        {
            Name = "ProgressTemplate",
            Type = "RenderFragment<BitProgressIndicator>?",
            DefaultValue = "null",
            Description = "A custom template for progress track.",
        },
        new()
        {
            Name = "ShowPercent",
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
            Parameters = new()
            {
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProgressIndicator."
               },
               new()
               {
                   Name = "LabelContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label container of the BitProgressIndicator."
               },
               new()
               {
                   Name = "LabelWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label wrapper of the BitProgressIndicator."
               },
               new()
               {
                   Name = "PercentContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the percent container of the BitProgressIndicator."
               },
               new()
               {
                   Name = "IndicatorWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the indicator wrapper of the BitProgressIndicator."
               },
               new()
               {
                   Name = "Tracker",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tracker of the BitProgressIndicator."
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
                   Name = "DescriptionContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description container of the BitProgressIndicator."
               }
            }
        }
    ];


    private readonly string example1RazorCode = @"
<BitProgressIndicator Label=""Basic ProgressIndicator""
                      Description=""Example description""
                      Percent=""42"" />

<BitProgressIndicator Label=""Bar Height""
                      Percent=""69""
                      BarHeight=""10"" />

<BitProgressIndicator Label=""Show Percent""
                      Percent=""85.69""
                      ShowPercent />

<BitProgressIndicator Label=""Percent Format""
                      Percent=""85.69""
                      PercentageFormat=""{0:P2}""
                      ShowPercent />

<BitProgressIndicator Label=""Indeterminate""
                      Indeterminate />";

    private readonly string example2RazorCode = @"
<BitProgressIndicator BarColor=""#c10606"" Percent=""69"" />

<BitProgressIndicator BarColor=""#ffba17"" Indeterminate />";

    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        background-color: darkred;
        border-radius: 0.5rem;
        padding: 0.2rem;
        margin-bottom: 1rem;
    }

    .custom-tracker {
        background-color: #ff6a00;
    }

    .custom-bar {
        background-color: #ff2700;
    }
</style>

<BitProgressIndicator Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" BarHeight=""10"" Indeterminate />

<BitProgressIndicator Class=""custom-class""
                      Percent=""69""
                      BarHeight=""10"" />


<BitProgressIndicator Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"" ,
                                         Tracker = ""background-color: green;"" })""
                      BarHeight=""10""
                      Indeterminate />

<BitProgressIndicator Classes=""@(new() { Bar = ""custom-bar"",
                                          Tracker = ""custom-tracker""})""
                      Percent=""69""
                      BarHeight=""10"" />";

    private readonly string example4RazorCode = @"
<BitProgressIndicator Dir=""BitDir.Rtl""
                      BarHeight=""10""
                      Indeterminate />

<BitProgressIndicator Label=""لیبل تست""
                      Description=""توضیحات تست""
                      Dir=""BitDir.Rtl""
                      Percent=""69""
                      BarHeight=""10""
                      ShowPercent />";
}
