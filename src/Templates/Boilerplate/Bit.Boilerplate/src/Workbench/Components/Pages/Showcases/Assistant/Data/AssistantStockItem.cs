namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant.Data;

/// <summary>A row of the low-stock card: the product, the units left and the quantity to order.</summary>
public sealed record AssistantStockItem(string Name, int Left, int Order);
