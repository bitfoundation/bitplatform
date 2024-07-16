namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Progress;

public partial class BitProgressDemo
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
            Type = "BitProgressClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressBar-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitProgress.",
        },
        new()
        {
            Name = "Color",
            Type = "string?",
            DefaultValue = "null",
            Description = "Color of the BitProgress.",
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Circular mode of the BitProgress.",
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
            Description = "Label to display above the BitProgress.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label template to display above the BitProgress.",
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
            Name = "Radius",
            Type = "int",
            DefaultValue = "6",
            Description = "The radius of the circular progress.",
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
            Type = "BitProgressClassStyles?",
            LinkType = LinkType.Link,
            Href = "#progressBar-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS Styles for different parts of the BitProgress.",
        },
        new()
        {
            Name = "Thickness",
            Type = "int",
            DefaultValue = "2",
            Description = "Thickness of the BitProgress.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "progressBar-class-styles",
            Title = "BitProgressClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProgress."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitProgress."
               },
               new()
               {
                   Name = "PercentNumber",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the percent number of the BitProgress."
               },
               new()
               {
                   Name = "BarContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar container of the BitProgress."
               },
               new()
               {
                   Name = "Track",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the track of the BitProgress."
               },
               new()
               {
                   Name = "Bar",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the bar of the BitProgress."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitProgress."
               }
            ]
        }
    ];



    private double barThickness = 10;



    private readonly string example1RazorCode = @"
<BitProgress Label=""Basic Progress""
             Description=""Example description""
             Percent=""42"" />";
    
    private readonly string example2RazorCode = @"
<BitProgress Circular
             Label=""Basic Circular Progress""
             Description=""Example description""
             Percent=""42"" />";

    private readonly string example3RazorCode = @"
<BitProgress Label=""Show Percent Number""
             Percent=""85.69""
             ShowPercentNumber />
<BitProgress Label=""Percent Number Format""
             Percent=""85.69""
             PercentNumberFormat=""{0:F2} %""
             ShowPercentNumber />

<BitProgress Circular
             Label=""Show Percent Number""
             Percent=""85.69""
             ShowPercentNumber />
<BitProgress Circular
             Label=""Percent Number Format""
             Percent=""85.69""
             PercentNumberFormat=""{0:F2} %""
             ShowPercentNumber />";

    private readonly string example4RazorCode = @"
<BitSlider @bind-Value=""barThickness"" Max=""50"" />

<BitProgress ShowPercentNumber Percent=""69"" Thickness=""(int)barThickness"" />

<BitProgress Circular ShowPercentNumber Percent=""69"" Thickness=""(int)barThickness"" />";
    private readonly string example4CsharpCode = @"
private double barThickness = 10;";

    private readonly string example5RazorCode = @"
<BitProgress Indeterminate />

<BitProgress Circular Indeterminate />";

    private readonly string example6RazorCode = @"
<BitProgress Color=""#c10606"" Percent=""69"" />
<BitProgress Color=""#ffba17"" Indeterminate />

<BitProgress Color=""#c10606"" Circular Percent=""69"" />
<BitProgress Color=""#ffba17"" Circular Indeterminate />";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        padding: 0.2rem;
        margin-bottom: 1rem;
        border-radius: 0.5rem;
        background-color: darkred;
    }

    .custom-track {
        background-color: #ff6a00;
    }

    .custom-bar {
        background-color: #ff2700;
    }

    .custom-circle-track {
        stroke: #ff6a00;
    }

    .custom-circle-bar {
        stroke: #ff2700;
    }
</style>


<BitProgress Indeterminate Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Height=""10"" />

<BitProgress Class=""custom-class""
             Percent=""69""
             Height=""10"" />


<BitProgress Circular Indeterminate Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Height=""10"" />

<BitProgress Circular
             Class=""custom-class""
             Percent=""69""
             Height=""10"" 

<BitProgress Indeterminate
             Height=""10""
             Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"",
                                Track = ""background-color: green;"" })"" />

<BitProgress Classes=""@(new() { Bar = ""custom-bar"",
                                 Track = ""custom-tracker""})""
             Percent=""69""
             Height=""10"" />


<BitProgress Circular Indeterminate
             Height=""10""
             Styles=""@(new() { Bar = ""stroke: greenyellow;"",
                               Track = ""stroke: green;"" })"" />

<BitProgress Circular
             Percent=""69""
             Height=""10""
             Classes=""@(new() { Bar = ""custom-circle-bar"",
                                Track = ""custom-circle-tracker""})"" />";

    private readonly string example8RazorCode = @"
<BitProgress Dir=""BitDir.Rtl""
             Height=""10""
             Indeterminate />

<BitProgress Label=""لیبل تست""
             Description=""توضیحات تست""
             Dir=""BitDir.Rtl""
             Percent=""69""
             Height=""10""
             ShowPercent />";
}
