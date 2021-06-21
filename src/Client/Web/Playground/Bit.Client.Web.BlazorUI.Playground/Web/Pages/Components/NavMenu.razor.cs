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
                     new NavLink { Name= "Button", Key = "Button", Url = "/components/buttons", Target = "_Self" },
                     new NavLink { Name= "Checkbox", Key = "Checkbox", Url = "/components/checkboxes" },
                     new NavLink { Name= "ChoiceGroup", Key = "Choice", Url = "/components/choicegroup" },
                     new NavLink { Name= "Label", Key = "Label", Url = "/components/labels" },
                     new NavLink { Name = "Link", Key = "Link", Url = "/components/links" },
                     new NavLink { Name= "Rating", Key = "Rating", Url = "#" },
                     new NavLink { Name= "SearchBox", Key = "SearchBox", Url = "#" },
                     new NavLink { Name= "TextField", Key = "TextField", Url = "#" },
                     new NavLink { Name= "Toggle", Key = "Toggle", Url = "#" }
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
                    new NavLink{ Name = "Spinner", Key = "Spinner", Url = "/components/spinner" }
                }
            },
        };
    }
}
