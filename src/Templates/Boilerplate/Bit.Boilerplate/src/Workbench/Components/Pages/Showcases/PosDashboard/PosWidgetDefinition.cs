namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

public sealed record PosWidgetDefinition(string Key,
                                         string TitleKey,
                                         string IconName,
                                         Type ComponentType,
                                         PosWidgetSize DefaultSize,
                                         IReadOnlyDictionary<string, object?>? Parameters = null);
