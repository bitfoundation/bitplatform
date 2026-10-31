//+:cnd:noEmit
// [mirror] app chrome - nav panel items - keep in sync with:
// - src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.items.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeShell
{
    private string currentPageUrl = string.Empty;
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] private ChromeStore chromeStore = default!;
    [AutoInject] private AuthenticationStateProvider authenticationStateProvider = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    public WorkbenchAuthState Auth => Scenario.Auth;

    public string TenantKey => chromeStore.TenantKey;

    public bool IsSignedIn => Auth is not WorkbenchAuthState.SignedOut;

    public WorkbenchDemoUser ActiveUser => WorkbenchDemoUsers.Find(Scenario.User);

    public IEnumerable<WorkbenchDemoUser> OtherAccounts => Auth is WorkbenchAuthState.MultiAccount
        ? WorkbenchDemoUsers.All.Where(u => u != ActiveUser)
        : [];

    protected override async Task OnInitializedAsync()
    {
        currentPageUrl = new Uri(NavigationManager.Uri).PathAndQuery;
        Subscribe(chromeStore);
        ScenarioState.Changed += OnScenarioChanged;

        await SetNavPanelItems();
    }

    public Task SignIn() => ReportScenario(Scenario with { Auth = WorkbenchAuthState.SignedIn });

    public Task SignOut() => ReportScenario(Scenario with { Auth = WorkbenchAuthState.SignedOut, User = 0 });

    public Task AddAccount() => ReportScenario(Scenario with { Auth = WorkbenchAuthState.MultiAccount });

    public Task SwitchAccount(WorkbenchDemoUser user) => ReportScenario(Scenario with { User = WorkbenchDemoUsers.All.ToList().IndexOf(user) });

    public void SwitchTenant(string tenantKey) => chromeStore.SwitchTenant(tenantKey);

    private void OnScenarioChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await SetNavPanelItems();
            StateHasChanged();
        });
    }

    private string PageUrl(string pageKey, string fallback)
    {
        if (WorkbenchPageCatalog.Find(pageKey) is null)
            return fallback;

        return pageKey == Page.Key ? currentPageUrl : WorkbenchUrls.Preview(pageKey, Scenario, PaneId);
    }

    private async Task SetNavPanelItems()
    {
        var authUser = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        navPanelItems =
        [
            new()
            {
                Text = AppLocalizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = "#home"
            }
        ];

        navPanelItems.Add(new()
        {
            Text = Localizer[nameof(WorkbenchStrings.NavShowcases)],
            IconName = BitIconName.Tiles,
            IsExpanded = true,
            ChildItems = [.. WorkbenchPageCatalog.Pages.Where(page => page.CategoryKey != nameof(WorkbenchStrings.CategoryAppPages)).Select(page => new BitNavItem
            {
                Text = Localizer[page.TitleKey],
                IconName = page.IconName,
                Url = PageUrl(page.Key, page.Key)
            })]
        });

        //#if (multitenant == true)
        var tenantIsSelected = await AuthorizationService.IsAuthorized(authUser, AuthPolicies.TENANT_SELECTED);
        //#endif

        //#if (module == "Admin")
        var dashboard = await AuthorizationService.IsAuthorized(authUser, AppFeatures.AdminPanel.Dashboard_View);
        var manageProductCatalog = await AuthorizationService.IsAuthorized(authUser, AppFeatures.AdminPanel.ProductCatalog_Manage);

        //#if (multitenant == true)
        if (tenantIsSelected is false)
        {
            dashboard = manageProductCatalog = false;
        }
        //#endif

        if (dashboard || manageProductCatalog)
        {
            BitNavItem adminPanelItem = new()
            {
                Text = AppLocalizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems = []
            };

            navPanelItems.Add(adminPanelItem);

            if (dashboard)
            {
                adminPanelItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Dashboard)], IconName = BitIconName.BarChartVerticalFill, Url = PageUrl("dashboard", "#dashboard") });
            }

            if (manageProductCatalog)
            {
                adminPanelItem.ChildItems.AddRange(
                [
                    new() { Text = AppLocalizer[nameof(AppStrings.Categories)], IconName = BitIconName.BuildQueue, Url = "#categories" },
                    new() { Text = AppLocalizer[nameof(AppStrings.Products)], IconName = BitIconName.Product, Url = "#products" }
                ]);
            }
        }
        //#endif

        //#if (sample == true || offlineDb == true)
        if (await AuthorizationService.IsAuthorized(authUser, AppFeatures.Todo.Todo_Manage_Self))
        {
            //#if (offlineDb == true)
            navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.OfflineTodoTitle)], IconName = BitIconName.ToDoLogoOutline, Url = "#offline-todo" });
            //#elseif (sample == true)
            navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Todo)], IconName = BitIconName.ToDoLogoInverse, Url = "#todo" });
            //#endif
        }
        //#endif

        navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Terms)], IconName = BitIconName.EntityExtraction, Url = "#terms" });
        navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.About)], IconName = BitIconName.Info, Url = "#about" });

        var manageRoles = await AuthorizationService.IsAuthorized(authUser, AppFeatures.Management.Roles_Manage);
        var manageUsers = await AuthorizationService.IsAuthorized(authUser, AppFeatures.Management.Users_Manage);
        var manageOAuthClients = await AuthorizationService.IsAuthorized(authUser, AppFeatures.System.OAuthClients_Manage);
        var viewOperations = await AuthorizationService.IsAuthorized(authUser, AppFeatures.System.Operations_View);

        //#if (signalR == true)
        var manageAiPrompt = await AuthorizationService.IsAuthorized(authUser, AppFeatures.Management.SystemPrompts_Write);
        //#endif

        //#if (multitenant == true)
        if (tenantIsSelected is false)
        {
            manageRoles = manageUsers = false;
            //#if (signalR == true)
            manageAiPrompt = false;
            //#endif
        }

        var manageTenantsGlobally = await AuthorizationService.IsAuthorized(authUser, AppFeatures.Management.Tenants_Manage_Global);
        //#endif

        if (manageRoles || manageUsers || manageOAuthClients || viewOperations
            //#if (signalR == true)
            || manageAiPrompt
            //#endif
            //#if (multitenant == true)
            || manageTenantsGlobally
            //#endif
            )
        {
            BitNavItem managementItem = new()
            {
                Text = AppLocalizer[nameof(AppStrings.Management)],
                IconName = BitIconName.SettingsSecure,
                ChildItems = []
            };

            navPanelItems.Add(managementItem);

            if (manageRoles)
            {
                managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.UserGroups)], IconName = BitIconName.WorkforceManagement, Url = "#roles" });
            }

            if (manageUsers)
            {
                managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Users)], IconName = BitIconName.SecurityGroup, Url = "#users" });
            }

            if (manageOAuthClients)
            {
                managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.OAuthClients)], IconName = BitIconName.PlugConnected, Url = "#oauth-clients" });
            }

            if (viewOperations)
            {
                managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Operations)], IconName = BitIconName.ServerProcesses, Url = "#operations" });
            }

            //#if (signalR == true)
            if (manageAiPrompt)
            {
                managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.SystemPromptsTitle)], IconName = BitIconName.TextDocumentSettings, Url = "#system-prompts" });
            }
            //#endif

            //#if (multitenant == true)
            if (manageTenantsGlobally)
            {
                managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.ManageAllTenants)], IconName = BitIconName.Org, Url = "#tenants" });
            }
            //#endif
        }

        if (authUser.IsAuthenticated())
        {
            navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Equalizer, Url = PageUrl("settings", "#settings") });
        }
    }

    public override async ValueTask DisposeAsync()
    {
        ScenarioState.Changed -= OnScenarioChanged;

        await base.DisposeAsync();
    }
}
