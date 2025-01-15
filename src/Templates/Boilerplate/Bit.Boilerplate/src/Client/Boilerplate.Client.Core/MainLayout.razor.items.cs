namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout
{
    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;

    private void InitializeNavPanelItems()
    {
        navPanelItems =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = Urls.HomePage,
            },
            //#if (module == "Admin")
            new()
            {
                Text = localizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems =
                [
                    new() {
                        Text = localizer[nameof(AppStrings.Dashboard)],
                        IconName = BitIconName.BarChartVerticalFill,
                        Url = Urls.DashboardPage,
                    },
                    new() {
                        Text = localizer[nameof(AppStrings.Categories)],
                        IconName = BitIconName.BuildQueue,
                        Url = Urls.CategoriesPage,
                    },
                    new() {
                        Text = localizer[nameof(AppStrings.Products)],
                        IconName = BitIconName.Product,
                        Url = Urls.ProductsPage,
                    }
                ]
            },
            //#endif
            //#if (sample == true)
            new()
            {
                Text = localizer[nameof(AppStrings.Todo)],
                IconName = BitIconName.ToDoLogoOutline,
                Url = Urls.TodoPage,
            },
            //#endif
            //#if (offlineDb == true)
            new()
            {
                Text = localizer[nameof(AppStrings.OfflineEditProfileTitle)],
                IconName = BitIconName.EditContact,
                Url = Urls.OfflineEditProfilePage,
            },
            //#endif
            new()
            {
                Text = localizer[nameof(AppStrings.Terms)],
                IconName = BitIconName.EntityExtraction,
                Url = Urls.TermsPage,
            }
        ];

        // Currently, the "About" page is absent from the Client/Core project, rendering it inaccessible on the web platform.
        // In order to exhibit a sample page that grants direct access to native functionalities without dependence on
        // dependency injection (DI) or publish-subscribe patterns, the "About" page is integrated within Blazor
        // hybrid projects like Client/Maui.
        if (AppPlatform.IsBlazorHybrid)
        {
            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.AboutTitle)],
                IconName = BitIconName.Info,
                Url = Urls.AboutPage,
            });
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
            ]
        });
    }
}
