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
    private readonly List<BitNavItem> TodoTemplateNavLinks = new()
    {
        new BitNavItem { Text = "Overview", Url = "/todo-template/overview"},
        new BitNavItem { Text = "Development prerequisites", Url = "/todo-template/development-prerequisites"},
        new BitNavItem { Text = "Getting Started", Url = "/todo-template/getting-started"},
        new BitNavItem { Text = "Database", Url = "/todo-template/database"},
        new BitNavItem { Text = "Run", Url = "/todo-template/run"},
        new BitNavItem { Text = "Hosting models", Url = "/todo-template/hosting-models"},
        new BitNavItem { Text = "Deployment type", Url = "/todo-template/deployment-type"},
        new BitNavItem { Text = "Settings", Url = "/todo-template/settings"},
        new BitNavItem { Text = "Project structure", Url = "/todo-template/project-structure"},
        new BitNavItem { Text = "Exception handling", Url = "/todo-template/exception-handling"},
        new BitNavItem { Text = "Cache mechanism", Url = "/todo-template/cache-mechanism"},
        new BitNavItem { Text = "Multilingualism", Url = "/todo-template/multilingualism"},
        new BitNavItem { Text = "DevOps", Url = "/todo-template/dev-ops"},
        new BitNavItem { Text = "Contribute", Url = "/todo-template/contribute"}
    };

    private readonly List<BitNavItem> AdminPanelNavLinks = new()
    {
        new BitNavItem { Text = "Overview", Url = "/admin-panel/overview"},
        new BitNavItem { Text = "Development prerequisites", Url = "/admin-panel/development-prerequisites"},
        new BitNavItem { Text = "Getting Started", Url = "/admin-panel/getting-started"},
        new BitNavItem { Text = "Database", Url = "/admin-panel/database"},
        new BitNavItem { Text = "Run", Url = "/admin-panel/run"},
        new BitNavItem { Text = "Hosting models", Url = "/admin-panel/hosting-models"},
        new BitNavItem { Text = "Deployment type", Url = "/admin-panel/deployment-type"},
        new BitNavItem { Text = "Settings", Url = "/admin-panel/settings"},
        new BitNavItem { Text = "Project structure", Url = "/admin-panel/project-structure"},
        new BitNavItem { Text = "Exception handling", Url = "/admin-panel/exception-handling"},
        new BitNavItem { Text = "Cache mechanism", Url = "/admin-panel/cache-mechanism"},
        new BitNavItem { Text = "Multilingualism", Url = "/admin-panel/multilingualism"},
        new BitNavItem { Text = "DevOps", Url = "/admin-panel/dev-ops"},
        new BitNavItem { Text = "Contribute", Url = "/admin-panel/contribute"}
    };

    private List<BitNavItem> filteredNavLinks;
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
        filteredNavLinks = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleLinkClick(BitNavItem item)
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

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e);
}
