namespace Bit.Blex.Demo.Client.Stores;

/// <summary>A line item in the <see cref="CartStore"/>.</summary>
public sealed record CartLine(string Sku, string Name, decimal UnitPrice, int Quantity)
{
    /// <summary>Line total, used by the store's computed properties.</summary>
    public decimal Total => UnitPrice * Quantity;
}

/// <summary>
/// Shows how <c>[BlexComputedAttribute]</c> memoization behaves. Every compute method bumps a public
/// evaluation counter, so the docs page can prove that reading a computed property ten times
/// only evaluates it once -- and that a state change is what invalidates it.
/// </summary>
[BlexStoreAttribute(Name = "cart")]
public partial class CartStore
{
    [BlexStateAttribute] private IReadOnlyList<CartLine> _lines =
    [
        new("REF-001", "Blex T-shirt", 24.00m, 1),
        new("REF-002", "Sticker pack", 6.50m, 2),
    ];

    [BlexStateAttribute] private decimal _discountRate;

    /// <summary>How many times each compute method has actually run (not a <c>[BlexStateAttribute]</c> field).</summary>
    /// <remarks>
    /// The compute methods only bump the counters, in a statement of their own, so the generator does not count
    /// them as inputs: <see cref="Subtotal"/> still depends on <see cref="Lines"/> alone. Reading one in a compute
    /// method would make that computed stale on every write.
    /// </remarks>
    public int SubtotalEvaluations { get; private set; }

    /// <summary>Evaluation counter for <see cref="Total"/>.</summary>
    public int TotalEvaluations { get; private set; }

    [BlexComputedAttribute]
    private decimal ComputeSubtotal()
    {
        SubtotalEvaluations++;
        return Lines.Sum(l => l.Total);
    }

    /// <summary>Computed values may build on other computed values.</summary>
    [BlexComputedAttribute]
    private decimal ComputeTotal()
    {
        TotalEvaluations++;
        return Math.Round(Subtotal * (1 - DiscountRate), 2);
    }

    [BlexComputedAttribute] private int ComputeItemCount() => Lines.Sum(l => l.Quantity);

    /// <summary>
    /// Sets one line's quantity, never below zero. A quantity the line already has assigns nothing, so the action
    /// records and invalidates nothing: a rebuilt list would be a new reference, which change detection counts as a
    /// change.
    /// </summary>
    [BlexActionAttribute]
    private void OnSetQuantity(string sku, int quantity)
    {
        quantity = Math.Max(0, quantity);
        if (Lines.All(l => l.Sku != sku || l.Quantity == quantity))
            return;

        Lines = [.. Lines.Select(l => l.Sku == sku ? l with { Quantity = quantity } : l)];
    }

    [BlexActionAttribute] private void OnSetDiscount(decimal rate) => DiscountRate = Math.Clamp(rate, 0m, 0.9m);

    /// <summary>Resets the evaluation counters so a demo can be replayed from a clean slate.</summary>
    public void ResetEvaluationCounters()
    {
        SubtotalEvaluations = 0;
        TotalEvaluations = 0;
    }
}
