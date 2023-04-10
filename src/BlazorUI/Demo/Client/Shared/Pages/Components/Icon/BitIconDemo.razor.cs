using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Icon;

public partial class BitIconDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Aria label for the root element of the component"
        },
        new()
        {
            Name = "Class",
            Type = "string",
            DefaultValue = "",
            Description = "Custom CSS class for the root element of the component."
        },
        new()
        {
            Name = "Style",
            Type = "string",
            DefaultValue = "",
            Description = "Custom style for the root element of the component",
        },
        new()
        {
            Name = "IconName",
            Type = "BitIconName",
            LinkType = LinkType.Link,
            Href = "/icons",
            DefaultValue = "",
            Description = "The icon name for the icon shown in the button"
        }
    };



    private BitIconName _iconName = BitIconName.Accept;


    private readonly string example1HTMLCode = @"
<BitIcon IconName=""BitIconName.Accept"" AriaLabel=""accept"" />
<BitIcon IconName=""BitIconName.Bus"" AriaLabel=""bus"" />
<BitIcon IconName=""BitIconName.Pinned"" AriaLabel=""pinned"" />";

    private readonly string example2HTMLCode = @"
<style>
    .icon-class {
        font-size: 3rem;
        margin: 1rem 2rem;
    }
</style>
<BitIcon IconName=""BitIconName.Accept"" AriaLabel=""accept"" Class=""icon-class"" />
<BitIcon IconName=""BitIconName.Bus"" AriaLabel=""bus"" Class=""icon-class"" />
<BitIcon IconName=""BitIconName.Pinned"" AriaLabel=""pinned"" Class=""icon-class"" />";

    private readonly string example3HTMLCode = @"
<BitIcon IconName=""BitIconName.Accept"" AriaLabel=""accept"" Style=""font-size: 2rem; margin: 1rem 2rem; color: red;"" />
<BitIcon IconName=""BitIconName.Bus"" AriaLabel=""bus"" Style=""font-size: 2rem; margin: 1rem 2rem; color: green;"" />
<BitIcon IconName=""BitIconName.Pinned"" AriaLabel=""pinned"" Style=""font-size: 2rem; margin: 1rem 2rem; color: mediumpurple;"" />";

    private readonly string example4HTMLCode = @"
<BitIcon IconName=""_iconName"" AriaLabel=""accept"" Class=""icon-class"" />
<BitToggleButton IconName=""_iconName"" OnChange=""() => _iconName = _iconName == BitIconName.Accept ? BitIconName.ChromeClose : BitIconName.Accept"" />";
}
