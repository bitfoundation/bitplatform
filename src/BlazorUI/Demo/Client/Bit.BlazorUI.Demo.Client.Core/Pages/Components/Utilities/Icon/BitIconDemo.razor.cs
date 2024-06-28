namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Icon;

public partial class BitIconDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "IconName",
            Type = "string",
            LinkType = LinkType.Link,
            Href = "/iconography",
            Description = "The icon name for the icon shown in the button"
        },
        new()
        {
            Name = "Size",
            Type = "BitIconSize",
            DefaultValue = "BitIconSize.Medium",
            Description = "Size of icon.",
            LinkType = LinkType.Link,
            Href = "#iconSize-enum",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "iconSize-enum",
            Name = "BitIconSize",
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
<BitIcon Size=""BitIconSize.Small"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitIconSize.Small"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitIconSize.Small"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitIconSize.Medium"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitIconSize.Medium"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitIconSize.Medium"" IconName=""@BitIconName.Pinned"" />

<BitIcon Size=""BitIconSize.Large"" IconName=""@BitIconName.Accept"" />
<BitIcon Size=""BitIconSize.Large"" IconName=""@BitIconName.Bus"" />
<BitIcon Size=""BitIconSize.Large"" IconName=""@BitIconName.Pinned"" />";
}
