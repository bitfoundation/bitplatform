namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Navs category: breadcrumbs, the drop menu, the nav and its tree, the nav bar, pagination and tabs.</summary>
public partial class NavsSection
{
    private List<BitBreadcrumbItem> crumbs = [];
    private List<BitBreadcrumbItem> longCrumbs = [];
    private List<BitNavItem> navItems = [];
    private List<BitNavItem> treeItems = [];
    private List<BitNavBarItem> navBarItems = [];

    private string SampleText => Localizer[nameof(WorkbenchStrings.ThemeSampleText)];

    protected override void OnInitialized()
    {
        crumbs =
        [
            new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], Href = "#", IconName = BitIconName.Home },
            new() { Key = "settings", Text = AppLocalizer[nameof(AppStrings.Settings)], Href = "#" },
            new() { Key = "profile", Text = AppLocalizer[nameof(AppStrings.ProfileTitle)], IsSelected = true }
        ];

        longCrumbs =
        [
            new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], Href = "#" },
            new() { Key = "shops", Text = Localizer[nameof(WorkbenchStrings.ThemeNavsShops)], Href = "#" },
            new() { Key = "amsterdam", Text = "Amsterdam", Href = "#" },
            new() { Key = "orders", Text = Localizer[nameof(WorkbenchStrings.ThemeExtrasOrders)], Href = "#" },
            new() { Key = "order", Text = "PO-1042", IsSelected = true }
        ];

        navItems =
        [
            new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], IconName = BitIconName.Home },
            new() { Key = "profile", Text = AppLocalizer[nameof(AppStrings.ProfileTitle)], IconName = BitIconName.Contact },
            new() { Key = "settings", Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Settings }
        ];

        treeItems =
        [
            new() { Key = "dashboard", Text = Localizer[nameof(WorkbenchStrings.AppDashboardTitle)], IconName = BitIconName.ViewDashboard },
            new()
            {
                Key = "shops", Text = Localizer[nameof(WorkbenchStrings.ThemeNavsShops)], IconName = BitIconName.Shop,
                ChildItems =
                [
                    new() { Key = "amsterdam", Text = "Amsterdam" },
                    new() { Key = "rotterdam", Text = "Rotterdam" },
                    new() { Key = "utrecht", Text = "Utrecht", IsDisabled = true }
                ]
            },
            new() { Key = "reports", Text = Localizer[nameof(WorkbenchStrings.ThemeNavsReports)], IconName = BitIconName.ReportDocument }
        ];

        navBarItems =
        [
            new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], IconName = BitIconName.Home },
            new() { Key = "profile", Text = AppLocalizer[nameof(AppStrings.ProfileTitle)], IconName = BitIconName.Contact },
            new() { Key = "settings", Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Settings }
        ];
    }
}
