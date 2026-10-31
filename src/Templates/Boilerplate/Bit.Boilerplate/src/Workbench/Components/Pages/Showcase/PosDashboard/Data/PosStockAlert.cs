namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

public sealed record PosStockAlert(string NameKey, string Emoji, int InStock, int Minimum)
{
    public bool IsOut => InStock == 0;
}
