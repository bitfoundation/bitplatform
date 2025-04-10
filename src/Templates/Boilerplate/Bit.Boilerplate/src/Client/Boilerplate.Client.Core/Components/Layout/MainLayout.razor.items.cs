namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout
{
    private List<BitNavItem> navPanelAuthenticatedItems = [];
    private List<BitNavItem> navPanelUnAuthenticatedItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;

    private void InitializeNavPanelItems()
    {
        BitNavItem homeNavItem = new()
        {
            Text = localizer[nameof(AppStrings.Home)],
            IconName = BitIconName.Home,
            Url = Urls.HomePage,
        };

        BitNavItem termsNavItem = new()
        {
            Text = localizer[nameof(AppStrings.Terms)],
            IconName = BitIconName.EntityExtraction,
            Url = Urls.TermsPage,
        };

        navPanelUnAuthenticatedItems = [homeNavItem, termsNavItem];

        navPanelAuthenticatedItems =
        [
            homeNavItem,
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
            termsNavItem
        ];

        BitNavItem aboutNavItem = new()
        {
            Text = localizer[nameof(AppStrings.About)],
            IconName = BitIconName.Info,
            Url = Urls.AboutPage,
        };

        navPanelAuthenticatedItems.Add(aboutNavItem);
        navPanelUnAuthenticatedItems.Add(aboutNavItem);

        navPanelAuthenticatedItems.Add(new()
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

        //#if (offlineDb == true)
        navPanelAuthenticatedItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.OfflineEditProfileTitle)],
            IconName = BitIconName.EditContact,
            Url = Urls.OfflineEditProfilePage,
        });
        navPanelUnAuthenticatedItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.OfflineEditProfileTitle)],
            IconName = BitIconName.EditContact,
            Url = Urls.OfflineEditProfilePage,
        });
        //#endif

        navPanelAuthenticatedItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.EditSystemPromptsTitle)],
            IconName = BitIconName.TextDocumentSettings,
            Url = Urls.EditSystemPrompts,
        });
    }
}
