using System;
using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI;
using Bit.Platform.WebSite.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Bit.Platform.WebSite.Web.Components;

public partial class NavMenu
{
    private bool isNavOpen = false;
    private readonly List<BitNavLinkItem> allNavLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "TodoTemplate",
            Key = "TodoTemplate",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name= "Overview", Key = "Overview", Url = "/todo-template/overview"},
                new BitNavLinkItem { Name= "Development prerequisites", Key = "Development prerequisites", Url = "/todo-template/development-prerequisites"},
                new BitNavLinkItem { Name= "Getting started", Key = "Getting started", Url = "/todo-template/getting-started"},
                new BitNavLinkItem { Name= "Database", Key = "Database", Url = "/todo-template/database"},
                new BitNavLinkItem { Name= "Run", Key = "Run", Url = "/todo-template/run"},
                new BitNavLinkItem { Name= "Hosting models (Blazor mode)", Key = "Hosting models", Url = "/todo-template/hosting-models"},
                new BitNavLinkItem { Name= "Deployment type", Key = "Deployment type", Url = "/todo-template/deployment-type"},
                new BitNavLinkItem { Name= "Settings", Key = "Settings", Url = "/todo-template/settings"},
                new BitNavLinkItem { Name= "Project structure", Key = "Project structure", Url = "/todo-template/project-structure"},
                new BitNavLinkItem { Name= "Deployment type", Key = "Deployment type", Url = "/todo-template/deployment-type"},
                new BitNavLinkItem { Name= "Exception handling", Key = "Exception handling", Url = "/todo-template/exception-handling"},
                new BitNavLinkItem { Name= "Cache mechanism", Key = "Cache mechanism", Url = "/todo-template/cache-mechanism"},
                new BitNavLinkItem { Name= "Contribute", Key = "Contribute", Url = "/todo-template/contribute"},
            }
        },
        new BitNavLinkItem
        {
            Name = "AdminPanel",
            Key = "AdminPanel",
            Links = new List<BitNavLinkItem>
            {
            }
        }
    };
    private List<BitNavLinkItem> filteredNavLinks;
    private BitNavRenderType renderType = BitNavRenderType.Grouped;
    private string searchText = string.Empty;

    [Inject] public NavManuService NavManuService { get; set; }
    [Inject] public IJSRuntime JsRuntime { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    public string CurrentUrl { get; set; }

    protected override void OnInitialized()
    {
        HandleClear();
        NavManuService.OnToggleMenu += ToggleMenu;
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;
        base.OnInitialized();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private async void ToggleMenu()
    {
        isNavOpen = !isNavOpen;

        await JsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
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
