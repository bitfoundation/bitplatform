namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>One hour's sales in the chosen period and in the same hour last week.</summary>
public sealed record PosHourlySales(int Hour, decimal Today, decimal LastWeek);
