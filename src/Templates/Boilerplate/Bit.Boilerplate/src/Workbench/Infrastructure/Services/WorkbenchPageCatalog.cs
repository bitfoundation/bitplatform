//+:cnd:noEmit
//#if (module == "Admin")
using Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;
//#endif
using Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The pages the Workbench shows, with their texts, icons, categories, design options and switches. A new page is added here.
/// </summary>
public static class WorkbenchPageCatalog
{
    public static IReadOnlyList<WorkbenchPageInfo> Pages { get; } =
    [
        new("theme",
            nameof(WorkbenchStrings.ThemePageTitle),
            nameof(WorkbenchStrings.ThemePageDescription),
            BitIconName.Color,
            typeof(ThemePage),
            nameof(WorkbenchStrings.CategoryDesignSystem),
            ["BitButton", "BitTextField", "BitDropdown", "BitNavBar", "BitDialog", "BitDataGrid", "BitAiChat"],
            nameof(WorkbenchStrings.ThemePageCaption))
        {
            Switches =
            [
                // The page's tabs, in their order: the token families, the overlays, then the demo's categories of components.
                new("section", nameof(WorkbenchStrings.ThemeSwitchSection),
                [
                    new("colors", nameof(WorkbenchStrings.ThemeColors)),
                    new("typography", nameof(WorkbenchStrings.ThemeTypography)),
                    new("shape", nameof(WorkbenchStrings.ThemeShape)),
                    new("shadows", nameof(WorkbenchStrings.ThemeShadows)),
                    new("sizes", nameof(WorkbenchStrings.ThemeSizes)),
                    new("motion", nameof(WorkbenchStrings.ThemeMotion)),
                    new("layers", nameof(WorkbenchStrings.ThemeLayers)),
                    new("overlays", nameof(WorkbenchStrings.ThemeOverlays)),
                    new("buttons", nameof(WorkbenchStrings.ThemeButtons)),
                    new("inputs", nameof(WorkbenchStrings.ThemeInputs)),
                    new("pickers", nameof(WorkbenchStrings.ThemePickers)),
                    new("layouts", nameof(WorkbenchStrings.ThemeLayouts)),
                    new("lists", nameof(WorkbenchStrings.ThemeLists)),
                    new("navs", nameof(WorkbenchStrings.ThemeNavigation)),
                    new("notifications", nameof(WorkbenchStrings.ThemeNotifications)),
                    new("progress", nameof(WorkbenchStrings.ThemeProgressSection)),
                    new("surfaces", nameof(WorkbenchStrings.ThemeSurfaces)),
                    new("utilities", nameof(WorkbenchStrings.ThemeUtilities)),
                    new("extras", nameof(WorkbenchStrings.ThemeExtras)),
                    new("ai", nameof(WorkbenchStrings.ThemeAi))
                ]),
                new("density", nameof(WorkbenchStrings.ThemeSwitchDensity),
                [
                    new("default", nameof(WorkbenchStrings.ThemeStateDefault)),
                    new("compact", nameof(WorkbenchStrings.ThemeDensityCompact)),
                    new("spacious", nameof(WorkbenchStrings.ThemeDensitySpacious))
                ]),
                new("overlay", nameof(WorkbenchStrings.ThemeSwitchOverlay),
                [
                    new("none", nameof(WorkbenchStrings.ThemeOverlayNone)),
                    new("dropdown", nameof(WorkbenchStrings.ThemeOverlayDropdown)),
                    new("date", nameof(WorkbenchStrings.ThemeOverlayDatePicker)),
                    new("menu", nameof(WorkbenchStrings.ThemeOverlayMenu)),
                    new("callout", nameof(WorkbenchStrings.ThemeOverlayCallout)),
                    new("dialog", nameof(WorkbenchStrings.ThemeOverlayDialog)),
                    new("modal", nameof(WorkbenchStrings.ThemeOverlayModal)),
                    new("panel", nameof(WorkbenchStrings.ThemeOverlayPanel)),
                    new("snackbar", nameof(WorkbenchStrings.ThemeOverlaySnackBar))
                ])
            ]
        },
        new("pos-dashboard",
            nameof(WorkbenchStrings.PosDashboardTitle),
            nameof(WorkbenchStrings.PosDashboardDescription),
            BitIconName.BarChartVerticalFill,
            typeof(PosDashboardPage),
            nameof(WorkbenchStrings.CategoryDashboards),
            ["bit Bortable", "BitChart", "BitShimmer", "bit Bmotion"],
            nameof(WorkbenchStrings.PosDashboardCaption)),
        new("buildings",
            nameof(WorkbenchStrings.BuildingsTitle),
            nameof(WorkbenchStrings.BuildingsDescription),
            BitIconName.MapPin,
            typeof(BuildingsPage),
            nameof(WorkbenchStrings.CategoryDataAndMaps),
            ["BitDataGrid", "BitMap", "BitModal", "BitMessageBox", "BitSnackBar"],
            nameof(WorkbenchStrings.BuildingsCaption))
        {
            Variants =
            [
                new("table", nameof(WorkbenchStrings.BuildingsVariantTable)),
                new("cards", nameof(WorkbenchStrings.BuildingsVariantCards))
            ],
            Switches =
            [
                new("amount", nameof(WorkbenchStrings.BuildingsSwitchAmount),
                [
                    new("normal", nameof(WorkbenchStrings.BuildingsAmountNormal)),
                    new("few", nameof(WorkbenchStrings.BuildingsAmountFew)),
                    new("many", nameof(WorkbenchStrings.BuildingsAmountMany))
                ]),
                new("names", nameof(WorkbenchStrings.BuildingsSwitchNames),
                [
                    new("short", nameof(WorkbenchStrings.BuildingsNamesAsTheyAre)),
                    new("long", nameof(WorkbenchStrings.BuildingsNamesLong))
                ])
            ]
        },
        new("assistant",
            nameof(WorkbenchStrings.AssistantTitle),
            nameof(WorkbenchStrings.AssistantDescription),
            BitIconName.Robot,
            typeof(AssistantPage),
            nameof(WorkbenchStrings.CategoryAi),
            ["BitAiChat", "BitAiConversationList", "BitAiToolCallView", "BitAiGenerativeUiView", "BitAiArtifactView"],
            nameof(WorkbenchStrings.AssistantCaption)),
        new("shifts",
            nameof(WorkbenchStrings.ShiftsTitle),
            nameof(WorkbenchStrings.ShiftsDescription),
            BitIconName.Calendar,
            typeof(ShiftsPage),
            nameof(WorkbenchStrings.CategoryPlanning),
            ["BitFullCalendar", "BitMessage", "bit Blex"],
            nameof(WorkbenchStrings.ShiftsCaption)),
        //#if (module == "Admin")
        new("dashboard",
            nameof(WorkbenchStrings.AppDashboardTitle),
            nameof(WorkbenchStrings.AppDashboardDescription),
            BitIconName.BarChartVerticalFill,
            // The app's own page, on a sample product catalog (See DashboardHost).
            typeof(DashboardHost),
            nameof(WorkbenchStrings.CategoryAppPages),
            ["BitChart", "BitCard", "BitEllipsisLoading"],
            nameof(WorkbenchStrings.AppDashboardCaption)),
        //#endif
        new("settings",
            nameof(WorkbenchStrings.SettingsTitle),
            nameof(WorkbenchStrings.SettingsDescription),
            BitIconName.Equalizer,
            // The app's own page, on the view's sample account (See SettingsHost).
            typeof(SettingsHost),
            nameof(WorkbenchStrings.CategoryAppPages),
            ["BitAccordionList", "BitPivot", "BitOtpInput", "BitFileUpload"],
            nameof(WorkbenchStrings.SettingsCaption))
        {
            Switches =
            [
                new(SettingsHost.SectionSwitch, nameof(WorkbenchStrings.SettingsSwitchSection),
                [
                    new(PageUrls.SettingsSections.Profile, nameof(WorkbenchStrings.SettingsSectionProfile)),
                    new(PageUrls.SettingsSections.Account, nameof(WorkbenchStrings.SettingsSectionAccount)),
                    new(PageUrls.SettingsSections.Tfa, nameof(WorkbenchStrings.SettingsSectionTfa)),
                    new(PageUrls.SettingsSections.Sessions, nameof(WorkbenchStrings.SettingsSectionSessions)),
                    new(PageUrls.SettingsSections.Privacy, nameof(WorkbenchStrings.SettingsSectionPrivacy)),
                    //#if (ads == true)
                    new(PageUrls.SettingsSections.UpgradeAccount, nameof(WorkbenchStrings.SettingsSectionUpgrade)),
                    //#endif
                ]),
                new(SettingsSampleAccount.TwoFactorSwitch, nameof(WorkbenchStrings.SettingsSwitchTwoFactor),
                [
                    new("off", nameof(WorkbenchStrings.SettingsTwoFactorOff)),
                    new(SettingsSampleAccount.On, nameof(WorkbenchStrings.SettingsTwoFactorOn))
                ]),
                new(SettingsSampleAccount.PasskeySwitch, nameof(WorkbenchStrings.SettingsSwitchPasskey),
                [
                    new("off", nameof(WorkbenchStrings.SettingsPasskeyOff)),
                    new(SettingsSampleAccount.On, nameof(WorkbenchStrings.SettingsPasskeyOn))
                ]),
                // What removing a session, changing the email or deleting the account first ask for (AuthManager.TryEnterElevatedAccessMode).
                new(WorkbenchElevation.Switch, nameof(WorkbenchStrings.SettingsSwitchConfirm),
                [
                    new("recent", nameof(WorkbenchStrings.SettingsConfirmRecent)),
                    new(WorkbenchElevation.AskCase, nameof(WorkbenchStrings.SettingsConfirmAsk))
                ])
            ]
        },
        new("landing-minimal",
            nameof(WorkbenchStrings.LandingMinimalTitle),
            nameof(WorkbenchStrings.LandingMinimalDescription),
            BitIconName.Globe,
            typeof(MinimalLandingPage),
            nameof(WorkbenchStrings.CategoryLandingPages),
            ["BitTextField", "BitCheckbox", "BitPanel"],
            nameof(WorkbenchStrings.LandingMinimalCaption))
        {
            HasAppShell = false,
            Switches =
            [
                new("consent", nameof(WorkbenchStrings.LandingSwitchConsent),
                [
                    new("decided", nameof(WorkbenchStrings.LandingConsentDecided)),
                    new("first-visit", nameof(WorkbenchStrings.LandingConsentFirstVisit))
                ])
            ]
        }
    ];

    /// <summary>The page with this key, ignoring case, or null.</summary>
    public static WorkbenchPageInfo? Find(string? key) => Pages.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
