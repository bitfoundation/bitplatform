using Bit.Websites.Platform.Web.Services;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Web;

public partial class NavMenu
{
    private bool _isNavOpen = false;
    private string _searchText = string.Empty;
    private List<BitNavItem> _filteredNavLinks = default!;

    private readonly List<BitNavItem> _adminPanelNavItems = new()
    {
        new BitNavItem { Text = "Overview", Url = "/admin-panel/overview"},
        new BitNavItem { Text = "Development prerequisites", Url = "/admin-panel/development-prerequisites"},
        new BitNavItem { Text = "Create project", Url = "/admin-panel/create-project"},
        new BitNavItem { Text = "Database", Url = "/admin-panel/database"},
        new BitNavItem { Text = "Run", Url = "/admin-panel/run"},
        new BitNavItem { Text = "Hosting models", Url = "/admin-panel/hosting-models"},
        new BitNavItem { Text = "Deployment type", Url = "/admin-panel/deployment-type"},
        new BitNavItem { Text = "Settings", Url = "/admin-panel/settings"},
        new BitNavItem { Text = "Project structure", Url = "/admin-panel/project-structure"},
        new BitNavItem { Text = "Exception handling", Url = "/admin-panel/exception-handling"},
        new BitNavItem { Text = "Cache mechanism", Url = "/admin-panel/cache-mechanism"},
        new BitNavItem { Text = "Multilingualism", Url = "/admin-panel/multilingualism"},
        new BitNavItem { Text = "DevOps", Url = "/admin-panel/devops"},
        new BitNavItem { Text = "Contribute", Url = "/admin-panel/contribute"}
    };

    private readonly List<BitNavItem> _todoTemplateNavItems = new()
    {
        new BitNavItem { Text = "Overview", Url = "/todo-template/overview"},
        new BitNavItem { Text = "Development prerequisites", Url = "/todo-template/development-prerequisites"},
        new BitNavItem { Text = "Create project", Url = "/todo-template/create-project"},
        new BitNavItem { Text = "Database", Url = "/todo-template/database"},
        new BitNavItem { Text = "Run", Url = "/todo-template/run"},
        new BitNavItem { Text = "Hosting models", Url = "/todo-template/hosting-models"},
        new BitNavItem { Text = "Deployment type", Url = "/todo-template/deployment-type"},
        new BitNavItem { Text = "Settings", Url = "/todo-template/settings"},
        new BitNavItem { Text = "Project structure", Url = "/todo-template/project-structure"},
        new BitNavItem { Text = "Exception handling", Url = "/todo-template/exception-handling"},
        new BitNavItem { Text = "Cache mechanism", Url = "/todo-template/cache-mechanism"},
        new BitNavItem { Text = "Multilingualism", Url = "/todo-template/multilingualism"},
        new BitNavItem { Text = "DevOps", Url = "/todo-template/devops"},
        new BitNavItem { Text = "Contribute", Url = "/todo-template/contribute"}
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
        _filteredNavLinks = CurrentUrl.Contains("admin-panel") ? _adminPanelNavItems : _todoTemplateNavItems;
    }

    private void HandleValueChanged(string text)
    {
        HandleOnClear();
        _searchText = text;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = CurrentUrl.Contains("admin-panel") ?
            Flatten(_adminPanelNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url)) : Flatten(_todoTemplateNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
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
