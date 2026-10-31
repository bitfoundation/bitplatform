//+:cnd:noEmit
// [mirror] app chrome - nav panel items - keep in sync with:
// - src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.items.cs
namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeShell
{
    private string currentPageUrl = string.Empty;
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] private ChromeStore chromeStore = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    public WorkbenchAuthState Auth { get; private set; }

    public int ActiveUserIndex => Auth is WorkbenchAuthState.MultiAccount ? Math.Clamp(chromeStore.ActiveUserIndex, 0, WorkbenchDemoUsers.All.Count - 1) : 0;

    public string TenantKey => chromeStore.TenantKey;

    public bool IsSignedIn => Auth is not WorkbenchAuthState.SignedOut;

    public WorkbenchDemoUser ActiveUser => WorkbenchDemoUsers.All[ActiveUserIndex];

    public IEnumerable<WorkbenchDemoUser> OtherAccounts => Auth is WorkbenchAuthState.MultiAccount
        ? WorkbenchDemoUsers.All.Where(u => u != ActiveUser)
        : [];

    protected override void OnInitialized()
    {
        Auth = Scenario.Auth;
        currentPageUrl = new Uri(NavigationManager.Uri).PathAndQuery;
        SetNavPanelItems();
        Subscribe(chromeStore);
    }

    public Task SignIn() => ChangeAuth(WorkbenchAuthState.SignedIn);

    public Task SignOut()
    {
        chromeStore.SwitchAccount(0);

        return ChangeAuth(WorkbenchAuthState.SignedOut);
    }

    public Task AddAccount() => ChangeAuth(WorkbenchAuthState.MultiAccount);

    public void SwitchAccount(WorkbenchDemoUser user) => chromeStore.SwitchAccount(WorkbenchDemoUsers.All.ToList().IndexOf(user));

    public void SwitchTenant(string tenantKey) => chromeStore.SwitchTenant(tenantKey);

    private async Task ChangeAuth(WorkbenchAuthState auth)
    {
        Auth = auth;
        SetNavPanelItems();
        StateHasChanged();

        await ReportScenario(Scenario with { Auth = auth });
    }

    private string PageUrl(string pageKey, string fallback)
    {
        if (WorkbenchPageCatalog.Find(pageKey) is null)
            return fallback;

        return pageKey == Page.Key ? currentPageUrl : WorkbenchUrls.Preview(pageKey, Scenario, PaneId);
    }

    private void SetNavPanelItems()
    {
        navPanelItems =
        [
            new()
            {
                Text = AppLocalizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = "#home"
            }
        ];

        navPanelItems.AddRange(WorkbenchPageCatalog.Pages.Where(page => page.CategoryKey != nameof(WorkbenchStrings.CategoryAppPages)).Select(page => new BitNavItem
        {
            Text = Localizer[page.TitleKey],
            IconName = page.IconName,
            Url = PageUrl(page.Key, page.Key)
        }));

        //#if (module == "Admin")
        if (IsSignedIn)
        {
            navPanelItems.Add(new()
            {
                Text = AppLocalizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems =
                [
                    new() { Text = AppLocalizer[nameof(AppStrings.Dashboard)], IconName = BitIconName.BarChartVerticalFill, Url = PageUrl("dashboard", "#dashboard") },
                    new() { Text = AppLocalizer[nameof(AppStrings.Categories)], IconName = BitIconName.BuildQueue, Url = "#categories" },
                    new() { Text = AppLocalizer[nameof(AppStrings.Products)], IconName = BitIconName.Product, Url = "#products" }
                ]
            });
        }
        //#endif

        //#if (offlineDb == true)
        if (IsSignedIn)
        {
            navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.OfflineTodoTitle)], IconName = BitIconName.ToDoLogoOutline, Url = "#offline-todo" });
        }
        //#elseif (sample == true)
        if (IsSignedIn)
        {
            navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Todo)], IconName = BitIconName.ToDoLogoInverse, Url = "#todo" });
        }
        //#endif

        navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Terms)], IconName = BitIconName.EntityExtraction, Url = "#terms" });
        navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.About)], IconName = BitIconName.Info, Url = "#about" });

        if (IsSignedIn is false)
            return;

        BitNavItem managementItem = new()
        {
            Text = AppLocalizer[nameof(AppStrings.Management)],
            IconName = BitIconName.SettingsSecure,
            ChildItems =
            [
                new() { Text = AppLocalizer[nameof(AppStrings.UserGroups)], IconName = BitIconName.WorkforceManagement, Url = "#roles" },
                new() { Text = AppLocalizer[nameof(AppStrings.Users)], IconName = BitIconName.SecurityGroup, Url = "#users" },
                new() { Text = AppLocalizer[nameof(AppStrings.OAuthClients)], IconName = BitIconName.PlugConnected, Url = "#oauth-clients" },
                new() { Text = AppLocalizer[nameof(AppStrings.Operations)], IconName = BitIconName.ServerProcesses, Url = "#operations" }
            ]
        };

        //#if (signalR == true)
        managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.SystemPromptsTitle)], IconName = BitIconName.TextDocumentSettings, Url = "#system-prompts" });
        //#endif

        //#if (multitenant == true)
        managementItem.ChildItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.ManageAllTenants)], IconName = BitIconName.Org, Url = "#tenants" });
        //#endif

        navPanelItems.Add(managementItem);

        navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Equalizer, Url = PageUrl("settings", "#settings") });
    }
}
