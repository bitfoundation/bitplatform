using System;
using System.Collections.Generic;
using System.Linq;
using Bit.Client.Web.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components
{
    public partial class NavMenu
    {
        private bool isNavOpen = false;
        private readonly List<BitNavLinkItem> allNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Basic Inputs",
                Key = "Inputs",
                Links = new List<BitNavLinkItem>
                {
                     new BitNavLinkItem { Name= "Button", Key = "Button", Url = "/components/button"},
                     new BitNavLinkItem { Name= "ActionButton", Key = "ActionButton", Url = "/components/actionbutton"},
                     new BitNavLinkItem { Name= "CompoundButton", Key = "CompoundButton", Url = "/components/compoundbutton"},
                     new BitNavLinkItem { Name= "IconButton", Key = "IconButton", Url = "/components/iconbutton"},
                     new BitNavLinkItem { Name= "ToggleButton", Key = "ToggleButton", Url = "/components/togglebutton" },
                     new BitNavLinkItem { Name= "Checkbox", Key = "Checkbox", Url = "/components/checkboxes" },
                     new BitNavLinkItem { Name= "ChoiceGroup", Key = "Choice", Url = "/components/choicegroup" },
                     new BitNavLinkItem { Name= "DropDown", Key = "DropDown", Url = "/components/dropdown" },
                     new BitNavLinkItem { Name= "FileUpload", Key= "FileUpload", Url = "/components/fileuploads"},
                     new BitNavLinkItem { Name= "Label", Key = "Label",  Url = "/components/labels" },
                     new BitNavLinkItem { Name= "Link", Key = "Link", Url = "/components/links" },
                     new BitNavLinkItem { Name= "Rating", Key = "Rating", Url = "/components/rating" },
                     new BitNavLinkItem { Name= "SearchBox", Key = "SearchBox", Url = "/components/searchbox" },
                     new BitNavLinkItem { Name= "Slider", Key = "Slider", Url = "/components/slider" },
                     new BitNavLinkItem { Name= "SpinButton", Key = "SpinButton", Url = "/components/spinbuttons"},
                     new BitNavLinkItem { Name= "TextField", Key = "TextField", Url = "/components/textfield" },
                     new BitNavLinkItem { Name= "Toggle", Key = "Toggle", Url = "/components/toggle" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Galleries & Pickers",
                Key = "Pickers",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "DatePicker", Key = "DatePicker", Url="/components/datepicker" },
                    new BitNavLinkItem { Name= "ColorPicker", Key = "ColorPicker", Url = "/components/colorpickers" },
                    new BitNavLinkItem { Name= "Carousel", Key = "Carousel", Url = "/components/carousel" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Items & Lists",
                Key = "Lists",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Basic List", Key = "BasicList", Url="/components/basiclist" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Commands, Menus & Navs",
                Key = "Navigations",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Nav", Key = "Nav", Url = "/components/nav" },
                    new BitNavLinkItem { Name = "Pivot", Key = "Pivot", Url = "/components/pivot" },
                }
            },
            new BitNavLinkItem
            {
                Name = "Notification & Engagement",
                Key = "Notifications",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "MessageBar", Key = "MessageBar", Url="/components/messagebar" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Progress",
                Key = "Progress",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem{ Name = "Spinner", Key = "Spinner", Url = "/components/spinner" },
                    new BitNavLinkItem{ Name = "Progressindicator", Key = "Progressindicator", Url = "/components/progressindicator" }
                },
            },
            new BitNavLinkItem
            {
                Name = "Surfaces",
                Key = "Surfaces",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Modal", Key = "Modal", Url = "/components/modal" },
                },
            },
        };
        private List<BitNavLinkItem> filteredNavLinks;
        private BitNavRenderType renderType = BitNavRenderType.Grouped;
        private string searchText = string.Empty;

        [Inject] public NavManuService NavManuService { get; set; }
        [Inject] public IJSRuntime JsRuntime { get; set; }

        protected override void OnInitialized()
        {
            HandleClear();
            NavManuService.OnToggleMenu += ToggleMenu;
            base.OnInitialized();
        }

        private async void ToggleMenu()
        {
            isNavOpen = !isNavOpen;

            await JsRuntime.SetToggleBodyOverflow(isNavOpen);
            StateHasChanged();
        }

        private void HandleClear()
        {
            renderType = BitNavRenderType.Grouped;
            filteredNavLinks = allNavLinks;
        }

        private void HandleChange(string text)
        {
            HandleClear();
            searchText = text;
            if (string.IsNullOrEmpty(text)) return;

            renderType = BitNavRenderType.Normal;
            var flatNavLinkList = Flatten(allNavLinks).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
            filteredNavLinks = flatNavLinkList.FindAll(link => link.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase));
        }

        private void HandleLinkClick(BitNavLinkItem item)
        {
            searchText = string.Empty;

            HandleClear();
            ToggleMenu();
        }

        private static IEnumerable<BitNavLinkItem> Flatten(IEnumerable<BitNavLinkItem> e) => e.SelectMany(c => Flatten(c.Links)).Concat(e);
    }
}
