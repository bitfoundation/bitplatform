//+:cnd:noEmit
// [mirror] app chrome - nav panel items - keep in sync with:
// - src/Workbench/Components/Chrome/ChromeShell.razor.cs
namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout
{
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] protected IAuthorizationService authorizationService = default!;

    private async Task SetNavPanelItems(ClaimsPrincipal authUser)
    {
        List<BitNavItem> items =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = PageUrls.Home,
            }
        ];

        //#if (multitenant == true)
        var tenantIsSelected = await authorizationService.IsAuthorized(authUser!, AuthPolicies.TENANT_SELECTED);
        //#endif

        //#if (module == "Admin")
        var (dashboard, manageProductCatalog) = await (authorizationService.IsAuthorized(authUser!, AppFeatures.AdminPanel.Dashboard_View),
            authorizationService.IsAuthorized(authUser!, AppFeatures.AdminPanel.ProductCatalog_Manage));

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
                Text = localizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems = []
            };

            items.Add(adminPanelItem);

            if (dashboard)
            {
                adminPanelItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Dashboard)],
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = PageUrls.Dashboard,
                });
            }

            if (manageProductCatalog)
            {
                adminPanelItem.ChildItems.AddRange(
                [
                    new()
                        {
                            Text = localizer[nameof(AppStrings.Categories)],
                            IconName = BitIconName.BuildQueue,
                            Url = PageUrls.Categories,
                        },
                        new()
                        {
                            Text = localizer[nameof(AppStrings.Products)],
                            IconName = BitIconName.Product,
                            Url = PageUrls.Products,
                        }
                ]);
            }
        }
        //#endif

        //#if (sample == true || offlineDb == true)
        if (await authorizationService.IsAuthorized(authUser!, AppFeatures.Todo.Todo_Manage_Self))
        {
            //#if (offlineDb == true)
            items.Add(new()
            {
                Text = localizer[nameof(AppStrings.OfflineTodoTitle)],
                IconName = BitIconName.ToDoLogoOutline,
                Url = PageUrls.OfflineTodo,
            });
            //#elseif (sample == true)
            items.Add(new()
            {
                Text = localizer[nameof(AppStrings.Todo)],
                IconName = BitIconName.ToDoLogoInverse,
                Url = PageUrls.Todo,
            });
            //#endif
        }
        //#endif

        items.Add(new()
        {
            Text = localizer[nameof(AppStrings.Terms)],
            IconName = BitIconName.EntityExtraction,
            Url = PageUrls.Terms,
        });

        items.Add(new()
        {
            Text = localizer[nameof(AppStrings.About)],
            IconName = BitIconName.Info,
            Url = PageUrls.About,
        });

        var (manageRoles, manageUsers) = await (authorizationService.IsAuthorized(authUser!, AppFeatures.Management.Roles_Manage),
            authorizationService.IsAuthorized(authUser!, AppFeatures.Management.Users_Manage));

        var manageOAuthClients = await authorizationService.IsAuthorized(authUser!, AppFeatures.System.OAuthClients_Manage);

        var viewOperations = await authorizationService.IsAuthorized(authUser!, AppFeatures.System.Operations_View);

        //#if (signalR == true)
        var manageAiPrompt = await authorizationService.IsAuthorized(authUser!, AppFeatures.Management.SystemPrompts_Write);
        //#endif

        //#if (multitenant == true)
        if (tenantIsSelected is false)
        {
            manageRoles = manageUsers = false;
            //#if (signalR == true)
            manageAiPrompt = false;
            //#endif
        }

        var manageTenantsGlobally = await authorizationService.IsAuthorized(authUser!, AppFeatures.Management.Tenants_Manage_Global);
        //#endif

        // Every flag in this condition has to be able to contribute a child item below, or the group renders empty.
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
                Text = localizer[nameof(AppStrings.Management)],
                IconName = BitIconName.SettingsSecure,
                ChildItems = []
            };

            items.Add(managementItem);

            if (manageRoles)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.UserGroups)],
                    IconName = BitIconName.WorkforceManagement,
                    Url = PageUrls.Roles,
                });
            }

            if (manageUsers)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Users)],
                    IconName = BitIconName.SecurityGroup,
                    Url = PageUrls.Users,
                });
            }

            if (manageOAuthClients)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.OAuthClients)],
                    IconName = BitIconName.PlugConnected,
                    Url = PageUrls.OAuthClients,
                });
            }

            if (viewOperations)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Operations)],
                    IconName = BitIconName.ServerProcesses,
                    Url = PageUrls.Operations,
                });
            }

            //#if (signalR == true)
            if (manageAiPrompt)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.SystemPromptsTitle)],
                    IconName = BitIconName.TextDocumentSettings,
                    Url = PageUrls.SystemPrompts,
                });
            }
            //#endif

            //#if (multitenant == true)
            if (manageTenantsGlobally)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.ManageAllTenants)],
                    IconName = BitIconName.Org,
                    Url = PageUrls.ManageAllTenants,
                });
            }
            //#endif
        }

        if (authUser.IsAuthenticated())
        {
            items.Add(new()
            {
                Text = localizer[nameof(AppStrings.Settings)],
                IconName = BitIconName.Equalizer,
                Url = PageUrls.Settings,
                Match = BitNavMatch.Prefix
            });
        }

        AddCultureUrls(items);

        // Assigned once it is complete: a render can happen at any of the awaits above, and AppShell compares the list by
        // reference (See AppShell.GetParametersRenderKey), so a list filled in after it was assigned would stay half shown.
        navPanelItems = items;
    }

    /// <summary>
    /// The items link to the culture-less urls the client-side router navigates to (<c>/about</c>), while a full page
    /// load serves the same page under its culture (<c>/en-US/about</c>). BitNav marks the current page by comparing
    /// the url with each item's urls, so without the culture-prefixed forms nothing is marked after a refresh or a deep
    /// link.
    /// </summary>
    private static void AddCultureUrls(List<BitNavItem> items)
    {
        foreach (var item in items)
        {
            if (item.Url is not null)
            {
                string[] urls = [item.Url, .. item.AdditionalUrls ?? []];
                item.AdditionalUrls = [.. item.AdditionalUrls ?? [], .. urls.SelectMany(CultureInfoManager.GetCultureUrls)];
            }

            AddCultureUrls(item.ChildItems);
        }
    }
}
