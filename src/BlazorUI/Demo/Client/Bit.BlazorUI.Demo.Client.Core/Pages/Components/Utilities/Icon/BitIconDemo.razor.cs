namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "Color",
            Type = "BitSeverity?",
            DefaultValue = "null",
            Description = "The severity of the icon.",
            LinkType = LinkType.Link,
            Href = "#severity-enum",
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
            Id = "severity-enum",
            Name = "BitSeverity",
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
<BitIcon Color=""BitSeverity.Info"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitSeverity.Info"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitSeverity.Info"" IconName=""@BitIconName.Pinned"" />

<BitIcon Color=""BitSeverity.Success"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitSeverity.Success"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitSeverity.Success"" IconName=""@BitIconName.Pinned"" />
                
<BitIcon Color=""BitSeverity.Warning"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitSeverity.Warning"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitSeverity.Warning"" IconName=""@BitIconName.Pinned"" />
                
<BitIcon Color=""BitSeverity.SevereWarning"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitSeverity.SevereWarning"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitSeverity.SevereWarning"" IconName=""@BitIconName.Pinned"" />
                
<BitIcon Color=""BitSeverity.Error"" IconName=""@BitIconName.Accept"" />
<BitIcon Color=""BitSeverity.Error"" IconName=""@BitIconName.Bus"" />
<BitIcon Color=""BitSeverity.Error"" IconName=""@BitIconName.Pinned"" />";
}
