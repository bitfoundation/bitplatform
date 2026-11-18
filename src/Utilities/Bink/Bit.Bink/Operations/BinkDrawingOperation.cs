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
