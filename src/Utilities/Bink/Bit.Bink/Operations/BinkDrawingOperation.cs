namespace Bit.Bink;

/// <summary>
/// An atomic, invertible change to an <see cref="BinkDocument"/>. Operations are the unit of undo/redo, of
/// document diffing and of collaboration: every mutation of a document is expressed as one.
/// </summary>
/// <remarks>
/// Operations reference elements by id and carry the new element instances (which share their point data), so
/// applying, inverting and transmitting them never copies stroke points.
/// </remarks>
public abstract record BinkDrawingOperation
{
    /// <summary>Short label for history UIs, e.g. "Add stroke".</summary>
    public abstract string Label { get; }

    /// <summary>Applies the operation to a document and returns its inverse. Throws when the operation cannot be applied.</summary>
    /// <param name="document">The document.</param>
    /// <param name="applied">The operation as it was applied: with the order key and clock it was placed with, when it places something, so that history and collaborators repeat exactly what happened here.</param>
    internal abstract BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied);

    /// <summary>
    /// Whether the operation can be applied to the document in its current state (e.g. the referenced element exists).
    /// Used to skip stale remote operations instead of throwing.
    /// </summary>
    public abstract bool CanApply(BinkDocument document);
}

/// <summary>Adds an element to a layer.</summary>
/// <param name="LayerId">Target layer.</param>
/// <param name="Element">The element to add. Its id must not already exist in the document.</param>
/// <param name="Index">Insertion index within the layer, or null to append on top.</param>
public sealed record BinkAddElementOperation(Guid LayerId, BinkElement Element, int? Index = null) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Add " + Element.TypeName.ToLowerInvariant();

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _) && !document.ContainsElement(Element.Id);

    /// <summary>
    /// The element's order key, which places it in the layer when given; otherwise <see cref="Index"/> does and the
    /// document makes the key. See <see cref="BinkOrderKey"/>.
    /// </summary>
    public string? OrderKey { get; init; }

    /// <summary>The move clock the element was placed with, or 0 for a new move; see <see cref="BinkReorderElementOperation.OrderClock"/>.</summary>
    public long OrderClock { get; init; }

    /// <summary>Set for a snapshot's additions, which state the key and clock to store, as they are.</summary>
    internal bool Authoritative { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        (string key, long clock) = document.InsertElementCore(LayerId, Element, Index, OrderKey, OrderClock, Authoritative);
        applied = key == OrderKey && clock == OrderClock ? this : this with { OrderKey = key, OrderClock = clock };
        return new BinkRemoveElementOperation(Element.Id);
    }
}

/// <summary>Removes an element.</summary>
/// <param name="ElementId">The element to remove.</param>
public sealed record BinkRemoveElementOperation(Guid ElementId) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Delete";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.ContainsElement(ElementId);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        (BinkLayer layer, int index, BinkElement removed, string key, long clock) = document.RemoveElementCore(ElementId);
        // Undoing puts the element back at its key, which is its place whatever has moved around it meanwhile. The
        // clock is what a rolled-back batch restores exactly; an undo is a move made now and takes a new one.
        return new BinkAddElementOperation(layer.Id, removed, index) { OrderKey = key, OrderClock = clock };
    }
}

/// <summary>Replaces an element with a new version of itself (same id). Covers move, transform, style changes and edits.</summary>
/// <param name="Element">The new element. The document assigns it the next version number.</param>
public sealed record BinkReplaceElementOperation(BinkElement Element) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Edit " + Element.TypeName.ToLowerInvariant();

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.ContainsElement(Element.Id);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        BinkElement previous = document.ReplaceElementCore(Element);
        return new BinkReplaceElementOperation(previous);
    }
}

/// <summary>Moves an element to a new z-order index and/or a different layer.</summary>
/// <param name="ElementId">The element.</param>
/// <param name="TargetLayerId">Destination layer.</param>
/// <param name="Index">Destination index within the layer (clamped), or null for the top.</param>
public sealed record BinkReorderElementOperation(Guid ElementId, Guid TargetLayerId, int? Index) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Reorder";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.ContainsElement(ElementId) && document.TryGetLayer(TargetLayerId, out _);

    /// <summary>
    /// The order key the element moves to, which places it when given; otherwise <see cref="Index"/> does and the
    /// document makes the key. See <see cref="BinkOrderKey"/>.
    /// </summary>
    public string? OrderKey { get; init; }

    /// <summary>
    /// The Lamport clock of the move, or 0 for a new move here. When two collaborators move one element at once, the
    /// move with the greater clock (then key, then layer id) wins on every replica, so they end in the same order.
    /// </summary>
    public long OrderClock { get; init; }

    /// <summary>Set for a snapshot's moves, which apply whatever clock the element was last moved with.</summary>
    internal bool Authoritative { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        var moved = document.ReorderElementCore(ElementId, TargetLayerId, Index, OrderKey, OrderClock, Authoritative);
        applied = moved.Key == OrderKey && moved.Clock == OrderClock ? this : this with { OrderKey = moved.Key, OrderClock = moved.Clock };
        return new BinkReorderElementOperation(ElementId, moved.PreviousLayerId, moved.PreviousIndex) { OrderKey = moved.PreviousKey, OrderClock = moved.PreviousClock };
    }
}

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

