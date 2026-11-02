namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

/// <summary>
/// The card the assistant draws for its <c>low_stock</c> generative UI: each product running low, the units left
/// and the quantity to order.
/// </summary>
public partial class AssistantStockCard
{
    [Parameter] public AssistantStockItem[] Items { get; set; } = [];
}
