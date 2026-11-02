namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>How many orders came in on one weekday at one hour: a cell of the peak hours heat map.</summary>
public sealed record PosHeatCell(DayOfWeek Day, int Hour, int Orders);
