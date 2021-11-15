using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Navs
{
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
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key7", Icon = "News", Target = "_self" },
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
            new BitNavLinkItem { Name = "News", Title = "News", Key = "key7", Icon = "News", Target = "_self" },
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
            new BitNavLinkItem { Name = "Shared Documents and Files", Url = "http://example.com", Key = "key3", Target = "_blank" },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
            new BitNavLinkItem { Name = "News", Key = "key7", Url = "http://cnn.com", Icon = "News", Target = "_self" }
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
                        Value="Automatic = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Manual",
                        Description="Selected key changes will be sent back to the parent component and the component won't change its value.",
                        Value="Manual= 1",
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
                       Name = "Icon",
                       Type = "string",
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

        private static string basicNavLinks = $"private readonly List<BitNavLinkItem> BasicNavLinks = new(){Environment.NewLine}" +
           $"{{ {Environment.NewLine}" +
           $"new BitNavLinkItem {Environment.NewLine}" +
           $"{{ {Environment.NewLine}" +
           $"Name = 'Home', {Environment.NewLine}" +
           $"Url = 'http://example.com', {Environment.NewLine}" +
           $"ExpandAriaLabel = 'Expand Home section', {Environment.NewLine}" +
           $"CollapseAriaLabel = 'Collapse Home section', {Environment.NewLine}" +
           $"IsExpanded = true, {Environment.NewLine}" +
           $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
           $"{{ {Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'Activity',Url = 'http://msn.com', Key = 'key1', Target='_blank' }}, {Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'MSN',Url = 'http://msn.com', Key = 'key2', IsEnabled = false, Target = '_blank' }}{Environment.NewLine}" +
           $"}}{Environment.NewLine}" +
           $"}},{Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'Documents',Url = 'http://example.com', Key = 'key3', Target = '_blank', IsExpanded = true }},{Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'Pages', Url = 'http://msn.com', Key = 'key4', Target = '_parent' }},{Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'Notebook', Url = 'http://msn.com', Key = 'key5', IsEnabled = false }},{Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'Communication and Media',Url = 'http://msn.com', Key = 'key6', Target = '_top' }},{Environment.NewLine}" +
           $"new BitNavLinkItem {{ Name = 'News',Title = 'News', Url = 'http://msn.com', Key = 'key7', Icon = 'News', Target = '_self' }},{Environment.NewLine}" +
           $"}};";

        private static string basicNavLinksWithoutURL = $"private readonly List<BitNavLinkItem> BasicNavLinksWithoutURL = new(){Environment.NewLine}" +
         $"{{ {Environment.NewLine}" +
         $"new BitNavLinkItem {Environment.NewLine}" +
         $"{{ {Environment.NewLine}" +
         $"Name = 'Home', {Environment.NewLine}" +
         $"ExpandAriaLabel = 'Expand Home section', {Environment.NewLine}" +
         $"CollapseAriaLabel = 'Collapse Home section', {Environment.NewLine}" +
         $"IsExpanded = true, {Environment.NewLine}" +
         $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
         $"{{ {Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'Activity', Key = 'key1', Target='_blank' }}, {Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'MSN', Key = 'key2', IsEnabled = false, Target = '_blank' }}{Environment.NewLine}" +
         $"}}{Environment.NewLine}" +
         $"}},{Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'Documents', Key = 'key3', Target = '_blank', IsExpanded = true }},{Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'Pages', Key = 'key4', Target = '_parent' }},{Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'Notebook', Key = 'key5', IsEnabled = false }},{Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'Communication and Media', Key = 'key6', Target = '_top' }},{Environment.NewLine}" +
         $"new BitNavLinkItem {{ Name = 'News',Title = 'News', Key = 'key7', Icon = 'News', Target = '_self' }},{Environment.NewLine}" +
         $"}}; {Environment.NewLine}";

        private static string basicNoToolTipNavLinks = $"private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Home', {Environment.NewLine}" +
          $"Title = '', {Environment.NewLine}" +
          $"Url = 'http://example.com', {Environment.NewLine}" +
          $"ExpandAriaLabel = 'Expand Home section', {Environment.NewLine}" +
          $"CollapseAriaLabel = 'Collapse Home section', {Environment.NewLine}" +
          $"IsExpanded = true, {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Activity',Url = 'http://msn.com', Key = 'key1', Target='_blank', Title = '' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'MSN',Url = 'http://msn.com', Key = 'key2', IsEnabled = false, Target = '_blank', Title = '' }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Shared Documents and Files',Url = 'http://example.com', Key = 'key3', Target = '_blank' }},{Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Pages', Url = 'http://msn.com', Key = 'key4', Target = '_parent' }},{Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Notebook', Url = 'http://msn.com', Key = 'key5', IsEnabled = false }},{Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Communication and Media',Url = 'http://msn.com', Key = 'key6', Target = '_top' }},{Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'News',Title = 'News', Url = 'http://msn.com', Key = 'key7', Icon = 'News', Target = '_self' }},{Environment.NewLine}" +
          $"}}; {Environment.NewLine}";

        private static string basicNoUrlNavLinks = $"private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Basic components', {Environment.NewLine}" +
          $"CollapseAriaLabel = 'Collapse Basic components section', {Environment.NewLine}" +
          $"IsExpanded = true, {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'ActivityItem',Url = '#/examples/activityitem', Key = 'ActivityItem' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Breadcrumb',Url = '#/examples/breadcrumb', Key = 'Breadcrumb' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Button',Url = '#/examples/button', Key = 'Button' }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Extended components', {Environment.NewLine}" +
          $"CollapseAriaLabel = 'Collapse Extended components section', {Environment.NewLine}" +
          $"IsExpanded = true, {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'ColorPicker',Url = '#/examples/colorpicker', Key = 'ColorPicker' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'ExtendedPeoplePicker',Url = '#/examples/extendedpeoplepicker', Key = 'ExtendedPeoplePicker' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'GroupedList',Url = '#/examples/groupedlist', Key = 'GroupedList' }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Utilities', {Environment.NewLine}" +
          $"CollapseAriaLabel = 'Collapse Utilities section', {Environment.NewLine}" +
          $"IsExpanded = true, {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'FocusTrapZone',Url = '#/examples/focustrapzone', Key = 'FocusTrapZone' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'FocusZone',Url = '#/examples/focuszone', Key = 'FocusZone' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'MarqueeSelection',Url = '#/examples/marqueeselection', Key = 'MarqueeSelection' }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"}}; {Environment.NewLine}";

        private static string nestedLinks = $"private readonly List<BitNavLinkItem> NestedLinks = new(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Parent link 1', {Environment.NewLine}" +
          $"Url = 'http://example.com', {Environment.NewLine}" +
          $"Key = 'Key1', {Environment.NewLine}" +
          $"Title = 'Parent link 1', {Environment.NewLine}" +
          $"CollapseAriaLabel = 'Collapse Parent link 1', {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Child link 1', {Environment.NewLine}" +
          $"Url = 'http://example.com', {Environment.NewLine}" +
          $"Key = 'Key1-1', {Environment.NewLine}" +
          $"Title = 'Child link 1', {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = '3rd level link 1',Url = 'http://msn.com', Key = 'Key1-1-1', Title = '3rd level link 1' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = '3rd level link 2',Url = 'http://msn.com', Key = 'Key1-1-2', Title = '3rd level link 2', IsEnabled = false }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = '3rd level link 2',Url = 'http://msn.com', Key = 'Key1-2', Title = 'Child link 2' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = '3rd level link 3',Url = 'http://msn.com', Key = 'Key1-3', Title = 'Child link 3', IsEnabled = false }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Parent link 2', {Environment.NewLine}" +
          $"Url = 'http://example.com', {Environment.NewLine}" +
          $"Key = 'Key2', {Environment.NewLine}" +
          $"Title = 'Parent link 2', {Environment.NewLine}" +
          $"CollapseAriaLabel = 'Collapse Parent link 2', {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Child link 4',Url = 'http://msn.com', Key = 'Key2-1', Title = 'Child link 4' }}, {Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}}; {Environment.NewLine}";

        private static string customHeaderLinks = $"private readonly List<BitNavLinkItem> CustomHeaderLinks = new(){Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Pages', {Environment.NewLine}" +
          $"IsExpanded = true, {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Activity',Url = 'http://msn.com', Key = 'key1-1', Target='_blank', Title = 'Activity' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'News',Title = 'News',Url = 'http://msn.com', Key = 'key1-2' }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"new BitNavLinkItem {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"Name = 'Pages', {Environment.NewLine}" +
          $"IsExpanded = true, {Environment.NewLine}" +
          $"Links = new List<BitNavLinkItem> {Environment.NewLine}" +
          $"{{ {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Settings',Url = 'http://example.com', Key = 'key2-1', Target='_blank', Title = 'Settings' }}, {Environment.NewLine}" +
          $"new BitNavLinkItem {{ Name = 'Notes',Title = 'News',Url = 'http://example.com', Key = 'key2-1' }}{Environment.NewLine}" +
          $"}}{Environment.NewLine}" +
          $"}},{Environment.NewLine}" +
          $"}}; {Environment.NewLine}";

        private readonly string navLinksSampleCode = $"<BitNav Style='width: 208px;height: 350px;box-sizing: border-box;border: 1px solid #eee;overflow-y: auto;',{Environment.NewLine}" +
          $"NavLinkItems = 'BasicNavLinks',{Environment.NewLine}" +
          $"IsOnTop='true',{Environment.NewLine}" +
          $"AriaLabel = 'Nav basic example',{Environment.NewLine}" +
          $"SelectedKey = 'key3'>,{Environment.NewLine}" +
          $"</BitNav>{Environment.NewLine}" + basicNavLinks;

        private readonly string navLinksWithoutTooltipSampleCode = $"<BitNav Style='width: 208px;height: 350px;box-sizing: border-box;border: 1px solid #eee;overflow-y: auto;',{Environment.NewLine}" +
         $"NavLinkItems = 'BasicNoToolTipNavLinks',{Environment.NewLine}" +
         $"AriaLabel = 'Nav example with wrapped link text',{Environment.NewLine}" +
         $"SelectedKey = 'key5'>,{Environment.NewLine}" +
         $"</BitNav>{Environment.NewLine}" + basicNoToolTipNavLinks;

        private readonly string navLinksWithNoUrlSampleCode = $"<BitNav Style='width: 300px;',{Environment.NewLine}" +
         $"NavLinkItems = 'BasicNoUrlNavLinks',{Environment.NewLine}" +
         $"AriaLabel = 'Nav example similar to one found in this demo page',{Environment.NewLine}" +
         $"RenderType='BitNavRenderType.Grouped'>,{Environment.NewLine}" +
         $"</BitNav>{Environment.NewLine}" + basicNoUrlNavLinks;

        private readonly string navWithNestedLinksSampleCode = $"<BitNav NavLinkItems = 'NestedLinks',{Environment.NewLine}" +
         $"AriaLabel = 'Nav example with nested links',{Environment.NewLine}" +
         $"SelectedKey='Key1-1-2'>,{Environment.NewLine}" +
         $"</BitNav>{Environment.NewLine}" + nestedLinks;

        private readonly string navWithCustomHeaderLinksSampleCode = $"<BitNav NavLinkItems = 'CustomHeaderLinks',{Environment.NewLine}" +
         $"AriaLabel = 'Nav with custom group header',{Environment.NewLine}" +
         $"RenderType='BitNavRenderType.Grouped'>,{Environment.NewLine}" +
         $"<HeaderTemplate Context='link'>,{Environment.NewLine}" +
         $"<h3> @link.Name </h3>,{Environment.NewLine}" +
         $"</HeaderTemplate='link'>,{Environment.NewLine}" +
         $"</BitNav>{Environment.NewLine}" + customHeaderLinks;

        private readonly string navTopOfAnotherSampleCode = $"<div style='position: relative;'>,{Environment.NewLine}" +
         $"<div style='width: 200px;height: 500px;background-color: rebeccapurple;'</div>,{Environment.NewLine}" +
         $"<BitNav Style='width: 208px;height: 350px;box-sizing: border-box;border: 1px solid #eee;overflow-y: auto;background-color: white;'{Environment.NewLine}" +
         $"NavLinkItems='BasicNavLinks',{Environment.NewLine}" +
         $"IsOnTop='true',{Environment.NewLine}" +
         $"AriaLabel='Nav basic example',{Environment.NewLine}" +
         $"SelectedKey='key3'>,{Environment.NewLine}" +
         $"</BitNav>{Environment.NewLine}" + basicNavLinks;

        private readonly string navWithManualModeLinksSampleCode = $"<BitNav Style = 'width: 208px;height: 350px;box-sizing: border-box;border: 1px solid #eee;overflow-y: auto;',{Environment.NewLine}" +
         $"AriaLabel = 'Nav manual mode example',{Environment.NewLine}" +
         $"NavLinkItems='BasicNavLinksWithoutURL'>,{Environment.NewLine}" +
         $"Mode='BitNavMode.Manual',{Environment.NewLine}" +
         $"SelectedKey='@BitNavManualModeSelectedKey',{Environment.NewLine}" +
         $"SelectedKeyChanged='HandleSelectedKeyChange'>,{Environment.NewLine}" +
         $"</BitNav>{Environment.NewLine}" + $"{basicNavLinksWithoutURL}{Environment.NewLine}" +
         $"private string BitNavManualModeSelectedKey = 'key3';{Environment.NewLine}" +
         $"private void HandleSelectedKeyChange(string selectedKey){Environment.NewLine}" +
         $"{{ {Environment.NewLine}" +
         $"BitNavManualModeSelectedKey = selectedKey;{Environment.NewLine}" +
         $"}} {Environment.NewLine}" +
         $"}},";
    }
}
