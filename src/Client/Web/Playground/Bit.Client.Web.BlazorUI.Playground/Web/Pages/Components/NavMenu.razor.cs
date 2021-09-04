using System;
using System.Collections.Generic;
using System.Linq;
using Bit.Client.Web.BlazorUI.Extensions;

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
                     new() { Name= "Button", Key = "Button", Url = "/components/button"},
                     new() { Name= "ActionButton", Key = "ActionButton", Url = "/components/actionbutton"},
                     new() { Name= "CompoundButton", Key = "CompoundButton", Url = "/components/compoundbutton"},
                     new() { Name= "IconButton", Key = "IconButton", Url = "/components/iconbutton"},
                     new() { Name= "ToggleButton", Key = "ToggleButton", Url = "/components/togglebutton" },
                     new() { Name= "Checkbox", Key = "Checkbox", Url = "/components/checkboxes" },
                     new() { Name= "ChoiceGroup", Key = "Choice", Url = "/components/choicegroup" },
                     new() { Name= "DropDown", Key = "DropDown", Url = "/components/dropdown" },
                     new() { Name= "FileUpload", Key= "FileUpload", Url = "/components/fileuploads"},
                     new() { Name= "Label", Key = "Label",  Url = "/components/labels" },
                     new() { Name= "Link", Key = "Link", Url = "/components/links" },
                     new() { Name= "Rating", Key = "Rating", Url = "/components/rating" },
                     new() { Name= "SearchBox", Key = "SearchBox", Url = "/components/searchbox" },
                     new() { Name= "SpinButton", Key = "SpinButton", Url = "/components/spinbuttons"},
                     new() { Name= "TextField", Key = "TextField", Url = "/components/textfield" },
                     new() { Name= "Toggle", Key = "Toggle", Url = "/components/toggle" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Galleries & Pickers",
                Key = "Pickers",
                Links = new List<BitNavLinkItem>
                {
                    new() { Name = "DatePicker", Key = "DatePicker", Url="/components/datepicker" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Items & Lists",
                Key = "Lists",
                Links = new List<BitNavLinkItem>
                {
                    new() { Name = "Basic List", Key = "BasicList", Url="/components/basiclist" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Commands, Menus & Navs",
                Key = "Navigations",
                Links = new List<BitNavLinkItem>
                {
                    new() { Name = "Nav", Key = "Nav", Url = "/components/nav" },
                    new() { Name = "Pivot", Key = "Pivot", Url = "/components/pivot" },
                }
            },
            new BitNavLinkItem
            {
                Name = "Notification & Engagement",
                Key = "Notifications",
                Links = new List<BitNavLinkItem>
                {
                    new() { Name = "MessageBar", Key = "MessageBar", Url="/components/messagebar" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Progress",
                Key = "Progress",
                Links = new List<BitNavLinkItem>
                {
                    new() { Name = "Spinner", Key = "Spinner", Url = "/components/spinner" },
                    new() { Name = "Progressindicator", Key = "Progressindicator", Url = "/components/progressindicator" }
                },
            },
        };
        private List<BitNavLinkItem> NavLinksToShow;

        public NavMenu()
        {
            HandleClear();
        }

        private void HandleClear()
        {
            NavLinksToShow = NavLinks.DeepCopy();
        }

        private void HandleChange(string text)
        {
            HandleClear();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            NavLinksToShow = FilterNavLinks(text, NavLinksToShow).ToList();
        }

        private IEnumerable<BitNavLinkItem> FilterNavLinks(string text, IEnumerable<BitNavLinkItem> navLinkItems)
        {
            foreach (var navLinkItem in navLinkItems)
            {
                if (navLinkItem.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                {
                    navLinkItem.IsExpanded = true;
                    yield return navLinkItem;
                    continue;
                }

                if (navLinkItem.Links == null)
                {
                    continue;
                }

                navLinkItem.Links = FilterNavLinks(text, navLinkItem.Links);

                if (!navLinkItem.Links.Any())
                {
                    continue;
                }

                navLinkItem.IsExpanded = true;
                yield return navLinkItem;
            }
        }
    }
}
