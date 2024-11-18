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
            Description = "The general color of the icon.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "",
            Description = "The icon name for the icon shown",
            LinkType = LinkType.Link,
            Href = "/iconography",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the icon.",
            LinkType = LinkType.Link,
            Href = "#icon-size-enum",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the icon.",
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
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Pinned"" />";

    private readonly string example4RazorCode = @"
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

    private readonly string example5RazorCode = @"
<style>
    .icon-class {
        padding: 4px;
        font-size: 3rem;
        margin-left: 1rem;
        background-color: aquamarine;
    }
</style>

<BitIcon IconName=""@BitIconName.Accept"" Size=""BitSize.Large""
         Style=""background-color: brown; border-radius: 4px"" />
<BitIcon IconName=""@BitIconName.Accept"" Class=""icon-class"" />";
}
