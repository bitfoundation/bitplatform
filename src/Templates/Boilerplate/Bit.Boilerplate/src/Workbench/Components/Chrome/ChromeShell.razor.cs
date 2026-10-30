//+:cnd:noEmit
namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeShell
{
    private bool isNavPanelOpen;
    private bool isNavPanelToggled;
    private string currentPageUrl = string.Empty;
    private List<BitNavItem> navPanelItems = [];

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    public WorkbenchAuthState Auth { get; private set; }

    public int ActiveUserIndex { get; private set; }

    public string TenantKey { get; private set; } = WorkbenchDemoUsers.TenantKeys[0];

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
    }

    public void SignIn()
    {
        Auth = WorkbenchAuthState.SignedIn;
        SetNavPanelItems();
        StateHasChanged();
    }

    public void SignOut()
    {
        Auth = WorkbenchAuthState.SignedOut;
        ActiveUserIndex = 0;
        SetNavPanelItems();
        StateHasChanged();
    }

    public void AddAccount()
    {
        Auth = WorkbenchAuthState.MultiAccount;
        StateHasChanged();
    }

    public void SwitchAccount(WorkbenchDemoUser user)
    {
        ActiveUserIndex = WorkbenchDemoUsers.All.ToList().IndexOf(user);
        StateHasChanged();
    }

    public void SwitchTenant(string tenantKey)
    {
        TenantKey = tenantKey;
        StateHasChanged();
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

        navPanelItems.AddRange(WorkbenchPageCatalog.Pages.Select(page => new BitNavItem
        {
            Text = Localizer[page.TitleKey],
            IconName = page.IconName,
            Url = page.Key == Page.Key ? currentPageUrl : WorkbenchUrls.Preview(page.Key, Scenario)
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
                    new() { Text = AppLocalizer[nameof(AppStrings.Dashboard)], IconName = BitIconName.BarChartVerticalFill, Url = "#dashboard" },
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

        navPanelItems.Add(new() { Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Equalizer, Url = "#settings" });
    }
}
