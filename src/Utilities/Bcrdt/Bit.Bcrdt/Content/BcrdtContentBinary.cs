namespace Bit.Bcrdt;

/// <summary>An opaque byte array stored as a single collaborative unit.</summary>
/// <param name="data">The bytes to store. Treated as immutable.</param>
public sealed class BcrdtContentBinary(byte[] data) : IBcrdtContent
{
    /// <summary>The stored bytes.</summary>
    public byte[] Data { get; } = data;

    /// <inheritdoc/>
    public int Length => 1;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Binary;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [Data];

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentBinary(Data);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset) => throw new InvalidOperationException("Binary content is indivisible.");

    /// <inheritdoc/>
    public bool CanMergeWith(IBcrdtContent right) => false;

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right) => false;

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) => writer.WriteBytes(Data);

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentBinary Read(ref BcrdtBufferReader reader) => new(reader.ReadBytes());
}
