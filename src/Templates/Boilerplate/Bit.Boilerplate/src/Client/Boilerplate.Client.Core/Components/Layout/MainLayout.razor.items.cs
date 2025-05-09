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
            if ((await authorizationService.AuthorizeAsync(user, AppPermissions.AdminPanel.Dashboard)).Succeeded ||
                (await authorizationService.AuthorizeAsync(user, AppPermissions.AdminPanel.ManageProductCatalog)).Succeeded)
            {
                BitNavItem adminPanelItem = new()
                {
                    Text = localizer[nameof(AppStrings.AdminPanel)],
                    IconName = BitIconName.Admin,
                    ChildItems = []
                };

                navPanelItems.Add(adminPanelItem);

                if ((await authorizationService.AuthorizeAsync(user, AppPermissions.AdminPanel.Dashboard)).Succeeded)
                {
                    adminPanelItem.ChildItems.Add(new()
                    {
                        Text = localizer[nameof(AppStrings.Dashboard)],
                        IconName = BitIconName.BarChartVerticalFill,
                        Url = Urls.DashboardPage,
                    });
                }

                if ((await authorizationService.AuthorizeAsync(user, AppPermissions.AdminPanel.ManageProductCatalog)).Succeeded)
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
            if ((await authorizationService.AuthorizeAsync(user, AppPermissions.Todo.ManageTodo)).Succeeded)
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
            if ((await authorizationService.AuthorizeAsync(user, AppPermissions.Management.ManageRoles)).Succeeded ||
                (await authorizationService.AuthorizeAsync(user, AppPermissions.Management.ManageUsers)).Succeeded ||
                (await authorizationService.AuthorizeAsync(user, AppPermissions.Management.ManageAiPrompt)).Succeeded)
            {
                BitNavItem managementItem = new()
                {
                    Text = localizer[nameof(AppStrings.Management)],
                    IconName = BitIconName.SettingsSecure,
                    ChildItems = []
                };

                navPanelItems.Add(managementItem);

                if ((await authorizationService.AuthorizeAsync(user, AppPermissions.Management.ManageRoles)).Succeeded)
                {
                    managementItem.ChildItems.Add(new()
                    {
                        Text = localizer[nameof(AppStrings.UserGroups)],
                        IconName = BitIconName.SecurityGroup,
                        Url = Urls.RolesPage,
                    });
                }

                if ((await authorizationService.AuthorizeAsync(user, AppPermissions.Management.ManageUsers)).Succeeded)
                {
                    managementItem.ChildItems.Add(new()
                    {
                        Text = localizer[nameof(AppStrings.Users)],
                        IconName = BitIconName.SecurityGroup,
                        Url = Urls.UsersPage,
                    });
                }

                //#if (signalR == true)
                if ((await authorizationService.AuthorizeAsync(user, AppPermissions.Management.ManageAiPrompt)).Succeeded)
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
