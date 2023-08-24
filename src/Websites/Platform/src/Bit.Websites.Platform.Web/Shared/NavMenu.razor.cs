using Bit.Websites.Platform.Web.Services;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Web.Shared;

public partial class NavMenu
{
    private bool _isNavOpen = false;
    private string _searchText = string.Empty;
    private List<BitNavItem> _filteredNavLinks = default!;

    private readonly List<BitNavItem> _templatesNavItems = new()
    {
        new BitNavItem { Text = "Overview", Url = "/templates/overview"},
        new BitNavItem { Text = "Development prerequisites", Url = "/templates/development-prerequisites"},
        new BitNavItem { Text = "Create project", Url = "/templates/create-project"},
        new BitNavItem { Text = "Project structure", Url = "/templates/project-structure"},
        new BitNavItem { Text = "Database", Url = "/templates/database"},
        new BitNavItem { Text = "Run", Url = "/templates/run"},
        new BitNavItem { Text = "Hosting models", Url = "/templates/hosting-models"},
        new BitNavItem { Text = "Deployment type", Url = "/templates/deployment-type"},
        new BitNavItem { Text = "Settings", Url = "/templates/settings"},
        new BitNavItem { Text = "Exception handling", Url = "/templates/exception-handling"},
        new BitNavItem { Text = "Cache mechanism", Url = "/templates/cache-mechanism"},
        new BitNavItem { Text = "Multilingualism", Url = "/templates/multilingualism"},
        new BitNavItem { Text = "DevOps", Url = "/templates/devops"},
        new BitNavItem { Text = "Contribute", Url = "/templates/contribute"}
    };

    [AutoInject] private NavManuService _navManuService = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;

    public string CurrentUrl { get; set; } = default!;

    protected override void OnInitialized()
    {
        _navManuService.OnToggleMenu += ToggleMenu;
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        _navigationManager.LocationChanged += OnLocationChanged;
        HandleOnClear();
        base.OnInitialized();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        HandleOnClear();
        StateHasChanged();
    }

    private async void ToggleMenu()
    {
        try
        {
            _isNavOpen = !_isNavOpen;

            await _jsRuntime.InvokeVoidAsync("toggleBodyOverflow", _isNavOpen);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

    private void HandleOnClear()
    {
        _filteredNavLinks = _templatesNavItems;
    }

    private void HandleValueChanged(string text)
    {
        HandleOnClear();
        _searchText = text;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = Flatten(_templatesNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        _filteredNavLinks = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        _searchText = string.Empty;

        HandleOnClear();

        _isNavOpen = false;

        await _jsRuntime.InvokeVoidAsync("toggleBodyOverflow", _isNavOpen);

        StateHasChanged();
    }

    private string GetNavMenuClass()
    {
        if (string.IsNullOrEmpty(_searchText))
        {
            return "side-nav";
        }
        else
        {
            return "side-nav searched-side-nav";
        }
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e);
}
