namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Badge;

public partial class BitBadgeDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the badge will apply to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitBadge.",
            LinkType = LinkType.Link,
            Href = "#badge-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the badge.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Content",
            Type = "object?",
            DefaultValue = "null",
            Description = "Content you want inside the badge. Supported types are string and integer."
        },
        new()
        {
            Name = "Dot",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reduces the size of the badge and hide any of its content."
        },
        new()
        {
            Name = "Hidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "The visibility of the badge."
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the Icon to use in the badge."
        },
        new()
        {
            Name = "Max",
            Type = "int?",
            DefaultValue = "null",
            Description = "Max value to display when content is integer type."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Button click event if set."
        },
        new()
        {
            Name = "Overlap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Overlaps the badge on top of the child content."
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "The position of the badge.",
            LinkType = LinkType.Link,
            Href = "#position-enum"
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of badge, Possible values: Small | Medium | Large",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitBadge.",
            LinkType = LinkType.Link,
            Href = "#badge-class-styles"
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the badge.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
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
                    Description="Info Primary general color.",
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
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size badge.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size badge.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size badge.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "position-enum",
            Name = "BitPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "TopLeft",
                    Value = "0"
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2"
                },
                new()
                {
                    Name = "CenterLeft",
                    Value = "3"
                },
                new()
                {
                    Name = "Center",
                    Value = "4"
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "5"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "6"
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "7"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "8"
                }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "badge-class-styles",
            Title = "BitBadgeClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitBadge."
               },
               new()
               {
                   Name = "BadgeWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge wrapper of the BitBadge."
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge of the BitBadge."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitBadge."
               },
            ]
        }
    ];



    private bool hidden;
    private BitPosition badgePosition;
    private List<BitDropdownItem<BitPosition>> badgePositionList = Enum.GetValues(typeof(BitPosition))
        .Cast<BitPosition>()
        .Select(enumValue => new BitDropdownItem<BitPosition>
        {
            Value = enumValue,
            Text = enumValue.ToString()
        })
        .ToList();

    private int counter;



    private readonly string example1RazorCode = @"
<BitBadge Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example2RazorCode = @"
<BitBadge Content=""84"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""84"" Variant=""BitVariant.Fill"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example3RazorCode = @"
<BitBadge Content=""63"" Overlap>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example4RazorCode = @"
<BitBadge Dot>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example5RazorCode = @"
<BitBadge Max=""63"" Content=""100"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example6RazorCode = @"
<BitBadge Content=""@(""Text"")"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge IconName=""@BitIconName.Ringer"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""hidden"" Label=""Hide the badge"" />

<BitBadge Hidden=""hidden"" Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";
    private readonly string example7CsharpCode = @"
private bool hidden;";

    private readonly string example8RazorCode = @"
<BitBadge Content=""63"" Position=""badgePosition"">
    <BitButton Variant=""BitVariant.Outline"">Position</BitButton>
</BitBadge>

<BitDropdown Items=""badgePositionList"" @bind-Value=""badgePosition"" Style=""width: 8rem;"" />";
    private readonly string example8CsharpCode = @"
private BitPosition badgePosition;

private List<BitDropdownItem<BitPosition>> badgePositionList = Enum.GetValues(typeof(BitPosition))
    .Cast<BitPosition>()
    .Select(enumValue => new BitDropdownItem<BitPosition>
    {
        Value = enumValue,
        Text = enumValue.ToString()
    })
    .ToList();";

    private readonly string example9RazorCode = @"
<BitBadge Content=""84"" Color=""BitColor.Primary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Secondary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Tertiary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Info"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Info"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Success"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Success"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Warning"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.SevereWarning"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Color=""BitColor.Error"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.Error"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example10RazorCode = @"
<BitBadge Content=""84"" Size=""BitSize.Small"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Small"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Size=""BitSize.Medium"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Medium"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" Size=""BitSize.Large"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Large"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example11RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-class div {
        padding: 0.5rem;
        color: blueviolet;
    }

    .custom-root {
        margin-left: 2rem;
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-wrapper {
        padding: 1rem;
    }

    .custom-badge {
        border-end-end-radius: 0.5rem;
        border-start-end-radius: unset;
        border-end-start-radius: unset;
        border-start-start-radius: 0.5rem;
    }

    .custom-icon {
        color: dodgerblue;
    }
</style>

<BitBadge Content=""84"" Style=""color: dodgerblue;"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Class=""custom-class"" Variant=""BitVariant.Outline"">
    <div>Anchor</div>
</BitBadge>


<BitBadge Content=""84"" IconName=""@BitIconName.Info""
           Styles=""@(new() { Root = ""color: tomato;"",
                             Badge = ""border-radius: unset;"",
                             Icon = ""color: tomato;"" })"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" IconName=""@BitIconName.Info""
          Variant=""BitVariant.Outline""
          Classes=""@(new() { Root = ""custom-root"",
                             BadgeWrapper = ""custom-wrapper"",
                             Badge = ""custom-badge"",
                             Icon = ""custom-icon"" })"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example12RazorCode = @"
<BitBadge Content=""counter"" OnClick=""() => counter++"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";
    private readonly string example12CsharpCode = @"
private int counter;";

}
