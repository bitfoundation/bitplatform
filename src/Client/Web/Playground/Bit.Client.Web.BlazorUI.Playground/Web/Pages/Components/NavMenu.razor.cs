using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class NavMenu
    {
        private readonly List<BitNavLinkItem> NavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Basic Inputs",
                Key = "Inputs",
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                     new BitNavLinkItem { Name= "Button", Key = "Button", Url = "/components/buttons", Target = "_Self" },
                     new BitNavLinkItem { Name= "Checkbox", Key = "Checkbox", Url = "/components/checkboxes" },
                     new BitNavLinkItem { Name= "Choice", Key = "Choice", Url = "#" },
                     new BitNavLinkItem { Name= "Label", Key = "Label",  Url = "/components/labels" },
                     new BitNavLinkItem { Name= "Link", Key = "Link", Url = "#" },
                     new BitNavLinkItem { Name= "Rating", Key = "Rating", Url = "#" },
                     new BitNavLinkItem { Name= "SearchBox", Key = "SearchBox", Url = "#" },
                     new BitNavLinkItem { Name= "TextField", Key = "TextField", Url = "#" },
                     new BitNavLinkItem { Name= "Toggle", Key = "Toggle", Url = "#" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Commands, Menus & Navs",
                Key = "Navigations",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Nav", Key = "Nav", Url = "#" },
                    new BitNavLinkItem { Name = "Pivot", Key = "Pivot", Url = "#" },
                }
            },
            new BitNavLinkItem
            {
                Name = "Notification & Engagement",
                Key = "Notifications",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "MessageBar", Key = "MessageBar",Url="#" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Progress",
                Key = "Progress",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem{ Name = "Spinner", Key = "Spinner", Url = "#" }
                }
            },
        };
    }
}
