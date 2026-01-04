namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "Specifies the color theme of the icon. Default value is BitColor.Primary.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Specifies the icon configuration for rendering icons from external icon libraries using custom CSS classes. Takes precedence over IconName when both are set.",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "",
            Description = "Specifies the name of the icon from the built-in Fluent UI icon library. This property is ignored when Icon is set.",
            LinkType = LinkType.Link,
            Href = "/iconography",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Specifies the size of the icon. Default value is BitSize.Medium.",
            LinkType = LinkType.Link,
            Href = "#icon-size-enum",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "Specifies the visual styling variant of the icon. Default value is BitVariant.Text.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
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
            Id = "icon-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "Display icon using small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "Display icon using medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "Display icon using large size.",
                    Value = "2",
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
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" />
<BitIcon IconName=""@BitIconName.Bus"" />
<BitIcon IconName=""@BitIconName.Pinned"" />

<BitIcon IconName=""@BitIconName.Accept"" IsEnabled=""false"" />
<BitIcon IconName=""@BitIconName.Bus"" IsEnabled=""false"" />
<BitIcon IconName=""@BitIconName.Pinned"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Fill"" />

<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Outline"" />

<BitIcon IconName=""@BitIconName.Accept"" Variant=""BitVariant.Text"" />";

    private readonly string example3RazorCode = @"
<BitIcon Color=""BitColor.Primary"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Primary"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Primary"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Secondary"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Secondary"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Secondary"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Tertiary"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Tertiary"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Tertiary"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Info"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Info"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Info"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Success"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Success"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Success"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Warning"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Warning"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Warning"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.SevereWarning"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.SevereWarning"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.SevereWarning"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitColor.Error"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitColor.Error"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitColor.Error"" IconName=""@BitIconName.Pinned"" />";

    private readonly string example4RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitIcon Icon=""@(""fa-solid fa-house"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Fa(""solid rocket"")"" Color=""BitColor.Secondary"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitIcon Icon=""@(""bi bi-house-fill"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Color=""BitColor.Error"" />
<BitIcon Icon=""@BitIconInfo.Bi(""github"")"" Size=""BitSize.Large"" />
<BitIcon Icon=""@BitIconInfo.Bi(""gear-fill"")"" Color=""BitColor.Secondary"" />";

    private readonly string example5RazorCode = @"
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Pinned"" />";

    private readonly string example6RazorCode = @"
<style>
    .icon-class {
        padding: 4px;
        font-size: 3rem;
        margin-left: 1rem;
        background-color: aquamarine;
    }
</style>

<BitIcon Size=""BitSize.Large""
         IconName=""@BitIconName.Accept"" 
         Style=""background-color: brown; border-radius: 4px"" />

<BitIcon Class=""icon-class""
         IconName=""@BitIconName.Accept"" />";
}