/// <summary>Removes a layer and everything in it.</summary>
/// <param name="LayerId">The layer.</param>
public sealed record BinkRemoveLayerOperation(Guid LayerId) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Remove layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _) && document.Layers.Count > 1;

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        (int index, BinkLayer removed, string key, long clock) = document.RemoveLayerCore(LayerId);
        return new BinkAddLayerOperation(removed.Id, removed.Properties, index, removed.Elements.ToArray())
        {
            OrderKey = key,
            OrderClock = clock,
            ElementOrderKeys = removed.Keys.ToArray(),
            ElementOrderClocks = removed.Clocks.ToArray(),
        };
    }
}

/// <summary>Changes a layer's properties (name, visibility, lock, opacity).</summary>
/// <param name="LayerId">The layer.</param>
/// <param name="Properties">New properties.</param>
public sealed record BinkUpdateLayerOperation(Guid LayerId, BinkLayerProperties Properties) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Change layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        BinkLayerProperties previous = document.UpdateLayerCore(LayerId, Properties);
        return new BinkUpdateLayerOperation(LayerId, previous);
    }
}

/// <summary>Moves a layer to a new index in the layer stack.</summary>
/// <param name="LayerId">The layer.</param>
/// <param name="Index">New index (0 = bottom).</param>
public sealed record BinkReorderLayerOperation(Guid LayerId, int Index) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Reorder layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _);

    /// <summary>The order key the layer moves to, which places it when given; otherwise <see cref="Index"/> does.</summary>
    public string? OrderKey { get; init; }

    /// <summary>The Lamport clock of the move, or 0 for a new move; concurrent moves resolve as <see cref="BinkReorderElementOperation.OrderClock"/> does.</summary>
    public long OrderClock { get; init; }

    /// <summary>Set for a snapshot's moves, which always apply.</summary>
    internal bool Authoritative { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        var moved = document.ReorderLayerCore(LayerId, Index, OrderKey, OrderClock, Authoritative);
        applied = moved.Key == OrderKey && moved.Clock == OrderClock ? this : this with { OrderKey = moved.Key, OrderClock = moved.Clock };
        return new BinkReorderLayerOperation(LayerId, moved.PreviousIndex) { OrderKey = moved.PreviousKey, OrderClock = moved.PreviousClock };
    }
}

/// <summary>Changes the document settings (size, background).</summary>
/// <param name="Settings">New settings.</param>
public sealed record BinkUpdateSettingsOperation(BinkDocumentSettings Settings) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Change document";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => true;

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        BinkDocumentSettings previous = document.UpdateSettingsCore(Settings);
        return new BinkUpdateSettingsOperation(previous);
    }
}

/// <summary>Sets or removes a document metadata entry.</summary>
/// <param name="Key">Metadata key.</param>
/// <param name="Value">New value, or null to remove.</param>
public sealed record BinkSetMetadataOperation(string Key, string? Value) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Change metadata";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => !string.IsNullOrEmpty(Key);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        string? previous = document.SetMetadataCore(Key, Value);
        return new BinkSetMetadataOperation(Key, previous);
    }
}

/// <summary>A group of operations applied atomically and undone as one step.</summary>
/// <param name="Operations">Operations in application order.</param>
/// <param name="Label">Optional label; defaults to the first operation's label.</param>
public sealed record BinkBatchOperation(IReadOnlyList<BinkDrawingOperation> Operations, string? Label = null) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label { get; } = Label ?? (Operations.Count > 0 ? Operations[0].Label : "Batch");

    /// <summary>True when the batch contains nothing.</summary>
    public bool IsEmpty => Operations.Count == 0;

    /// <inheritdoc />
    /// <remarks>
    /// Only the first operation is checked: the later ones depend on what the earlier ones change, so whether they
    /// apply is only known by applying them, and a batch that fails part way is rolled back. A collaboration session
    /// therefore applies a remote batch one operation at a time instead, skipping the ones that no longer apply.
    /// </remarks>
    public override bool CanApply(BinkDocument document) => Operations.Count == 0 || Operations[0].CanApply(document);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        var inverses = new BinkDrawingOperation[Operations.Count];
        BinkDrawingOperation[]? resolved = null;
        int count = 0;
        try
        {
            for (int i = 0; i < Operations.Count; i++)
            {
                inverses[Operations.Count - 1 - i] = Operations[i].ApplyCore(document, out BinkDrawingOperation done);
                if (!ReferenceEquals(done, Operations[i]) && resolved is null)
                {
                    resolved = new BinkDrawingOperation[Operations.Count];
                    for (int j = 0; j < i; j++) resolved[j] = Operations[j];
                }
                if (resolved is not null) resolved[i] = done;
                count++;
            }
        }
        catch
        {
            // Roll back the partially applied batch so the document is never left half-changed. The rollback is
            // never reported, so it has to restore elements exactly: a replacement undone the ordinary way gets a
            // newer version, and this replica would then out-rank every collaborator's next edit of the element.
            bool restoring = document.Restoring;
            document.Restoring = true;
            try
            {
                for (int i = 0; i < count; i++) inverses[Operations.Count - count + i].ApplyCore(document, out _);
            }
            finally
            {
                document.Restoring = restoring;
            }
            throw;
        }
        applied = resolved is null ? this : new BinkBatchOperation(resolved, Label);
        return new BinkBatchOperation(inverses, Label);
    }
}
