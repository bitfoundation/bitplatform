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
            Type = "BitColor?",
            LinkType = LinkType.Link,
            Href = "#color-enum",
            DefaultValue = "null",
            Description = "The general color of the BitProgress.",
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
            DefaultValue = "{0:F0} %",
            Description = "The format of the percent number in percentage display.",
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
            Name = "Size",
            Type = "BitSize?",
            LinkType = LinkType.Link,
            Href = "#size-enum",
            DefaultValue = "null",
            Description = "The size of the BitProgress.",
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
            Type = "int?",
            DefaultValue = "null",
            Description = "Thickness of the BitProgress. When not set, the value is determined by the Size parameter.",
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

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
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
<BitProgress Color=""BitColor.Primary"" Percent=""69"" />
<BitProgress Color=""BitColor.Secondary"" Percent=""69"" />
<BitProgress Color=""BitColor.Tertiary"" Percent=""69"" />
<BitProgress Color=""BitColor.Info"" Percent=""69"" />
<BitProgress Color=""BitColor.Success"" Percent=""69"" />
<BitProgress Color=""BitColor.Warning"" Percent=""69"" />
<BitProgress Color=""BitColor.SevereWarning"" Percent=""69"" />
<BitProgress Color=""BitColor.Error"" Percent=""69"" />

<BitProgress Color=""BitColor.Primary"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Secondary"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Tertiary"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Info"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Success"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Warning"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.SevereWarning"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Error"" Circular Percent=""69"" />";

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


<BitProgress Indeterminate Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Thickness=""10"" />

<BitProgress Class=""custom-class""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Circular Indeterminate Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Thickness=""10"" />

<BitProgress Circular
             Class=""custom-class""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Indeterminate
             Thickness=""10""
             Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"",
                                Track = ""background-color: green;"" })"" />

<BitProgress Classes=""@(new() { Bar = ""custom-bar"",
                                 Track = ""custom-tracker""})""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Circular Indeterminate
             Thickness=""10""
             Styles=""@(new() { Bar = ""stroke: greenyellow;"",
                               Track = ""stroke: green;"" })"" />

<BitProgress Circular
             Percent=""69""
             Thickness=""10""
             Classes=""@(new() { Bar = ""custom-circle-bar"",
                                Track = ""custom-circle-tracker""})"" />";

    private readonly string example8RazorCode = @"
<BitProgress Dir=""BitDir.Rtl""
             Thickness=""10""
             Indeterminate />

<BitProgress Label=""لیبل تست""
             Description=""توضیحات تست""
             Dir=""BitDir.Rtl""
             Percent=""69""
             Thickness=""10""
             ShowPercentNumber />

<BitProgress Circular
             Dir=""BitDir.Rtl""
             Thickness=""10""
             Indeterminate />

<BitProgress Circular
             Label=""لیبل تست""
             Description=""توضیحات تست""
             Dir=""BitDir.Rtl""
             Percent=""69""
             Thickness=""10""
             ShowPercentNumber />";

    private readonly string example9RazorCode = @"
<BitProgress Size=""BitSize.Small"" Label=""Small"" Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Medium"" Label=""Medium"" Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Large"" Label=""Large"" Percent=""69"" ShowPercentNumber />

<BitProgress Size=""BitSize.Small"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Medium"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Large"" Circular Percent=""69"" ShowPercentNumber />";

    private readonly string example10RazorCode = @"
<BitProgress Circular Indeterminate Size=""BitSize.Small"" />
<BitProgress Circular Indeterminate Size=""BitSize.Medium"" />
<BitProgress Circular Indeterminate Size=""BitSize.Large"" />

<BitProgress Circular Indeterminate Color=""BitColor.Success"" />
<BitProgress Circular Indeterminate Color=""BitColor.Warning"" />
<BitProgress Circular Indeterminate Color=""BitColor.Error"" />";
}
