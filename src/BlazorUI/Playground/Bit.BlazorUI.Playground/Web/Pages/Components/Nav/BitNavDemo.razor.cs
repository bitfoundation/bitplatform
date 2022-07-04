using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Nav;

public partial class BitNavDemo
{
    private readonly List<BitNavLinkItem> BasicNavLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "Home",
            Url = "http://example.com",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank" },
                new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" }
            }
        },
        new BitNavLinkItem { Name = "Documents", Url = "http://example.com", Key = "key3", Target = "_blank", IsExpanded = true },
        new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
        new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false },
        new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
        new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key7", IconName = BitIconName.News, Target = "_self" },
    };

    private readonly List<BitNavLinkItem> BasicNavLinksWithoutURL = new()
    {
        new BitNavLinkItem
        {
            Name = "Home",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "Activity", Key = "key1", Target="_blank" },
                new BitNavLinkItem { Name = "MSN", Key = "key2", IsEnabled = false, Target = "_blank" }
            }
        },
        new BitNavLinkItem { Name = "Documents", Key = "key3", Target = "_blank", IsExpanded = true },
        new BitNavLinkItem { Name = "Pages", Key = "key4", Target = "_parent" },
        new BitNavLinkItem { Name = "Notebook", Key = "key5", IsEnabled = false },
        new BitNavLinkItem { Name = "Communication and Media", Key = "key6", Target = "_top" },
        new BitNavLinkItem { Name = "News", Title = "News", Key = "key7", IconName = BitIconName.News, Target = "_self" },
    };

    private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "Home",
            Url = "http://example.com",
            Title = "",
            IsExpanded = true,
            CollapseAriaLabel = "Collapse Home section",
            ExpandAriaLabel = "Expand Home section",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank", Title = "" },
                new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank", Title = "" }
            }
        },
        new BitNavLinkItem { Name = "Shared Documents and Files", Url = "http://example.com", Key = "key3", Target = "_blank", Title = "" },
        new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent", Title = "" },
        new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false, Title = "" },
        new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top", Title = "" },
        new BitNavLinkItem { Name = "News", Key = "key7", Url = "http://cnn.com", IconName = BitIconName.News, Target = "_self", Title = "" }
    };

    private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "Basic components",
            CollapseAriaLabel = "Collapse Basic components section",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name= "ActivityItem", Key = "ActivityItem", Url = "#/examples/activityitem" },
                new BitNavLinkItem { Name= "Breadcrumb", Key = "Breadcrumb", Url = "#/examples/breadcrumb" },
                new BitNavLinkItem { Name= "Button", Key = "Button", Url = "#/examples/button" }
            }
        },
        new BitNavLinkItem
        {
            Name = "Extended components",
            CollapseAriaLabel = "Collapse Extended components section",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "ColorPicker", Key = "ColorPicker", Url ="#/examples/colorpicker" },
                new BitNavLinkItem { Name = "ExtendedPeoplePicker", Key = "ExtendedPeoplePicker", Url ="#/examples/extendedpeoplepicker" },
                new BitNavLinkItem { Name = "GroupedList", Key = "GroupedList", Url ="#/examples/groupedlist" }
            }
        },
        new BitNavLinkItem
        {
            Name = "Utilities",
            CollapseAriaLabel = "Collapse Utilities section",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "FocusTrapZone", Key = "FocusTrapZone", Url = "#/examples/focustrapzone" },
                new BitNavLinkItem { Name = "FocusZone", Key = "FocusZone", Url = "#/examples/focuszone" },
                new BitNavLinkItem { Name = "MarqueeSelection", Key = "MarqueeSelection", Url = "#/examples/marqueeselection" }
            }
        }
    };

    private readonly List<BitNavLinkItem> NestedLinks = new()
    {
        new()
        {
            Name = "Parent link 1",
            Url = "http://example.com",
            Key = "Key1",
            Title = "Parent link 1",
            CollapseAriaLabel = "Collapse Parent link 1",
            Links = new List<BitNavLinkItem>()
            {
                new()
                {
                    Name = "Child link 1",
                    Url = "http://msn.com",
                    Key = "Key1-1",
                    Title = "Child link 1",
                    Links = new List<BitNavLinkItem>()
                    {
                        new() { Name = "3rd level link 1", Title = "3rd level link 1", Url = "http://msn.com", Key = "Key1-1-1" },
                        new() { Name = "3rd level link 2", Title = "3rd level link 2", Url = "http://msn.com", Key = "Key1-1-2", IsEnabled = false }
                    }
                },
                new() { Name = "Child link 2", Title = "Child link 2", Url = "http://msn.com", Key = "Key1-2" },
                new() { Name = "Child link 3", Title = "Child link 3", Url = "http://msn.com", Key = "Key1-3", IsEnabled = false },
            }
        },
        new()
        {
            Name = "Parent link 2",
            Title = "Parent link 2",
            Url = "http://example.com",
            Key = "Key2",
            CollapseAriaLabel = "Collapse Parent link 2",
            Links = new List<BitNavLinkItem>()
            {
                new() { Name = "Child link 4", Title = "Child link 4", Url = "http://example.com", Key = "Key2-1" }
            }
        }
    };

    private readonly List<BitNavLinkItem> CustomHeaderLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "Pages",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "Key1-1", Title = "Activity" },
                new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "Key1-2" },
            }
        },
        new BitNavLinkItem
        {
            Name = "More pages",
            IsExpanded = true,
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name= "Settings", Title = "Settings", Url = "http://example.com", Key = "Key2-1" },
                new BitNavLinkItem { Name= "Notes", Title = "Notes", Url = "http://example.com", Key = "Key2-1" }
            }
        }
    };

    private string BitNavManualModeSelectedKey = "key3";

    private void HandleSelectedKeyChange(string selectedKey)
    {
        BitNavManualModeSelectedKey = selectedKey;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "The aria-label of the control for the benefit of screen readers.",
        },
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
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "",
            Description = "Capture and render additional attributes in addition to the component's parameters.",
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
            Name = "IsEnabled",
            Type = "bool",
            DefaultValue = "",
            Description = "Whether or not the component is enabled.",
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

    private static string example1HTMLCode = @"<BitNav Style=""width: 208px;
       height: 350px;
       box-sizing: border-box;
       border: 1px solid #eee;
       overflow-y: auto;""
        NavLinkItems=""BasicNavLinks""
        AriaLabel=""Nav basic example""
        SelectedKey=""key3"">
