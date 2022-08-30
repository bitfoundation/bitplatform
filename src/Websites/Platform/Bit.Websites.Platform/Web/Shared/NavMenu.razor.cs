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
    private readonly List<BitNavLinkItem> allNavLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "Templates",
            Key = "Templates",
            Links = new List<BitNavLinkItem>
            {
                new BitNavLinkItem { Name= "Overview", Key = "Overview", Url = "/templates/overview"},
                new BitNavLinkItem { Name= "TodoTemplate", Key = "TodoTemplate", Url = "/todo-template/overview"},
                new BitNavLinkItem { Name= "AdminPanel", Key = "AdminPanel", Url = "/admin-panel/overview"},
                new BitNavLinkItem
                {
                    Name = "Getting Started",
                    Key = "Getting Started",
                    Links = new List<BitNavLinkItem>
                    {
                        new BitNavLinkItem { Name= "Development prerequisites", Key = "Development prerequisites", Url = "/templates/getting-started/development-prerequisites"},
                        new BitNavLinkItem { Name= "Getting Starteda", Key = "Todo Getting Started", Url = "/templates/getting-started/getting-started"},
                        new BitNavLinkItem { Name= "Database", Key = "Database", Url = "/templates/getting-started/database"},
                        new BitNavLinkItem { Name= "Run", Key = "Run", Url = "/templates/getting-started/run"},
                        new BitNavLinkItem { Name= "Hosting models", Key = "Hosting models", Url = "/templates/getting-started/hosting-models"},
                        new BitNavLinkItem { Name= "Deployment type", Key = "Deployment type", Url = "/templates/getting-started/deployment-type"},
                        new BitNavLinkItem { Name= "Settings", Key = "Settings", Url = "/templates/getting-started/settings"},
                        new BitNavLinkItem { Name= "Project structure", Key = "Project structure", Url = "/templates/getting-started/project-structure"},
                        new BitNavLinkItem { Name= "Exception handling", Key = "Exception handling", Url = "/templates/getting-started/exception-handling"},
                        new BitNavLinkItem { Name= "Cache mechanism", Key = "Cache mechanism", Url = "/templates/getting-started/cache-mechanism"},
                    }
                }
            }
        },
        new BitNavLinkItem
        {
            Name = "Cloud hosting solutions",
            Key = "Cloud hosting solutions"
        },
        new BitNavLinkItem
        {
            Name = "Support",
            Key = "Support"
        },
        new BitNavLinkItem
        {
            Name = "Contribute",
            Key = "Contribute",
            Url = "/contribute"
        }
    };
    private List<BitNavLinkItem> filteredNavLinks;
    private string searchText = string.Empty;

    [AutoInject] private NavManuService _navManuService = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;

    public string CurrentUrl { get; set; }

    protected override void OnInitialized()
    {
        HandleClear();
        _navManuService.OnToggleMenu += ToggleMenu;
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        _navigationManager.LocationChanged += OnLocationChanged;
        base.OnInitialized();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
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
