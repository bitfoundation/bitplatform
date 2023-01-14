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
    private readonly List<BitNavItem> allNavLinks = new()
    {
        new BitNavItem { Name= "Overview", Url = "/overview"},
        new BitNavItem { Name= "Getting started", Url = "/getting-started"},
        new BitNavItem
        {
            Name = "Basic Inputs",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name= "Button", Url = "/components/button"},
                new BitNavItem { Name= "ActionButton", Url = "/components/action-button"},
                new BitNavItem { Name= "CompoundButton", Url = "/components/compound-button"},
                new BitNavItem { Name= "IconButton", Url = "/components/icon-button"},
                new BitNavItem { Name= "LoadingButton", Url = "/components/loading-button"},
                new BitNavItem { Name= "ToggleButton", Url = "/components/toggle-button" },
                new BitNavItem { Name= "MenuButton", Url = "/components/menu-button" },
                new BitNavItem { Name= "SplitButton", Url = "/components/split-button" },
                new BitNavItem { Name= "CheckBox", Url = "/components/check-box" },
                new BitNavItem { Name= "ChoiceGroup", Url = "/components/choice-group" },
                new BitNavItem { Name= "RadioButtonGroup", Url = "/components/radio-button-group" },
                new BitNavItem { Name = "RadioButtonList", Url="/components/radio-button-list" },
                new BitNavItem { Name= "DropDown", Url = "/components/drop-down" },
                new BitNavItem { Name= "FileUpload", Url = "/components/file-upload"},
                new BitNavItem { Name= "Label",  Url = "/components/label" },
                new BitNavItem { Name= "Link", Url = "/components/link" },
                new BitNavItem { Name= "Rating", Url = "/components/rating" },
                new BitNavItem { Name= "SearchBox", Url = "/components/search-box" },
                new BitNavItem { Name= "Slider", Url = "/components/slider" },
                new BitNavItem { Name= "SpinButton", Url = "/components/spin-button"},
                new BitNavItem { Name= "TextField", Url = "/components/text-field" },
                new BitNavItem { Name= "NumericTextField", Url = "/components/numeric-text-field" },
                new BitNavItem { Name= "OtpInput", Url = "/components/otp-input" },
                new BitNavItem { Name= "Toggle (Switch)", Url = "/components/toggle" }
            }
        },
        new BitNavItem
        {
            Name = "Galleries & Pickers",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name= "ColorPicker", Url = "/components/color-picker" },
                new BitNavItem { Name = "DatePicker", Url="/components/date-picker" },
                new BitNavItem { Name = "DateRangePicker", Url="/components/date-range-picker" },
                new BitNavItem { Name = "Chart", Url="/components/chart" }
            }
        },
        new BitNavItem
        {
            Name = "Items & Lists",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name = "BasicList", Url="/components/basic-list" },
                new BitNavItem { Name = "DataGrid", Url="/components/data-grid" },
                new BitNavItem { Name= "Carousel", Url = "/components/carousel" },
                new BitNavItem { Name= "Swiper", Url = "/components/swiper" },
                new BitNavItem { Name = "Persona (AvatarView)", Url="/components/persona" }
            }
        },
        new BitNavItem
        {
            Name = "Commands, Menus & Navs",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name = "Breadcrumb", Url = "/components/breadcrumb" },
                new BitNavItem { Name = "BreadGroup", Url = "/components/bread-group" },
                new BitNavItem { Name = "BreadList", Url = "/components/bread-list" },
                new BitNavItem { Name = "Nav (TreeList)", Url = "/components/nav" },
                new BitNavItem { Name = "Pivot (Tab)", Url = "/components/pivot" },
            }
        },
        new BitNavItem
        {
            Name = "Notification & Engagement",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name = "MessageBar", Url="/components/message-bar" }
            }
        },
        new BitNavItem
        {
            Name = "Progress",
            Items = new List<BitNavItem>
            {
                new BitNavItem{ Name = "Progressindicator", Url = "/components/progress-indicator" },
                new BitNavItem{ Name = "Spinner (BusyIndicator)", Url = "/components/spinner" },
                new BitNavItem{ Name = "Loading", Url = "/components/loading" }
            },
        },
        new BitNavItem
        {
            Name = "Surfaces",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name = "Accordion (Expander)", Url="/components/accordion" },
                new BitNavItem { Name = "Modal", Url = "/components/modal" },
            },
        },
        new BitNavItem
        {
            Name = "Utilities",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Name = "Icon", Url = "/components/icon" },
            },
        },
        new BitNavItem { Name= "Icons", Url = "/icons"},
    };

    private List<BitNavItem> filteredNavLinks;
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
        filteredNavLinks = allNavLinks;
    }

    private void HandleChange(string text)
    {
        HandleClear();
        searchText = text;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = Flatten(allNavLinks).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        filteredNavLinks = flatNavLinkList.FindAll(link => link.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleLinkClick(BitNavItem item)
    {
        if (item.Url.HasNoValue()) return;

        searchText = string.Empty;

        HandleClear();
        await ToggleMenu();
    }

    private string GetNavMenuClass()
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return "side-nav";
        }
        else
        {
            return "side-nav searched-side-nav";
        }
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e);
}