</BitNav>";

    private static string example1CSharpCode = @"
private readonly List<BitNavLinkItem> BasicNavLinks = new()
{
    new BitNavLinkItem
    {
        Name = ""Home"",
        Url = ""http://example.com"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name = ""Activity"", Url = ""http://msn.com"", Key = ""key1"", Target=""_blank"" },
            new BitNavLinkItem { Name = ""MSN"", Url = ""http://msn.com"", Key = ""key2"", IsEnabled = false, Target = ""_blank"" }
        }
    },
    new BitNavLinkItem { Name = ""Documents"", Url = ""http://example.com"", Key = ""key3"", Target = ""_blank"", IsExpanded = true },
    new BitNavLinkItem { Name = ""Pages"", Url = ""http://msn.com"", Key = ""key4"", Target = ""_parent"" },
    new BitNavLinkItem { Name = ""Notebook"", Url = ""http://msn.com"", Key = ""key5"", IsEnabled = false },
    new BitNavLinkItem { Name = ""Communication and Media"", Url = ""http://msn.com"", Key = ""key6"", Target = ""_top"" },
    new BitNavLinkItem { Name = ""News"", Title = ""News"", Url = ""http://msn.com"", Key = ""key7"", IconName = BitIconName.News, Target = ""_self"" },
};";

    private static string example2HTMLCode = @"<BitNav Style=""width: 208px;
       height: 350px;
       box-sizing: border-box;
       border: 1px solid #eee;
       overflow-y: auto;""
        Class=""bit-nav-wrapped-link""
        SelectedKey=""key6""
        AriaLabel=""Nav example with wrapped link text""
        NavLinkItems=""BasicNoToolTipNavLinks"">
</BitNav>";

    private static string example2CSharpCode = @"
private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new()
{
    new BitNavLinkItem
    {
        Name = ""Home"",
        Url = ""http://example.com"",
        Title = """",
        IsExpanded = true,
        CollapseAriaLabel = ""Collapse Home section"",
        ExpandAriaLabel = ""Expand Home section"",
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name = ""Activity"", Url = ""http://msn.com"", Key = ""key1"", Target=""_blank"", Title = """" },
            new BitNavLinkItem { Name = ""MSN"", Url = ""http://msn.com"", Key = ""key2"", IsEnabled = false, Target = ""_blank"", Title = """" }
        }
    },
    new BitNavLinkItem { Name = ""Shared Documents and Files"", Url = ""http://example.com"", Key = ""key3"", Target = ""_blank"", Title = """" },
    new BitNavLinkItem { Name = ""Pages"", Url = ""http://msn.com"", Key = ""key4"", Target = ""_parent"", Title = """" },
    new BitNavLinkItem { Name = ""Notebook"", Url = ""http://msn.com"", Key = ""key5"", IsEnabled = false, Title = """" },
    new BitNavLinkItem { Name = ""Communication and Media"", Url = ""http://msn.com"", Key = ""key6"", Target = ""_top"", Title = """" },
    new BitNavLinkItem { Name = ""News"", Key = ""key7"", Url = ""http://cnn.com"", IconName = BitIconName.News, Target = ""_self"", Title = """" }
};";

    private static string example3HTMLCode = @"<BitNav Style=""width: 300px;""
        NavLinkItems=""BasicNoUrlNavLinks""
        RenderType=""BitNavRenderType.Grouped""
        AriaLabel=""Nav example similar to one found in this demo page"">
</BitNav>";

    private static string example3CSharpCode = @"
private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new()
{
    new BitNavLinkItem
    {
        Name = ""Basic components"",
        CollapseAriaLabel = ""Collapse Basic components section"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name= ""ActivityItem"", Key = ""ActivityItem"", Url = ""#/examples/activityitem"" },
            new BitNavLinkItem { Name= ""Breadcrumb"", Key = ""Breadcrumb"", Url = ""#/examples/breadcrumb"" },
            new BitNavLinkItem { Name= ""Button"", Key = ""Button"", Url = ""#/examples/button"" }
        }
    },
    new BitNavLinkItem
    {
        Name = ""Extended components"",
        CollapseAriaLabel = ""Collapse Extended components section"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name = ""ColorPicker"", Key = ""ColorPicker"", Url =""#/examples/colorpicker"" },
            new BitNavLinkItem { Name = ""ExtendedPeoplePicker"", Key = ""ExtendedPeoplePicker"", Url =""#/examples/extendedpeoplepicker"" },
            new BitNavLinkItem { Name = ""GroupedList"", Key = ""GroupedList"", Url =""#/examples/groupedlist"" }
        }
    },
    new BitNavLinkItem
    {
        Name = ""Utilities"",
        CollapseAriaLabel = ""Collapse Utilities section"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name = ""FocusTrapZone"", Key = ""FocusTrapZone"", Url = ""#/examples/focustrapzone"" },
            new BitNavLinkItem { Name = ""FocusZone"", Key = ""FocusZone"", Url = ""#/examples/focuszone"" },
            new BitNavLinkItem { Name = ""MarqueeSelection"", Key = ""MarqueeSelection"", Url = ""#/examples/marqueeselection"" }
        }
    }
};";

    private static string example4HTMLCode = @"<BitNav NavLinkItems=""NestedLinks""
        AriaLabel=""Nav example with nested links""
        SelectedKey=""Key1-1-2"">
</BitNav>";

    private static string example4CSharpCode = @"
private readonly List<BitNavLinkItem> NestedLinks = new()
{
    new()
    {
        Name = ""Parent link 1"",
        Url = ""http://example.com"",
        Key = ""Key1"",
        Title = ""Parent link 1"",
        CollapseAriaLabel = ""Collapse Parent link 1"",
        Links = new List<BitNavLinkItem>()
        {
            new()
            {
                Name = ""Child link 1"",
                Url = ""http://msn.com"",
                Key = ""Key1-1"",
                Title = ""Child link 1"",
                Links = new List<BitNavLinkItem>()
                {
                    new() { Name = ""3rd level link 1"", Title = ""3rd level link 1"", Url = ""http://msn.com"", Key = ""Key1-1-1"" },
                    new() { Name = ""3rd level link 2"", Title = ""3rd level link 2"", Url = ""http://msn.com"", Key = ""Key1-1-2"", IsEnabled = false }
                }
            },
            new() { Name = ""Child link 2"", Title = ""Child link 2"", Url = ""http://msn.com"", Key = ""Key1-2"" },
            new() { Name = ""Child link 3"", Title = ""Child link 3"", Url = ""http://msn.com"", Key = ""Key1-3"", IsEnabled = false },
        }
    },
    new()
    {
        Name = ""Parent link 2"",
        Title = ""Parent link 2"",
        Url = ""http://example.com"",
        Key = ""Key2"",
        CollapseAriaLabel = ""Collapse Parent link 2"",
        Links = new List<BitNavLinkItem>()
        {
            new() { Name = ""Child link 4"", Title = ""Child link 4"", Url = ""http://example.com"", Key = ""Key2-1"" }
        }
    }
};";

    private static string example5HTMLCode = @"<BitNav NavLinkItems=""CustomHeaderLinks""
        RenderType=""BitNavRenderType.Grouped""
        AriaLabel=""Nav with custom group header"">
    <HeaderTemplate Context=""link"">
        <h3> @link.Name </h3>
    </HeaderTemplate>
</BitNav>";

    private static string example5CSharpCode = @"
private readonly List<BitNavLinkItem> CustomHeaderLinks = new()
{
    new BitNavLinkItem
    {
        Name = ""Pages"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name = ""Activity"", Url = ""http://msn.com"", Key = ""Key1-1"", Title = ""Activity"" },
            new BitNavLinkItem { Name = ""News"", Title = ""News"", Url = ""http://msn.com"", Key = ""Key1-2"" },
        }
    },
    new BitNavLinkItem
    {
        Name = ""More pages"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name= ""Settings"", Title = ""Settings"", Url = ""http://example.com"", Key = ""Key2-1"" },
            new BitNavLinkItem { Name= ""Notes"", Title = ""Notes"", Url = ""http://example.com"", Key = ""Key2-1"" }
        }
    }
};";

    private static string example6HTMLCode = @"<BitNav Style=""width: 208px;
                height: 350px;
                box-sizing: border-box;
                border: 1px solid #eee;
                overflow-y: auto;
                z-index: 2;
                background-color: white;""
        NavLinkItems=""BasicNavLinks""
        AriaLabel=""Nav basic example""
        SelectedKey=""key3"">
</BitNav>";

    private static string example7HTMLCode = @"<BitNav Style=""width: 208px;
       height: 350px;
       box-sizing: border-box;
       border: 1px solid #eee;
       overflow-y: auto;""
        NavLinkItems=""BasicNavLinksWithoutURL""
        Mode=""BitNavMode.Manual""
        AriaLabel=""Nav manual mode example""
        SelectedKey=""@BitNavManualModeSelectedKey""
        SelectedKeyChanged=""HandleSelectedKeyChange"">
</BitNav>";

    private static string example7CSharpCode = @"
private readonly List<BitNavLinkItem> BasicNavLinksWithoutURL = new()
{
    new BitNavLinkItem
    {
        Name = ""Home"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded = true,
        Links = new List<BitNavLinkItem>
        {
            new BitNavLinkItem { Name = ""Activity"", Key = ""key1"", Target=""_blank"" },
            new BitNavLinkItem { Name = ""MSN"", Key = ""key2"", IsEnabled = false, Target = ""_blank"" }
        }
    },
    new BitNavLinkItem { Name = ""Documents"", Key = ""key3"", Target = ""_blank"", IsExpanded = true },
    new BitNavLinkItem { Name = ""Pages"", Key = ""key4"", Target = ""_parent"" },
    new BitNavLinkItem { Name = ""Notebook"", Key = ""key5"", IsEnabled = false },
    new BitNavLinkItem { Name = ""Communication and Media"", Key = ""key6"", Target = ""_top"" },
    new BitNavLinkItem { Name = ""News"", Title = ""News"", Key = ""key7"", IconName = BitIconName.News, Target = ""_self"" },
};

private string BitNavManualModeSelectedKey = ""key3"";

private void HandleSelectedKeyChange(string selectedKey)
{
    BitNavManualModeSelectedKey = selectedKey;
}";
}
