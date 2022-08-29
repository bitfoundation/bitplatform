using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Bit.Websites.Platform.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Bit.Websites.Platform.Web.Shared;

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
                new BitNavLinkItem { Name= "Overview", Key = "Todo Overview", Url = "/todo-template/overview"},
                new BitNavLinkItem { Name= "Development prerequisites", Key = "Todo Development prerequisites", Url = "/todo-template/development-prerequisites"},
                new BitNavLinkItem { Name= "Getting Started", Key = "Todo Getting Started", Url = "/todo-template/getting-started"},
                new BitNavLinkItem { Name= "Database", Key = "Todo Database", Url = "/todo-template/database"},
                new BitNavLinkItem { Name= "Run", Key = "Todo Run", Url = "/todo-template/run"},
                new BitNavLinkItem { Name= "Hosting models", Key = "Todo Hosting models", Url = "/todo-template/hosting-models"},
                new BitNavLinkItem { Name= "Deployment type", Key = "Todo Deployment type", Url = "/todo-template/deployment-type"},
                new BitNavLinkItem { Name= "Settings", Key = "Todo Settings", Url = "/todo-template/settings"},
                new BitNavLinkItem { Name= "Project structure", Key = "Todo Project structure", Url = "/todo-template/project-structure"},
                new BitNavLinkItem { Name= "Exception handling", Key = "Todo Exception handling", Url = "/todo-template/exception-handling"},
                new BitNavLinkItem { Name= "Cache mechanism", Key = "Todo Cache mechanism", Url = "/todo-template/cache-mechanism"},
                new BitNavLinkItem { Name= "Contribute", Key = "Todo Contribute", Url = "/todo-template/contribute"},
            }
        },
        new BitNavLinkItem
        {
            Name = "AdminPanel",
            Key = "AdminPanel",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name= "Overview", Key = "Admin Overview", Url = "/admin-panel/overview"},
                new BitNavLinkItem { Name= "Development prerequisites", Key = "Admin Development prerequisites", Url = "/admin-panel/development-prerequisites"},
                new BitNavLinkItem { Name= "Getting Started", Key = "Admin Getting Started", Url = "/admin-panel/getting-started"},
                new BitNavLinkItem { Name= "Database", Key = "Admin Database", Url = "/admin-panel/database"},
                new BitNavLinkItem { Name= "Run", Key = "Admin Run", Url = "/admin-panel/run"},
                new BitNavLinkItem { Name= "Hosting models", Key = "Admin Hosting models", Url = "/admin-panel/hosting-models"},
                new BitNavLinkItem { Name= "Deployment type", Key = "Admin Deployment type", Url = "/admin-panel/deployment-type"},
                new BitNavLinkItem { Name= "Settings", Key = "Admin Settings", Url = "/admin-panel/settings"},
                new BitNavLinkItem { Name= "Project structure", Key = "Admin Project structure", Url = "/admin-panel/project-structure"},
                new BitNavLinkItem { Name= "Exception handling", Key = "Admin Exception handling", Url = "/admin-panel/exception-handling"},
                new BitNavLinkItem { Name= "Cache mechanism", Key = "Admin Cache mechanism", Url = "/admin-panel/cache-mechanism"},
                new BitNavLinkItem { Name= "Contribute", Key = "Admin Contribute", Url = "/admin-panel/contribute"},
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
        try
        {
            isNavOpen = !isNavOpen;

            await JsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

    private async Task HideMenu()
    {
        isNavOpen = false;
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

    private async Task HandleLinkClick(BitNavLinkItem item)
    {
        searchText = string.Empty;

        HandleClear();
        await HideMenu();
    }

    private static IEnumerable<BitNavLinkItem> Flatten(IEnumerable<BitNavLinkItem> e) => e.SelectMany(c => Flatten(c.Links)).Concat(e);
}
