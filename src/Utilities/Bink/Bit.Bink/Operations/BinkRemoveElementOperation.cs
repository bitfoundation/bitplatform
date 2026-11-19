namespace Bit.Bink;

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
