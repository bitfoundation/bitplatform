using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Bit.Websites.Platform.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Websites.Platform.Web.Shared;

public partial class NavMenu
{
    private bool isNavOpen = false;
    private readonly List<BitNavLinkItem> TodoTemplateNavLinks = new()
    {
        new BitNavLinkItem { Name= "Overview", Key = "Overview", Url = "/todo-template/overview"},
        new BitNavLinkItem { Name= "Development prerequisites", Key = "Development prerequisites", Url = "/todo-template/development-prerequisites"},
        new BitNavLinkItem { Name= "Getting Started", Key = "Getting Started", Url = "/todo-template/getting-started"},
        new BitNavLinkItem { Name= "Database", Key = "Database", Url = "/todo-template/database"},
        new BitNavLinkItem { Name= "Run", Key = "Run", Url = "/todo-template/run"},
        new BitNavLinkItem { Name= "Hosting models", Key = "Hosting models", Url = "/todo-template/hosting-models"},
        new BitNavLinkItem { Name= "Deployment type", Key = "Deployment type", Url = "/todo-template/deployment-type"},
        new BitNavLinkItem { Name= "Settings", Key = "Settings", Url = "/todo-template/settings"},
        new BitNavLinkItem { Name= "Project structure", Key = "Project structure", Url = "/todo-template/project-structure"},
        new BitNavLinkItem { Name= "Exception handling", Key = "Exception handling", Url = "/todo-template/exception-handling"},
        new BitNavLinkItem { Name= "Cache mechanism", Key = "Cache mechanism", Url = "/todo-template/cache-mechanism"},
        new BitNavLinkItem { Name= "Multilingualism", Key = "Multilingualism", Url = "/todo-template/multilingualism"},
        new BitNavLinkItem { Name= "DevOps", Key = "DevOps", Url = "/admin-panel/dev-ops"},
        new BitNavLinkItem { Name = "Contribute", Key = "Contribute", Url = "/todo-template/contribute"}
    };

    private readonly List<BitNavLinkItem> AdminPanelNavLinks = new()
    {
        new BitNavLinkItem { Name= "Overview", Key = "Overview", Url = "/admin-panel/overview"},
        new BitNavLinkItem { Name= "Development prerequisites", Key = "Development prerequisites", Url = "/admin-panel/development-prerequisites"},
        new BitNavLinkItem { Name= "Getting Started", Key = "Getting Started", Url = "/admin-panel/getting-started"},
        new BitNavLinkItem { Name= "Database", Key = "Database", Url = "/admin-panel/database"},
        new BitNavLinkItem { Name= "Run", Key = "Run", Url = "/admin-panel/run"},
        new BitNavLinkItem { Name= "Hosting models", Key = "Hosting models", Url = "/admin-panel/hosting-models"},
        new BitNavLinkItem { Name= "Deployment type", Key = "Deployment type", Url = "/admin-panel/deployment-type"},
        new BitNavLinkItem { Name= "Settings", Key = "Settings", Url = "/admin-panel/settings"},
        new BitNavLinkItem { Name= "Project structure", Key = "Project structure", Url = "/admin-panel/project-structure"},
        new BitNavLinkItem { Name= "Exception handling", Key = "Exception handling", Url = "/admin-panel/exception-handling"},
        new BitNavLinkItem { Name= "Cache mechanism", Key = "Cache mechanism", Url = "/admin-panel/cache-mechanism"},
        new BitNavLinkItem { Name= "Multilingualism", Key = "Multilingualism", Url = "/admin-panel/multilingualism"},
        new BitNavLinkItem { Name= "DevOps", Key = "DevOps", Url = "/admin-panel/dev-ops"},
        new BitNavLinkItem { Name = "Contribute", Key = "Contribute", Url = "/admin-panel/contribute"}
    };

    private List<BitNavLinkItem> filteredNavLinks;
    private string searchText = string.Empty;

    [AutoInject] private NavManuService _navManuService = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;

    public string CurrentUrl { get; set; }

    protected override void OnInitialized()
    {
        _navManuService.OnToggleMenu += ToggleMenu;
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        _navigationManager.LocationChanged += OnLocationChanged;
        HandleClear();
        base.OnInitialized();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        HandleClear();
        StateHasChanged();
    }

    private async void ToggleMenu()
    {
        try
        {
            isNavOpen = !isNavOpen;

            await _jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
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
        await _jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
        StateHasChanged();
    }

    private void HandleClear()
    {
        filteredNavLinks = CurrentUrl.Contains("admin-panel") ? AdminPanelNavLinks : TodoTemplateNavLinks;
    }

    private void HandleChange(string text)
    {
        HandleClear();
        searchText = text;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = CurrentUrl.Contains("admin-panel") ?
            Flatten(AdminPanelNavLinks).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url)) : Flatten(TodoTemplateNavLinks).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        filteredNavLinks = flatNavLinkList.FindAll(link => link.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleLinkClick(BitNavLinkItem item)
    {
        searchText = string.Empty;

        HandleClear();
        await HideMenu();
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

    private static IEnumerable<BitNavLinkItem> Flatten(IEnumerable<BitNavLinkItem> e) => e.SelectMany(c => Flatten(c.Links)).Concat(e);
}
