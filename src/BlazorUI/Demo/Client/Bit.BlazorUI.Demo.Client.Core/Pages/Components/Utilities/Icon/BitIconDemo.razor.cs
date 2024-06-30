namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Color",
            Type = "BitColor",
            LinkType = LinkType.Link,
            Href = "#icon-color-enum",
            DefaultValue = "null",
            Description = "The color of icon.",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            LinkType = LinkType.Link,
            Href = "/iconography",
            Description = "The icon name for the icon shown"
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
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "icon-color-enum",
            Name = "BitColor",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Info",
                    Description="Info styled icon.",
                    Value="0",
                },
                new()
                {
                    Name= "Success",
                    Description="Success styled icon.",
                    Value="1",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning styled icon.",
                    Value="2",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="Severe Warning styled icon.",
                    Value="3",
                },
                new()
                {
                    Name= "Error",
                    Description="Error styled icon.",
                    Value="4",
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
        }
    ];



    private readonly string example1RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" AriaLabel=""accept"" />
<BitIcon IconName=""@BitIconName.Bus"" AriaLabel=""bus"" />
<BitIcon IconName=""@BitIconName.Pinned"" AriaLabel=""pinned"" />";

    private readonly string example2RazorCode = @"
<style>
    .icon-class {
        font-size: 3rem;
        margin: 1rem 2rem;
    }
</style>
<BitIcon IconName=""@BitIconName.Accept"" AriaLabel=""accept"" Class=""icon-class"" />
<BitIcon IconName=""@BitIconName.Bus"" AriaLabel=""bus"" Class=""icon-class"" />
<BitIcon IconName=""@BitIconName.Pinned"" AriaLabel=""pinned"" Class=""icon-class"" />";

    private readonly string example3RazorCode = @"
<BitIcon IconName=""@BitIconName.Accept"" AriaLabel=""accept"" Style=""font-size: 2rem; margin: 1rem 2rem; color: red;"" />
<BitIcon IconName=""@BitIconName.Bus"" AriaLabel=""bus"" Style=""font-size: 2rem; margin: 1rem 2rem; color: green;"" />
<BitIcon IconName=""@BitIconName.Pinned"" AriaLabel=""pinned"" Style=""font-size: 2rem; margin: 1rem 2rem; color: mediumpurple;"" />";

    private readonly string example4RazorCode = @"
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Small"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Medium"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitSize.Large"" IconName=""@BitIconName.Pinned"" />";

    private readonly string example5RazorCode = @"
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
}
