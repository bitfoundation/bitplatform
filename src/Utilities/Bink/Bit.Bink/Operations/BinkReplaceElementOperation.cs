namespace Bit.Bink;

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
