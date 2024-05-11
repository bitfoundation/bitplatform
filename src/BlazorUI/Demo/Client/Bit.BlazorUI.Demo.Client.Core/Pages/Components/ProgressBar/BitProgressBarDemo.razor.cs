namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ProgressBar;

public partial class BitProgressBarDemo
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
            Type = "BitProgressBarClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressBar-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitProgressBar.",
        },
        new()
        {
            Name = "Color",
            Type = "string?",
            DefaultValue = "null",
            Description = "Color of the BitProgressBar.",
        },
        new()
        {
            Name = "Height",
            Type = "int",
            DefaultValue = "2",
            Description = "Height of the BitProgressBar.",
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
            Description = "Label to display above the BitProgressBar.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label template to display above the BitProgressBar.",
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
            Type = "BitProgressBarClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressBar-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS Styles for different parts of the BitProgressBar.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "progressBar-class-styles",
            Title = "BitProgressBarClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProgressBar."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitProgressBar."
               },
               new()
               {
                   Name = "PercentNumber",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the percent number of the BitProgressBar."
               },
               new()
               {
                   Name = "BarContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar container of the BitProgressBar."
               },
               new()
               {
                   Name = "Track",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the track of the BitProgressBar."
               },
               new()
               {
                   Name = "Bar",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar of the BitProgressBar."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitProgressBar."
               }
            ]
        }
    ];


    private readonly string example1RazorCode = @"
<BitProgressBar Label=""Basic ProgressBar""
                Description=""Example description""
                Percent=""42"" />";

    private readonly string example2RazorCode = @"
<BitProgressBar Percent=""69"" Height=""10"" />";

    private readonly string example3RazorCode = @"
<BitProgressBar Label=""Show Percent Number""
                Percent=""85.69""
                ShowPercentNumber />

<BitProgressBar Label=""Percent Number Format""
                Percent=""85.69""
                PercentNumberFormat=""{0:F2} %""
                ShowPercentNumber />";

    private readonly string example4RazorCode = @"
<BitProgressBar Indeterminate />";

    private readonly string example5RazorCode = @"
<BitProgressBar Color=""#c10606"" Percent=""69"" />

<BitProgressBar Color=""#ffba17"" Indeterminate />";

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

<BitProgressBar Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Height=""10"" Indeterminate />

<BitProgressBar Class=""custom-class""
                Percent=""69""
                Height=""10"" />


<BitProgressBar Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"" ,
                                   Track = ""background-color: green;"" })""
                Height=""10""
                Indeterminate />

<BitProgressBar Classes=""@(new() { Bar = ""custom-bar"",
                                          Track = ""custom-track""})""
                Percent=""69""
                Height=""10"" />";

    private readonly string example7RazorCode = @"
<BitProgressBar Dir=""BitDir.Rtl""
                Height=""10""
                Indeterminate />

<BitProgressBar Label=""لیبل تست""
                Description=""توضیحات تست""
                Dir=""BitDir.Rtl""
                Percent=""69""
                Height=""10""
                ShowPercent />";
}
