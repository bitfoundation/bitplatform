using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Services;
using Bit.BlazorUI.Playground.Web.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Playground.Web.Components;

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
                new BitNavLinkItem { Name= "ActionButton", Key = "ActionButton", Url = "/components/action-button"},
                new BitNavLinkItem { Name= "CompoundButton", Key = "CompoundButton", Url = "/components/compound-button"},
                new BitNavLinkItem { Name= "IconButton", Key = "IconButton", Url = "/components/icon-button"},
                new BitNavLinkItem { Name= "ToggleButton", Key = "ToggleButton", Url = "/components/toggle-button" },
                new BitNavLinkItem { Name= "CheckBox", Key = "CheckBox", Url = "/components/check-box" },
                new BitNavLinkItem { Name= "ChoiceGroup", Key = "ChoiceGroup", Url = "/components/choice-group" },
                new BitNavLinkItem { Name= "RadioButtonGroup", Key = "RadioButtonGroup", Url = "/components/radio-button-group" },
                new BitNavLinkItem { Name = "RadioButtonList", Key = "RadioButtonList", Url="/components/radio-button-list" },
                new BitNavLinkItem { Name= "DropDown", Key = "DropDown", Url = "/components/drop-down" },
                new BitNavLinkItem { Name= "FileUpload", Key= "FileUpload", Url = "/components/file-upload"},
                new BitNavLinkItem { Name= "Label", Key = "Label",  Url = "/components/label" },
                new BitNavLinkItem { Name= "Link", Key = "Link", Url = "/components/link" },
                new BitNavLinkItem { Name= "Rating", Key = "Rating", Url = "/components/rating" },
                new BitNavLinkItem { Name= "SearchBox", Key = "SearchBox", Url = "/components/search-box" },
                new BitNavLinkItem { Name= "Slider", Key = "Slider", Url = "/components/slider" },
                new BitNavLinkItem { Name= "SpinButton", Key = "SpinButton", Url = "/components/spin-button"},
                new BitNavLinkItem { Name= "TextField", Key = "TextField", Url = "/components/text-field" },
                new BitNavLinkItem { Name= "NumericTextField", Key = "NumericTextField", Url = "/components/numeric-text-field" },
                new BitNavLinkItem { Name= "Toggle (Switch)", Key = "Toggle", Url = "/components/toggle" }
            }
        },
        new BitNavLinkItem
        {
            Name = "Galleries & Pickers",
            Key = "Pickers",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name= "ColorPicker", Key = "ColorPicker", Url = "/components/color-picker" },
                new BitNavLinkItem { Name = "DatePicker", Key = "DatePicker", Url="/components/date-picker" },
                new BitNavLinkItem { Name = "Chart", Key = "Chart", Url="/components/chart" }
            }
        },
        new BitNavLinkItem
        {
            Name = "Items & Lists",
            Key = "Lists",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "BasicList", Key = "BasicList", Url="/components/basic-list" },
                new BitNavLinkItem { Name = "DataGrid", Key = "DataGrid", Url="/components/data-grid" },
                new BitNavLinkItem { Name= "Carousel", Key = "Carousel", Url = "/components/carousel" },
                new BitNavLinkItem { Name= "Swiper", Key = "Swiper", Url = "/components/swiper" },
                new BitNavLinkItem { Name = "Persona", Key = "Persona", Url="/components/persona" }
            }
        },
        new BitNavLinkItem
        {
            Name = "Commands, Menus & Navs",
            Key = "Navigations",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "Breadcrumb", Key = "Breadcrumb", Url = "/components/breadcrumb" },
                new BitNavLinkItem { Name = "Nav (TreeList)", Key = "Nav", Url = "/components/nav" },
                new BitNavLinkItem { Name = "Pivot (Tab)", Key = "Pivot", Url = "/components/pivot" },
            }
        },
        new BitNavLinkItem
        {
            Name = "Notification & Engagement",
            Key = "Notifications",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name = "MessageBar", Key = "MessageBar", Url="/components/message-bar" }
            }
        },
        new BitNavLinkItem
        {
            Name = "Progress",
            Key = "Progress",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem{ Name = "Progressindicator", Key = "ProgressIndicator", Url = "/components/progress-indicator" },
                new BitNavLinkItem{ Name = "Spinner", Key = "Spinner", Url = "/components/spinner" }
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
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] private IExceptionHandler exceptionHandler { get; set; } = default!;

    public string CurrentUrl { get; set; }

    protected override async Task OnInitAsync()
    {
        HandleClear();
        NavManuService.OnToggleMenu += ToggleMenu;
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    async Task ToggleMenu()
    {
        try
        {
            isNavOpen = !isNavOpen;

            await JsRuntime.SetToggleBodyOverflow(isNavOpen);
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
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

    private async Task HandleLinkClick()
    {
        searchText = string.Empty;

        HandleClear();
        await ToggleMenu();
    }

    private string GetDemoLinkClassName(string link)
    {
        var className = "nav-menu-demo-link";
        if (CurrentUrl == "/components/overview" && link == "overview")
        {
            className += " nav-menu-demo-link--active";
        }
        else if (CurrentUrl == "/get-started" && link == "get-started")
        {
            className += " nav-menu-demo-link--active";
        }

        return className;
    }

    private static IEnumerable<BitNavLinkItem> Flatten(IEnumerable<BitNavLinkItem> e) => e.SelectMany(c => Flatten(c.Links)).Concat(e);
}
