namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

/// <summary>
/// A widget the catalog offers: its key, title and icon, the component that draws it, its default size and the
/// parameters that component gets, like the KPI widget's metric.
/// </summary>
public sealed record PosWidgetDefinition(string Key,
                                         string TitleKey,
                                         string IconName,
                                         Type ComponentType,
                                         PosWidgetSize DefaultSize,
                                         IReadOnlyDictionary<string, object?>? Parameters = null);
