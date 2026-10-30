namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

public static class DashboardDemoData
{
    public static IReadOnlyList<(string Name, string Color, int ProductCount)> Categories { get; } =
    [
        ("Ford", "#FFCD56", 8),
        ("Nissan", "#FF6384", 8),
        ("Benz", "#4BC0C0", 19),
        ("BMW", "#FF9124", 8),
        ("Tesla", "#2B88D8", 7)
    ];

    public const int Last30DaysProductCount = 12;
}
