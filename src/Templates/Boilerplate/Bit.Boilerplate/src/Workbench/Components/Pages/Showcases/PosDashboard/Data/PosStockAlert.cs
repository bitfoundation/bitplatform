namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>A product running low: how many are left and the minimum to keep in stock.</summary>
public sealed record PosStockAlert(string NameKey, string Emoji, int InStock, int Minimum)
{
    public bool IsOut => InStock == 0;
}
