using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Bit.BlazorUI.Playground.Web.Services;
using Bit.BlazorUI.Playground.Web.Services.Contracts;

namespace Bit.BlazorUI.Playground.Web.Shared;

public partial class NavMenu
{
    private bool isNavOpen = false;
    private readonly List<BitNavItem> allNavLinks = new()
    {
        new BitNavItem { Text = "Overview", Url = "/overview" },
        new BitNavItem { Text = "Getting started", Url = "/getting-started" },
        new BitNavItem
        {
            Text = "Basic Inputs",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Button", Url = "/components/button" },
                new BitNavItem { Text = "ActionButton", Url = "/components/action-button" },
                new BitNavItem { Text = "CompoundButton", Url = "/components/compound-button" },
                new BitNavItem { Text = "IconButton", Url = "/components/icon-button" },
                new BitNavItem { Text = "LoadingButton", Url = "/components/loading-button" },
                new BitNavItem { Text = "ToggleButton", Url = "/components/toggle-button" },
                new BitNavItem { Text = "MenuButton", Url = "/components/menu-button" },
                new BitNavItem { Text = "MenuButtonGroup", Url = "/components/menu-button-group" },
                new BitNavItem { Text = "MenuButtonList", Url = "/components/menu-button-list" },
                new BitNavItem { Text = "SplitButton", Url = "/components/split-button" },
                new BitNavItem { Text = "SplitButtonList", Url = "/components/split-button-list" },
                new BitNavItem { Text = "CheckBox", Url = "/components/check-box" },
                new BitNavItem { Text = "ChoiceGroup", Url = "/components/choice-group" },
                new BitNavItem { Text = "RadioButtonGroup", Url = "/components/radio-button-group" },
                new BitNavItem { Text = "RadioButtonList", Url = "/components/radio-button-list" },
                new BitNavItem { Text = "DropDown", Url = "/components/drop-down" },
                new BitNavItem { Text = "FileUpload", Url = "/components/file-upload" },
                new BitNavItem { Text = "Label",  Url = "/components/label" },
                new BitNavItem { Text = "Link", Url = "/components/link" },
                new BitNavItem { Text = "Rating", Url = "/components/rating" },
                new BitNavItem { Text = "SearchBox", Url = "/components/search-box" },
                new BitNavItem { Text = "Slider", Url = "/components/slider" },
                new BitNavItem { Text = "SpinButton", Url = "/components/spin-button" },
                new BitNavItem { Text = "TextField", Url = "/components/text-field" },
                new BitNavItem { Text = "NumericTextField", Url = "/components/numeric-text-field" },
                new BitNavItem { Text = "OtpInput", Url = "/components/otp-input" },
                new BitNavItem { Text = "Toggle (Switch)", Url = "/components/toggle" }
            }
        },
        new BitNavItem
        {
            Text = "Galleries & Pickers",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "ColorPicker", Url = "/components/color-picker" },
                new BitNavItem { Text = "DatePicker", Url = "/components/date-picker" },
                new BitNavItem { Text = "DateRangePicker", Url = "/components/date-range-picker" },
                new BitNavItem { Text = "Chart", Url = "/components/chart" }
            }
        },
        new BitNavItem
        {
            Text = "Items & Lists",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "BasicList", Url = "/components/basic-list" },
                new BitNavItem { Text = "DataGrid", Url = "/components/data-grid" },
                new BitNavItem { Text = "Carousel", Url = "/components/carousel" },
                new BitNavItem { Text = "Swiper", Url = "/components/swiper" },
                new BitNavItem { Text = "Persona (AvatarView)", Url = "/components/persona" }
            }
        },
        new BitNavItem
        {
            Text = "Commands, Menus & Navs",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Breadcrumb", Url = "/components/breadcrumb" },
                new BitNavItem { Text = "BreadGroup", Url = "/components/bread-group" },
                new BitNavItem { Text = "BreadList", Url = "/components/bread-list" },
                new BitNavItem { Text = "Nav (TreeList)", Url = "/components/nav" },
                new BitNavItem { Text = "NavGroup (TreeList)", Url = "/components/nav-group" },
                new BitNavItem { Text = "NavList (TreeList)", Url = "/components/nav-list" },
                new BitNavItem { Text = "Pivot (Tab)", Url = "/components/pivot" },
            }
        },
        new BitNavItem
        {
            Text = "Notification & Engagement",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "MessageBar", Url = "/components/message-bar" }
            }
        },
        new BitNavItem
        {
            Text = "Progress",
            Items = new List<BitNavItem>
            {
                new BitNavItem{ Text = "ProgressIndicator", Url = "/components/progress-indicator" },
                new BitNavItem{ Text = "Spinner (BusyIndicator)", Url = "/components/spinner" },
                new BitNavItem{ Text = "Loading", Url = "/components/loading" }
            },
        },
        new BitNavItem
        {
            Text = "Surfaces",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Accordion (Expander)", Url = "/components/accordion" },
                new BitNavItem { Text = "Modal", Url = "/components/modal" },
            },
        },
        new BitNavItem
        {
            Text = "Utilities",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Icon", Url = "/components/icon" },
                new BitNavItem { Text = "Overlay", Url = "/components/overlay" },
            },
        },
        new BitNavItem { Text = "Icons", Url = "/icons" },
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
        filteredNavLinks = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (item.Url.HasNoValue()) return;

        searchText = string.Empty;

        HandleClear();
        await ToggleMenu();
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e);
}
