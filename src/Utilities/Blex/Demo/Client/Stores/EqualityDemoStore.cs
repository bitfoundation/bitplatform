namespace Bit.Blex.Demo.Client.Stores;

/// <summary>
/// Demonstrates the one rule that catches everybody: change detection uses
/// <see cref="EqualityComparer{T}.Default"/>, which for an ordinary collection means
/// <em>reference</em> equality. Mutating a list in place leaves the reference unchanged, so no
/// notification fires; assigning a new instance is what makes the change visible.
/// </summary>
[BlexStoreAttribute(Name = "equalityDemo")]
public partial class EqualityDemoStore
{
    // New items are numbered from the list itself rather than from a counter field of their own: an undo or
    // a time-travel jump restores the list but not such a field, and the next item would repeat a number.
    [BlexStateAttribute] private List<string> _items = ["alpha"];

    [BlexComputedAttribute] private int ComputeCount() => Items.Count;

    /// <summary>The trap: the list contents change but the reference does not, so nothing notifies.</summary>
    [BlexActionAttribute]
    private void OnMutateInPlace() => Items.Add($"item {Items.Count}");

    /// <summary>The fix: assign a new list, so the reference differs and change detection fires.</summary>
    [BlexActionAttribute]
    private void OnAssignNew() => Items = [.. Items, $"item {Items.Count}"];

    [BlexActionAttribute]
    private void OnReset()
    {
        // Already back to its one item: a fresh ["alpha"] would still be a new reference, which change
        // detection counts as a change, and record a Reset that changed nothing.
        if (Items is ["alpha"])
            return;

        Items = ["alpha"];
    }
}
