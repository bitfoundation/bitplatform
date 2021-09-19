using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Navs
{
    public partial class BitNavDemo
    {
        private readonly List<BitNavLinkItem> BasicNavLinks = new()
        {
            new()
            {
                Name = "Home",
                Url = "http://example.com",
                Key = "key1",
                Title = "Home",
                Target = "_blank",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                ExpandAriaLabel = "Expand Home section",
                Links = new List<BitNavLinkItem>()
                {
                    new()
                    {
                        Name = "Activity",
                        Url = "http://msn.com",
                        Key = "key1-1",
                        Title = "Activity",
                        Links = new List<BitNavLinkItem>()
                        {
                            new() { Name = "Activity", Title = "Activity", Url = "http://msn.com", Key = "key1-1-1" },
                            new() { Name = "MSN", Title = "MSN", Url = "http://msn.com", Key = "key1-1-2", IsEnabled = false }
                        }
                    },
                    new() { Name = "MSN", Title = "MSN", Url = "http://msn.com", Key = "key1-2", IsEnabled = false },
                }
            },
            new() { Name = "Documents", Title = "Documents", Url = "http://example.com", Key = "key2", Target = "_blank", IsExpanded = true },
            new() { Name = "Pages", Title = "Pages", Url = "http://msn.com", Key = "key3", Target = "_parent" },
            new() { Name = "Notebook", Title = "Notebook", Url = "http://msn.com", Key = "key4", IsEnabled = false },
            new() { Name = "Communication and Media", Title = "Communication and Media", Url = "http://msn.com", Key = "key5", Target = "_top" },
            new()
            {
                Name = "News with children",
                Title = "News with children",
                Url = "http://msn.com",
                Key = "key6",
                Icon = "News",
                Target = "_self",
                Links = new List<BitNavLinkItem>()
                {
                    new()
                    {
                        Name = "News Activity",
                        Url = "http://msn.com",
                        Key = "key6-1",
                        Title = "News Activity",
                        Links = new List<BitNavLinkItem>()
                        {
                            new() { Name = "Activity of News", Title = "Activity of News", Url = "http://msn.com", Key = "key6-1-1" },
                            new() { Name = "MSN of News", Title = "MSN of News", Url = "http://msn.com", Key = "key6-1-2", IsEnabled = false }
                        }
                    },
                    new() { Name = "News MSN", Title = "News MSN", Url = "http://msn.com", Key = "key6-2", IsEnabled = false },
                }
            },
            new()
            {
                Name = "News",
                Title = "News",
                Url = "http://msn.com",
                Key = "key6",
                Icon = "News",
                Target = "_self",
            },
        };

        private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new()
        {
            new()
            {
                Name = "Home",
                Url = "http://example.com",
                Key = "key1",
                Target = "_blank",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                Links = new List<BitNavLinkItem>
                {
                    new (){
                        Name = "Activity",
                        Url = "http://msn.com",
                        Key = "key1-1",
                        Links = new List<BitNavLinkItem>()
                        {
                            new () { Name = "Activity", Url = "http://msn.com", Key = "key1-1-1" },
                            new () { Name = "MSN", Url = "http://msn.com", Key = "key1-1-2", IsEnabled = false }
                        }
                    },
                    new () { Name = "MSN", Url = "http://msn.com", Key = "key1-2", IsEnabled = false },
                }
            },
            new() { Name = "Shared Documents and Files", Url = "http://example.com", Key = "key2", Target = "_blank", IsExpanded = true },
            new() { Name = "Pages", Url = "http://msn.com", Key = "key3", Target = "_parent" },
            new() { Name = "Notebook", Url = "http://msn.com", Key = "key4", IsEnabled = false },
            new() { Name = "Communication and Media", Url = "http://msn.com", Key = "key5", Target = "_top" },
            new() { Name = "News", Url = "http://msn.com", Key = "key6", Icon = "News", Target = "_self" },
        };

        private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new()
        {
            new()
            {
                Name = "Basic components",
                Key = "Key1",
                CollapseAriaLabel = "Collapse Basic components section",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>()
                {
                    new() { Name = "ActivityItem", Key = "ActivityItem", Url = "#/examples/activityitem" },
                    new() { Name = "Breadcrumb", Key = "Breadcrumb", Url = "#/examples/breadcrumb" },
                    new() { Name = "Button", Key = "Button", Url = "#/examples/button" }
                }
            },
            new()
            {
                Name = "Extended components",
                Key = "Key2",
                CollapseAriaLabel = "Collapse Extended components section",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>()
                {
                    new() { Name = "ColorPicker", Key = "ColorPicker", Url = "#/examples/colorpicker" },
                    new() { Name = "ExtendedPeoplePicker", Key = "ExtendedPeoplePicker", Url = "#/examples/extendedpeoplepicker" },
                    new() { Name = "GroupedList", Key = "GroupedList", Url = "#/examples/groupedlist" }
                }
            },
            new()
            {
                Name = "Utilities",
                Key = "Key3",
                CollapseAriaLabel = "Collapse Utilities section",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>()
                {
                    new() { Name = "FocusTrapZone", Key = "FocusTrapZone", Url = "#/examples/focustrapzone" },
                    new() { Name = "FocusZone", Key = "FocusZone", Url = "#/examples/focuszone" },
                    new() { Name = "MarqueeSelection", Key = "MarqueeSelection", Url = "#/examples/marqueeselection" }
                }
            },
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
            new()
            {
                Name = "Pages",
                Links = new List<BitNavLinkItem>()
                {
                    new() { Name = "Activity", Url = "http://msn.com", Key = "Key1-1", Title = "Activity" },
                    new() { Name = "News", Title = "News", Url = "http://msn.com", Key = "Key1-2" },
                }
            },
            new()
            {
                Name = "More pages",
                Links = new List<BitNavLinkItem>()
                {
                    new() { Name = "Settings", Title = "Settings", Url = "http://example.com", Key = "Key2-1" },
                    new() { Name = "Notes", Title = "Notes", Url = "http://example.com", Key = "Key2-1" }
                }
            }
        };
    }
}
