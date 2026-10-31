//#if (module == "Admin")
using Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;
//#endif

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchPageCatalog
{
    public static IReadOnlyList<WorkbenchPageInfo> Pages { get; } =
    [
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
            typeof(DashboardPage),
            nameof(WorkbenchStrings.CategoryAppPages),
            ["BitChart", "BitCard", "BitEllipsisLoading"],
            nameof(WorkbenchStrings.AppDashboardCaption)),
        //#endif
        new("settings",
            nameof(WorkbenchStrings.SettingsTitle),
            nameof(WorkbenchStrings.SettingsDescription),
            BitIconName.Equalizer,
            typeof(SettingsPage),
            nameof(WorkbenchStrings.CategoryAppPages),
            ["BitAccordionList", "BitPivot", "BitOtpInput", "BitFileUpload"],
            nameof(WorkbenchStrings.SettingsCaption))
    ];

    public static WorkbenchPageInfo? Find(string? key) => Pages.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
