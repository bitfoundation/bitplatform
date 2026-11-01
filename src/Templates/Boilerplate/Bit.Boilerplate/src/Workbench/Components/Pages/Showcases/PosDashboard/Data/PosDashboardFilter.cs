namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>The dashboard's filter: a store's resource key and the days the period covers. Widgets reload when it changes.</summary>
public sealed record PosDashboardFilter(string StoreKey, int Days)
{
    /// <summary>All stores, today.</summary>
    public static PosDashboardFilter Default { get; } = new(PosDashboardDataService.StoreKeys[0], 1);
}
