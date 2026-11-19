namespace Bit.Bcrdt;

/// <summary>The marker type used to tag updates a persistence layer applied.</summary>
public sealed class BcrdtDocumentPersistenceOrigin
{
    /// <inheritdoc/>
    public override string ToString() => "persistence";
}
