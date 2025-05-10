namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout
{
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] protected IAuthorizationService authorizationService = default!;

    private async Task SetNavPanelItems()
    {
        navPanelItems =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = Urls.HomePage,
            }
        ];

        if (user?.IsAuthenticated() is true)
        {
            //#if (module == "Admin")

            var (dashboard, manageProductCatalog) = await (authorizationService.IsAuthorizedAsync(user, AppFeatures.AdminPanel.Dashboard),
                authorizationService.IsAuthorizedAsync(user, AppFeatures.AdminPanel.ManageProductCatalog));

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
                        Url = Urls.DashboardPage,
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
                            Url = Urls.CategoriesPage,
                        },
                        new()
                        {
                            Text = localizer[nameof(AppStrings.Products)],
                            IconName = BitIconName.Product,
                            Url = Urls.ProductsPage,
                        }
                    ]);
                }
            }
            //#endif

            //#if (sample == true)
            if (await authorizationService.IsAuthorizedAsync(user, AppFeatures.Todo.ManageTodo))
            {
                navPanelItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Todo)],
                    IconName = BitIconName.ToDoLogoOutline,
                    Url = Urls.TodoPage,
                });
            }
            //#endif

            //#if (offlineDb == true)
            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.OfflineEditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = Urls.OfflineEditProfilePage,
            });
            //#endif
        }

        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.Terms)],
            IconName = BitIconName.EntityExtraction,
            Url = Urls.TermsPage,
        });

        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.About)],
            IconName = BitIconName.Info,
            Url = Urls.AboutPage,
        });

        if (user?.IsAuthenticated() is true)
        {
            var (manageRoles, manageUsers, manageAiPrompt) = await (authorizationService.IsAuthorizedAsync(user, AppFeatures.Management.ManageRoles),
                authorizationService.IsAuthorizedAsync(user, AppFeatures.Management.ManageUsers),
                authorizationService.IsAuthorizedAsync(user, AppFeatures.Management.ManageAiPrompt));

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
                        IconName = BitIconName.SecurityGroup,
                        Url = Urls.RolesPage,
                    });
                }

                if (manageUsers)
                {
                    managementItem.ChildItems.Add(new()
                    {
                        Text = localizer[nameof(AppStrings.Users)],
                        IconName = BitIconName.SecurityGroup,
                        Url = Urls.UsersPage,
                    });
                }

                //#if (signalR == true)
                if (manageAiPrompt)
                {
                    managementItem.ChildItems.Add(new()
                    {
                        Text = localizer[nameof(AppStrings.SystemPromptsTitle)],
                        IconName = BitIconName.TextDocumentSettings,
                        Url = Urls.SystemPrompts,
                    });
                }
                //#endif
            }

            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.Settings)],
                IconName = BitIconName.Equalizer,
                Url = Urls.SettingsPage,
                AdditionalUrls =
                [
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Profile}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Account}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Tfa}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Sessions}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.UpgradeAccount}",
                ]
            });
        }
    }
}
