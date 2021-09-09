using System.Collections.Generic;

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
                Key = "key1",
                Title = "Home",
                Target = "_blank",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                ExpandAriaLabel = "Expand Home section",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem{
                        Name = "Activity",
                        Url = "http://msn.com",
                        Key = "key1-1",
                        Title = "Activity",
                        Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem { Name = "Activity", Title = "Activity", Url = "http://msn.com", Key = "key1-1-1" },
                            new BitNavLinkItem { Name = "MSN", Title= "MSN", Url = "http://msn.com", Key = "key1-1-2", Disabled=true }
                        }
                    },
                    new BitNavLinkItem { Name = "MSN", Title = "MSN", Url = "http://msn.com", Key = "key1-2", Disabled = true },
                }
            },
            new BitNavLinkItem { Name = "Documents", Title = "Documents", Url = "http://example.com", Key = "key2", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Title = "Pages", Url = "http://msn.com", Key = "key3", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Title = "Notebook", Url = "http://msn.com", Key = "key4", Disabled = true },
            new BitNavLinkItem { Name = "Communication and Media", Title = "Communication and Media", Url = "http://msn.com", Key = "key5", Target = "_top" },
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key6", Icon = "News", Target = "_self" },
        };

        private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "http://example.com",
                Key = "key1",
                Target = "_blank",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                Links = new List<BitNavLinkItem>
                {
                     new BitNavLinkItem{
                         Name = "Activity",
                         Url = "http://msn.com",
                         Key = "key1-1",
                         Links = new List<BitNavLinkItem>
                         {
                             new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1-1-1" },
                             new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key1-1-2", Disabled = true }
                         } },
                     new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key1-2", Disabled = true },
                }
            },
            new BitNavLinkItem { Name = "Shared Documents and Files", Url = "http://example.com", Key = "key2", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key3", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key4", Disabled = true },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key5", Target = "_top" },
            new BitNavLinkItem { Name = "News", Url = "http://msn.com", Key = "key6", Icon = "News", Target = "_self" },
        };

        private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Basic components",
                Key = "Key1",
                CollapseAriaLabel = "Collapse Basic components section",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                            new BitNavLinkItem { Name = "ActivityItem", Key = "ActivityItem", Url = "#/examples/activityitem" },
                            new BitNavLinkItem { Name = "Breadcrumb", Key = "Breadcrumb", Url = "#/examples/breadcrumb" },
                            new BitNavLinkItem { Name = "Button", Key = "Button", Url = "#/examples/button" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Extended components",
                Key = "Key2",
                CollapseAriaLabel = "Collapse Extended components section",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                     new BitNavLinkItem { Name = "ColorPicker", Key = "ColorPicker", Url = "#/examples/colorpicker" },
                     new BitNavLinkItem { Name = "ExtendedPeoplePicker", Key = "ExtendedPeoplePicker", Url = "#/examples/extendedpeoplepicker" },
                     new BitNavLinkItem { Name = "GroupedList", Key = "GroupedList", Url = "#/examples/groupedlist" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Utilities",
                Key = "Key3",
                CollapseAriaLabel = "Collapse Utilities section",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                     new BitNavLinkItem { Name= "FocusTrapZone", Key = "FocusTrapZone", Url = "#/examples/focustrapzone" },
                     new BitNavLinkItem { Name= "FocusZone", Key = "FocusZone", Url = "#/examples/focuszone" },
                     new BitNavLinkItem { Name= "MarqueeSelection", Key = "MarqueeSelection", Url = "#/examples/marqueeselection" }
                }
            },
        };

        private readonly List<BitNavLinkItem> NestedLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Parent link 1",
                Url = "http://example.com",
                Key = "Key1",
                Title = "Parent link 1",
                CollapseAriaLabel = "Collapse Parent link 1",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem
                    {
                        Name = "Child link 1",
                        Url = "http://msn.com",
                        Key = "Key1-1",
                        Title = "Child link 1",
                        Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem { Name = "3rd level link 1", Title = "3rd level link 1", Url= "http://msn.com", Key = "Key1-1-1" },
                            new BitNavLinkItem { Name = "3rd level link 2", Title = "3rd level link 2", Url= "http://msn.com", Key = "Key1-1-2", Disabled = true }
                        }
                    },
                    new BitNavLinkItem { Name = "Child link 2", Title = "Child link 2", Url = "http://msn.com", Key = "Key1-2" },
                    new BitNavLinkItem { Name = "Child link 3", Title = "Child link 3", Url = "http://msn.com", Key = "Key1-3", Disabled = true },
                }
            },
            new BitNavLinkItem
            {
                Name = "Parent link 2",
                Title = "Parent link 2",
                Url = "http://example.com",
                Key = "Key2",
                CollapseAriaLabel = "Collapse Parent link 2",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Child link 4", Title = "Child link 4", Url = "http://example.com", Key = "Key2-1" }
                }
            }
        };

        private readonly List<BitNavLinkItem> CustomHeaderLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Pages",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "Key1-1", Title = "Activity" },
                    new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "Key1-2" },
                }
            },
            new BitNavLinkItem
            {
                Name = "More pages",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name= "Settings", Title = "Settings", Url = "http://example.com", Key = "Key2-1" },
                    new BitNavLinkItem { Name= "Notes", Title = "Notes", Url = "http://example.com", Key = "Key2-1" }
                }
            }
        };
    }
}
