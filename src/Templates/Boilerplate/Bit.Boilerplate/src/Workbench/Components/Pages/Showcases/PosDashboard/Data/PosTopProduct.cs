namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>A best-selling product with its units sold and its revenue in the period.</summary>
public sealed record PosTopProduct(string NameKey, string Emoji, int Units, decimal Revenue);
