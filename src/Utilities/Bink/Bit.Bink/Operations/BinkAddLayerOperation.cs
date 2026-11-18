namespace Bit.Bink;

/// <summary>Adds a layer, optionally with elements (used to undo a layer removal).</summary>
/// <param name="LayerId">The layer id.</param>
/// <param name="Properties">Layer properties.</param>
/// <param name="Index">Insertion index (bottom = 0), or null for the top.</param>
/// <param name="Elements">Elements to place in the layer, or null.</param>
public sealed record BinkAddLayerOperation(Guid LayerId, BinkLayerProperties Properties, int? Index = null, IReadOnlyList<BinkElement>? Elements = null) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Add layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => !document.TryGetLayer(LayerId, out _);

    /// <summary>The layer's order key in the layer stack, which places it when given; otherwise <see cref="Index"/> does.</summary>
    public string? OrderKey { get; init; }

    /// <summary>The move clock the layer was placed with, or 0 for a new move.</summary>
    public long OrderClock { get; init; }

    /// <summary>The order keys of <see cref="Elements"/>, in their order, or null to make them from it.</summary>
    public IReadOnlyList<string>? ElementOrderKeys { get; init; }

    /// <summary>The move clocks of <see cref="Elements"/>, kept only for a collaborator's layer.</summary>
    public IReadOnlyList<long>? ElementOrderClocks { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        (string key, long clock) = document.InsertLayerCore(LayerId, Properties, Index, Elements, OrderKey, OrderClock, ElementOrderKeys, ElementOrderClocks);
        BinkLayer layer = document.GetLayer(LayerId);
        // The elements' keys and clocks are copied only when the layer did not keep the ones given: copying them every
        // time cost two arrays the size of the layer on every apply, and made any batch holding it rebuild itself.
        bool sameElements = Elements is not { Count: > 0 } || (SameAs(ElementOrderKeys, layer.Keys) && SameAs(ElementOrderClocks, layer.Clocks));
        if (key == OrderKey && clock == OrderClock && sameElements) applied = this;
        else if (sameElements) applied = this with { OrderKey = key, OrderClock = clock };
        else applied = this with { OrderKey = key, OrderClock = clock, ElementOrderKeys = layer.Keys.ToArray(), ElementOrderClocks = layer.Clocks.ToArray() };
        return new BinkRemoveLayerOperation(LayerId);
    }

    private static bool SameAs<T>(IReadOnlyList<T>? given, IReadOnlyList<T> kept)
    {
        if (given is null || given.Count != kept.Count) return false;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < kept.Count; i++) if (!comparer.Equals(given[i], kept[i])) return false;
        return true;
    }
}
