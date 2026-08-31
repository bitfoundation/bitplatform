namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Label;

public partial class BitLabelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the label, which can be a text or any custom markup. A form control put inside it is named by the label without needing the For parameter.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitLabelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the label.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the label. The label inherits the color of its container while this is not set.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. The default is \"label\", and a name that is not one a tag can have falls back to it.",
        },
        new()
        {
            Name = "For",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the form control this label is bound to, rendered as the \"for\" attribute of the label element. It is ignored while the Element parameter renders another tag.",
        },
        new()
        {
            Name = "NoSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the text of the label from being selected, which is what a double click on a label does instead of reaching the control it names.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the label on a single line and truncates the overflow with an ellipsis.",
        },
        new()
        {
            Name = "Optional",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the associated field is optional, which renders an indicator after the content of the label. It is ignored while Required is set.",
        },
        new()
        {
            Name = "OptionalTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template of the optional indicator of the label. Takes precedence over OptionalText.",
        },
        new()
        {
            Name = "OptionalText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the optional indicator of the label. The default is \"(optional)\".",
        },
        new()
        {
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the associated field is required, which renders an indicator after the content of the label. The default asterisk is hidden from assistive technologies.",
        },
        new()
        {
            Name = "RequiredTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template of the required indicator of the label. Takes precedence over RequiredText.",
        },
        new()
        {
            Name = "RequiredText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the required indicator of the label. The default is \"*\".",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the label. The default is the medium size.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLabelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the label.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "VisuallyHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the label from the page while keeping it available to assistive technologies, so it still names its control.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitLabelClassStyles",
            Description = "The custom CSS classes/styles for the different parts of the label.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the label.",
                },
                new()
                {
                    Name = "RequiredIndicator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the required indicator of the label, which only exists while Required is set.",
                },
                new()
                {
                    Name = "OptionalIndicator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the optional indicator of the label, which only exists while Optional is set and Required is not.",
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
                new()
                {
                    Name= "Primary",
                    Description="Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<BitLabel>I'm a Label</BitLabel>
<BitLabel IsEnabled=""false"">I'm a disabled Label</BitLabel>";

    private readonly string example2RazorCode = @"
<BitLabel For=""label-input"">A Label for an input</BitLabel>
<input type=""text"" name=""label-input"" id=""label-input"" />

<BitLabel><input type=""checkbox"" /> A Label wrapping its own control</BitLabel>";

    private readonly string example3RazorCode = @"
<BitLabel Required>I'm a required Label</BitLabel>

<BitLabel Required RequiredText=""(required)"">A required Label with a word instead of the asterisk</BitLabel>

<BitLabel Required>
    <ChildContent>A required Label with a custom template</ChildContent>
    <RequiredTemplate>
        <BitIcon IconName=""@BitIconName.Important"" Color=""BitColor.Error"" Size=""BitSize.Small"" />
    </RequiredTemplate>
</BitLabel>";

    private readonly string example4RazorCode = @"
<BitLabel Optional>I'm an optional Label</BitLabel>

<BitLabel Optional OptionalText=""- if you have one"">An optional Label with its own text</BitLabel>

<BitLabel Optional>
    <ChildContent>An optional Label with a custom template</ChildContent>
    <OptionalTemplate>
        <BitTag Text=""optional"" Size=""BitSize.Small"" Color=""BitColor.Tertiary"" />
    </OptionalTemplate>
</BitLabel>";

    private readonly string example5RazorCode = @"
<BitLabel Element=""div"" Id=""favorite-color-label"" Required>Favorite color</BitLabel>
<div role=""radiogroup"" aria-labelledby=""favorite-color-label"">
    <label><input type=""radio"" name=""favorite-color"" /> Red</label>
    <label><input type=""radio"" name=""favorite-color"" /> Green</label>
    <label><input type=""radio"" name=""favorite-color"" /> Blue</label>
</div>

<fieldset>
    <BitLabel Element=""legend"" Optional>Delivery notes</BitLabel>
    <label><input type=""checkbox"" /> Leave with a neighbour</label>
    <label><input type=""checkbox"" /> Ring the doorbell</label>
</fieldset>";

    private readonly string example6RazorCode = @"
<BitLabel Style=""width:220px"">A caption long enough to need more than one line at this width</BitLabel>

<BitLabel Style=""width:220px"" NoWrap>A caption long enough to need more than one line at this width</BitLabel>";

    private readonly string example7RazorCode = @"
<BitLabel For=""selectable-checkbox""><input type=""checkbox"" id=""selectable-checkbox"" /> Selectable caption</BitLabel>

<BitLabel NoSelect For=""unselectable-checkbox""><input type=""checkbox"" id=""unselectable-checkbox"" /> Unselectable caption</BitLabel>";

    private readonly string example8RazorCode = @"
<BitLabel VisuallyHidden For=""search-input"">Search the documentation</BitLabel>
<input type=""search"" id=""search-input"" placeholder=""Search..."" />";

    private readonly string example9RazorCode = @"
Visible: [ <BitLabel Visibility=""BitVisibility.Visible"">Visible Label</BitLabel> ]
Hidden: [ <BitLabel Visibility=""BitVisibility.Hidden"">Hidden Label</BitLabel> ]
Collapsed: [ <BitLabel Visibility=""BitVisibility.Collapsed"">Collapsed Label</BitLabel> ]";

    private readonly string example10RazorCode = @"
<BitLabel Color=""BitColor.Primary"">Primary</BitLabel>
<BitLabel Color=""BitColor.Secondary"">Secondary</BitLabel>
<BitLabel Color=""BitColor.Tertiary"">Tertiary</BitLabel>
<BitLabel Color=""BitColor.Info"">Info</BitLabel>
<BitLabel Color=""BitColor.Success"">Success</BitLabel>
<BitLabel Color=""BitColor.Warning"">Warning</BitLabel>
<BitLabel Color=""BitColor.SevereWarning"">SevereWarning</BitLabel>
<BitLabel Color=""BitColor.Error"">Error</BitLabel>
<BitLabel Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitLabel>
<BitLabel Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitLabel>
<BitLabel Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitLabel>
<BitLabel Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitLabel>
<BitLabel Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitLabel>
<BitLabel Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitLabel>";

    private readonly string example11RazorCode = @"
<BitLabel Size=""BitSize.Small"" Required>Small</BitLabel>
<BitLabel Size=""BitSize.Medium"" Required>Medium</BitLabel>
<BitLabel Size=""BitSize.Large"" Required>Large</BitLabel>";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border: 1px solid red;
        max-width: max-content;
    }

    .custom-root {
        text-transform: uppercase;
        letter-spacing: 0.05rem;
    }

    .custom-optional {
        color: mediumseagreen;
        font-style: italic;
    }
</style>

<BitLabel Style=""color: dodgerblue; font-weight: bold"">I'm a Label with Style</BitLabel>
<BitLabel Class=""custom-class"">I'm a Label with Class</BitLabel>

<BitLabel Required Styles=""@(new() { Root = ""font-style: italic"", RequiredIndicator = ""color: blueviolet; font-size: 1rem"" })"">
    I'm a Label with Styles
</BitLabel>

<BitLabel Optional Classes=""@(new() { Root = ""custom-root"", OptionalIndicator = ""custom-optional"" })"">
    I'm a Label with Classes
</BitLabel>";

    private readonly string example13RazorCode = @"
<BitLabel Dir=""BitDir.Rtl"">من یک برچسب هستم</BitLabel>

<BitLabel Dir=""BitDir.Rtl"" Required>من یک برچسب الزامی هستم</BitLabel>

<BitLabel Dir=""BitDir.Rtl"" Optional OptionalText=""(اختیاری)"">من یک برچسب اختیاری هستم</BitLabel>";
}
