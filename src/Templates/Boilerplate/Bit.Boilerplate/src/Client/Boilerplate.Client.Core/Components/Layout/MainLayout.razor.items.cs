namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout
{
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] protected IAuthorizationService authorizationService = default!;

    private async Task SetNavPanelItems(ClaimsPrincipal authUser)
    {
        navPanelItems =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = PageUrls.HomePage,
            }
        ];

        //#if (module == "Admin")

        var (dashboard, manageProductCatalog) = await (authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.AdminPanel.Dashboard),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.AdminPanel.ManageProductCatalog));

        if (dashboard || manageProductCatalog)
        {
            BitNavItem adminPanelItem = new()
            {
                Text = localizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems = []
            };

            navPanelItems.Add(adminPanelItem);

            if (dashboard)
            {
                adminPanelItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Dashboard)],
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = PageUrls.DashboardPage,
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
                            Url = PageUrls.CategoriesPage,
                        },
                        new()
                        {
                            Text = localizer[nameof(AppStrings.Products)],
                            IconName = BitIconName.Product,
                            Url = PageUrls.ProductsPage,
                        }
                ]);
            }
        }
        //#endif

        //#if (sample == true)
        if (await authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Todo.ManageTodo))
        {
            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.Todo)],
                IconName = BitIconName.ToDoLogoOutline,
                Url = PageUrls.TodoPage,
            });
        }
        //#endif

        //#if (offlineDb == true)
        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.OfflineDatabaseDemoTitle)],
            IconName = BitIconName.EditContact,
            Url = PageUrls.OfflineDatabaseDemo,
        });
        //#endif

        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.Terms)],
            IconName = BitIconName.EntityExtraction,
            Url = PageUrls.TermsPage,
        });

        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.About)],
            IconName = BitIconName.Info,
            Url = PageUrls.AboutPage,
        });

        var (manageRoles, manageUsers, manageAiPrompt) = await (authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageRoles),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageUsers),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageAiPrompt));

        if (manageRoles || manageUsers || manageAiPrompt)
        {
            BitNavItem managementItem = new()
            {
                Text = localizer[nameof(AppStrings.Management)],
                IconName = BitIconName.SettingsSecure,
                ChildItems = []
            };

            navPanelItems.Add(managementItem);

            if (manageRoles)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.UserGroups)],
                    IconName = BitIconName.WorkforceManagement,
                    Url = PageUrls.RolesPage,
                });
            }

            if (manageUsers)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Users)],
                    IconName = BitIconName.SecurityGroup,
                    Url = PageUrls.UsersPage,
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
        }

        if (authUser.IsAuthenticated())
        {
            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.Settings)],
                IconName = BitIconName.Equalizer,
                Url = PageUrls.SettingsPage,
                AdditionalUrls =
                [
                    $"{PageUrls.SettingsPage}/{PageUrls.SettingsSections.Profile}",
                    $"{PageUrls.SettingsPage}/{PageUrls.SettingsSections.Account}",
                    $"{PageUrls.SettingsPage}/{PageUrls.SettingsSections.Tfa}",
                    $"{PageUrls.SettingsPage}/{PageUrls.SettingsSections.Sessions}",
                    $"{PageUrls.SettingsPage}/{PageUrls.SettingsSections.UpgradeAccount}",
                ]
            });
        }
    }
}
