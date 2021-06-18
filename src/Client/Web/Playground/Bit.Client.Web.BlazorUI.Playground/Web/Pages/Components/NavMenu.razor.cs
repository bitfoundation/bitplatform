using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class NavMenu
    {
        private readonly List<NavLink> NavLinks = new()
        {
            new NavLink
            {
                Name = "Basic Inputs",
                Key = "Inputs",
                IsExpanded = true,
                Links = new List<NavLink>
                {
                     new NavLink { Name = "Button", Key = "Button", Url = "/components/buttons", TargetType = NavLinkTargetType.Self },
                     new NavLink { Name = "Checkbox", Key = "Checkbox", Url = "#" },
                     new NavLink { Name = "Choice", Key = "Choice", Url = "#" },
                     new NavLink { Name = "Label", Key = "Label", Url = "#" },
                     new NavLink { Name = "Link", Key = "Link", Url = "/components/links" , Target = "_Self" },
                     new NavLink { Name = "Rating", Key = "Rating", Url = "#" },
                     new NavLink { Name = "SearchBox", Key = "SearchBox", Url = "#" },
                     new NavLink { Name = "TextField", Key = "TextField", Url = "#" },
                     new NavLink { Name = "Toggle", Key = "Toggle", Url = "#" }
                }
            },
            new NavLink
            {
                Name = "Commands, Menus & Navs",
                Key = "Navigations",
                Links = new List<NavLink>
                {
                    new NavLink { Name = "Nav", Key = "Nav", Url = "#" },
                    new NavLink { Name = "Pivot", Key = "Pivot", Url = "#" },
                }
            },
            new NavLink
            {
                Name = "Notification & Engagement",
                Key = "Notifications",
                Links = new List<NavLink>
                {
                    new NavLink { Name = "MessageBar", Key = "MessageBar",Url="#" }
                }
            },
            new NavLink
            {
                Name = "Progress",
                Key = "Progress",
                Links = new List<NavLink>
                {
                    new NavLink{ Name = "Spinner", Key = "Spinner", Url = "#" }
                }
            },
        };
    }
}
