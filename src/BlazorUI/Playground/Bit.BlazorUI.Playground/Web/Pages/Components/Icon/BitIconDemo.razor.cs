using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Icon;

public partial class BitIconDemo
{
    private BitIconName _iconName = BitIconName.Accept;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Aria label for the root element of the component"
        },
        new ComponentParameter()
        {
            Name = "Class",
            Type = "string",
            DefaultValue = "",
            Description = "Custom CSS class for the root element of the component."
        },
        new ComponentParameter()
        {
            Name = "Style",
            Type = "string",
            DefaultValue = "",
            Description = "Custom style for the root element of the component",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIconName",
            LinkType = LinkType.Link,
            Href = "/icons",
            DefaultValue = "",
            Description = "The icon name for the icon shown in the button"
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

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
<BitIcon Icon=""_iconName"" AriaLabel=""accept"" Class=""icon-class"" />
<BitToggleButton IconName=""_iconName"" OnChange=""() => _iconName = _iconName == BitIconName.Accept ? BitIconName.ChromeClose : BitIconName.Accept"" />";
}
