namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Tag;

public partial class BitTagDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Child content of component, the content that the tag will apply to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the tag.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the Icon to use in the tag."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Click event handler of the tag."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Dismiss button click event, if set the dismiss icon will show up."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the tag.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tag.",
            LinkType = LinkType.Link,
            Href = "#tag-class-styles"
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the tag."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the tag.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "tag-class-styles",
            Title = "BitTagClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTag."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitTag."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitTag."
               },
               new()
               {
                   Name = "DismissButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss button of the BitTag."
               },
               new()
               {
                   Name = "DismissIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dismiss icon of the BitTag."
               },
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



    private bool isDismissed;



    private readonly string example1RazorCode = @"
<BitTag Text=""Basic tag"" />";

    private readonly string example2RazorCode = @"
<BitTag Text=""Fill"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Outline"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Text"" Variant=""BitVariant.Text"" />";

    private readonly string example3RazorCode = @"
<BitTag Text=""Calendar icon"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example4RazorCode = @"
@if (isDismissed is false)
{
    <BitTag IconName=""@BitIconName.AlarmClock"" Text=""Dismiss me"" OnDismiss=""() => isDismissed = true"" />
}

<BitButton IsEnabled=""isDismissed"" Variant=""BitVariant.Outline"" OnClick=""() => isDismissed = false"">
    Dismissed, click to reset
</BitButton>";
    private readonly string example4CsharpCode = @"
private bool isDismissed;";

    private readonly string example5RazorCode = @"
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Text"" />

<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Text"" />

<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"" />

<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" />

<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" />";

    private readonly string example6RazorCode = @"
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Text"" />

<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Text"" />

<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Text"" />";

    private readonly string example7RazorCode = @"
<BitTag>
    <BitStack Horizontal Gap=""0.5rem"" Style=""padding-inline: 0.5rem;"">
        <BitLabel>Custom content</BitLabel>
        <BitRollerLoading CustomSize=""32"" Color=""BitColor.Tertiary"" />
    </BitStack>
</BitTag>";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        border-radius: 0.25rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root {
        color: mediumpurple;
        border-radius: 0.5rem;
        border-color: mediumpurple;
        background-color: transparent;
        box-shadow: mediumpurple 0 0 0.5rem;
    }

    .custom-icon {
        font-size: 1.25rem;
        font-weight: bolder;
    }
</style>


<BitTag Text=""Styled Tag""
        IconName=""@BitIconName.People""
        Style=""border-radius: 1rem; font-weight:bold"" />

<BitTag Text=""Classed Tag""
        IconName=""@BitIconName.People""
        Class=""custom-class"" Variant=""BitVariant.Outline"" />


<BitTag Text=""Styles""
        IconName=""@BitIconName.People""
        Styles=""@(new() { Root = ""border-color: red; background-color: transparent;"",
                          Text = ""color: tomato; font-weight: bold;"",
                          Icon = ""color: tomato;"" })"" />

<BitTag Text=""Classes""
        IconName=""@BitIconName.People""
        Classes=""@(new() { Root = ""custom-root"",
                           Icon = ""custom-icon"" })"" />";
}
