namespace Bit.Bink;

/// <summary>Raised after a document changed.</summary>
public sealed class BinkDocumentChangedEventArgs : EventArgs
{
    internal BinkDocumentChangedEventArgs(BinkDrawingOperation operation, BinkDrawingOperation inverse, BinkChangeOrigin origin, long revision, int depth)
    {
        Operation = operation;
        Inverse = inverse;
        Origin = origin;
        Revision = revision;
        Depth = depth;
    }

    /// <summary>The operation that was applied.</summary>
    public BinkDrawingOperation Operation { get; }

    /// <summary>The operation that undoes it.</summary>
    public BinkDrawingOperation Inverse { get; }

    /// <summary>Who caused the change.</summary>
    public BinkChangeOrigin Origin { get; }

    /// <summary>The document revision after the change.</summary>
    public long Revision { get; }

    /// <summary>
    /// How deeply this change is nested inside another change's notification. Zero for a change applied on its
    /// own; one for a change a listener applied while handling that one, and so on. Use it to tell a consequence
    /// from its cause, for example to avoid re-deriving geometry that a handler is already deriving.
    /// </summary>
    public int Depth { get; }
}
