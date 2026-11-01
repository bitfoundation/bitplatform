namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>The amount paid with one method; the color paints its slice of the donut and its legend dot.</summary>
public sealed record PosPaymentShare(string MethodKey, decimal Amount, string Color);
