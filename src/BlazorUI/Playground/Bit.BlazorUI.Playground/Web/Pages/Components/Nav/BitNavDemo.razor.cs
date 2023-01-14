using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Nav;

public partial class BitNavDemo
{
    private readonly List<BitNavItem> BasicNavItems = new()
    {
        new BitNavItem
        {
            Name = "Home",
            Url = "http://example.com",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name = "Activity", Url = "http://msn.com", Target="_blank" },
                new BitNavItem {
                    Name = "MSN",
                    Url = "http://msn.com",
                    Target = "_blank",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem { Name = "Activity", Url = "http://msn.com", Target="_blank" },
                        new BitNavItem { Name = "MSN", Url = "http://msn.com", IsEnabled = false, Target = "_blank" }
                    }
                }
            }
        },
        new BitNavItem { Name = "Documents", Url = "http://example.com", Target = "_blank", IsExpanded = true },
        new BitNavItem { Name = "Pages", Url = "http://msn.com", Target = "_parent" },
        new BitNavItem { Name = "Notebook", Url = "http://msn.com", IsEnabled = false },
        new BitNavItem { Name = "Communication and Media", Url = "http://msn.com", Target = "_top" },
        new BitNavItem { Name = "News", Title = "News", Url = "http://msn.com", IconName = BitIconName.News, Target = "_self" },
    };

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<BitNavLinkItem>",
            DefaultValue = "",
            LinkType = LinkType.Link,
            Href = "#navLinkItem",
            Description = "Used to customize how content inside the group header is rendered.",
        },
        new ComponentParameter()
        {
            Name = "InitialSelectedKey",
            Type = "string",
            DefaultValue = "",
            Description = "(Optional) The key of the nav item initially selected in manual mode.",
        },
        new ComponentParameter()
        {
            Name = "LinkTemplate",
            Type = "RenderFragment<BitNavLinkItem>",
            DefaultValue = "",
            LinkType = LinkType.Link,
            Href = "#navLinkItem",
            Description = "Used to customize how content inside the link tag is rendered.",
        },
        new ComponentParameter()
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            LinkType = LinkType.Link,
            Href = "#nav-mode-enum",
            Description = "Determines how the navigation will be handled. The default value is Automatic",
        },
        new ComponentParameter()
        {
            Name = "NavLinkItems",
            Type = "ICollection<BitNavLinkItem>",
            LinkType = LinkType.Link,
            Href = "#navLinkItem",
            DefaultValue = "",
            Description = "A collection of link items to display in the navigation bar.",
        },
        new ComponentParameter()
        {
            Name = "OnLinkClick",
            Type = "EventCallback<BitNavLinkItem>",
            DefaultValue = "",
            Description = "Function callback invoked when a link in the navigation is clicked.",
        },
        new ComponentParameter()
        {
            Name = "OnLinkExpandClick",
            Type = "EventCallback<BitNavLinkItem>",
            LinkType = LinkType.Link,
            Href = "#navLinkItem",
            DefaultValue = "",
            Description = "Function callback invoked when the chevron on a link is clicked.",
        },
        new ComponentParameter()
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            Href = "#nav-render-type-enum",
            LinkType = LinkType.Link,
            DefaultValue = "RenderType.Normal",
            Description = "The way to render nav links.",
        },
        new ComponentParameter()
        {
            Name = "SelectedKey",
            Type = "string",
            DefaultValue = "",
            Description = "The key of the nav item selected by caller.",
        },
    };

    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "navLinkItem",
            Title = "BitNavLinkItem",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavLinkItemAriaCurrent",
                   DefaultValue = "Page",
                   Description = "Aria-current token for active nav links. Must be a valid token value, and defaults to 'page'",
               },
               new ComponentParameter()
               {
                   Name = "AriaLabel",
                   Type = "string",
                   DefaultValue = "",
                   Description = "Aria label for nav link. Ignored if collapseAriaLabel or expandAriaLabel is provided",
               },
               new ComponentParameter()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string",
                   DefaultValue = "",
                   Description = "ARIA label when items is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string",
                   DefaultValue = "",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName",
                   DefaultValue = "",
                   Description = "Name of an icon to render next to this link button.",
               },
               new ComponentParameter()
               {
                   Name = "IsCollapseByDefault",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "If true, the group should render collapsed by default.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "Whether or not the link is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new ComponentParameter()
               {
                   Name = "ItemStyle",
                   Type = "string?",
                   DefaultValue = "",
                   Description = "Custom style for the each item element.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string",
                   DefaultValue = "",
                   Description = "A unique value to use as a key or id of the item, used when rendering the list of links and for tracking the currently selected link.",
               },
               new ComponentParameter()
               {
                   Name = "Links",
                   Type = "IEnumerable<BitNavLinkItem>",
                   DefaultValue = "",
                   Description = "A list of items to render as children of the current item.",
               },
               new ComponentParameter()
               {
                   Name = "Name",
                   Type = "string",
                   DefaultValue = "",
                   Description = "Text to render for this link.",
               },
               new ComponentParameter()
               {
                   Name = "OnClick",
                   Type = "Action<BitNavLinkItem>",
                   DefaultValue = "",
                   Description = "Callback invoked when a link in the navigation is clicked.",
               },
               new ComponentParameter()
               {
                   Name = "OnHeaderClick",
                   Type = "Action<bool>",
                   DefaultValue = "",
                   Description = "Callback invoked when a group header is clicked.",
               },
               new ComponentParameter()
               {
                   Name = "Target",
                   Type = "string",
                   DefaultValue = "",
                   Description = "Link target, specifies how to open the link.",
               },
               new ComponentParameter()
               {
                   Name = "Title",
                   Type = "string",
                   DefaultValue = "",
                   Description = "Text for title tooltip.",
               },
               new ComponentParameter()
               {
                   Name = "Url",
                   Type = "string",
                   DefaultValue = "",
                   Description = "URL to navigate to for this link.",
               }
            }
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "nav-mode-enum",
            Title = "BitNavMode Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Automatic",
                    Description="The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Manual",
                    Description="Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-render-type-enum",
            Title = "BitNavRenderType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Normal",
                    Description="",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Grouped",
                    Description="",
                    Value="1",
                }
            }
        },
    };

    #region Sample Code 1

    private static string example1HTMLCode = @"
";

    private static string example1CSharpCode = @"
";

    #endregion
}
