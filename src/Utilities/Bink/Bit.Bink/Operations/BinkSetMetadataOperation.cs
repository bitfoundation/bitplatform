namespace Bit.Bink;

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
