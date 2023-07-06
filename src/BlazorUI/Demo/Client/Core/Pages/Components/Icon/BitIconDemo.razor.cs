using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Icon;

public partial class BitIconDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "IconName",
            Type = "BitIconName",
            LinkType = LinkType.Link,
            Href = "/iconography",
            DefaultValue = "BitIconName.NotSet",
            Description = "The icon name for the icon shown in the button"
        }
    };



    private BitIconName iconName = BitIconName.Accept;


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
< BitIcon IconName=""iconName"" AriaLabel=""accept"" Class=""icon-class"" />
<BitToggleButton IconName=""iconName"" OnChange=""() => iconName = iconName == BitIconName.Accept ? BitIconName.ChromeClose : BitIconName.Accept"" />";
    private readonly string example4CSharpCode = @"
private BitIconName iconName = BitIconName.Accept;";
}
