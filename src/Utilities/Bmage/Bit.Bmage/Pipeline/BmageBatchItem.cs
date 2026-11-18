namespace Bit.Bmage;

/// <summary>One input for a batch run.</summary>
/// <param name="Data">The encoded file. Empty for an item made by <see cref="Deferred"/>.</param>
/// <param name="FileName">A name to report the item by.</param>
/// <param name="Tag">Anything the caller wants back with the result.</param>
public sealed record BmageBatchItem(ReadOnlyMemory<byte> Data, string? FileName = null, object? Tag = null)
{
    /// <summary>
    /// An item whose file is read only when the batch reaches it, so a batch of a thousand photos never holds a
    /// thousand files: only those of the images in flight.
    /// </summary>
    public static BmageBatchItem Deferred(Func<CancellationToken, ValueTask<ReadOnlyMemory<byte>>> load, string? fileName = null, object? tag = null)
    {
        ArgumentNullException.ThrowIfNull(load);
        return new BmageBatchItem(ReadOnlyMemory<byte>.Empty, fileName, tag) { Loader = load };
    }

    /// <summary>Reads the file of a deferred item.</summary>
    internal Func<CancellationToken, ValueTask<ReadOnlyMemory<byte>>>? Loader { get; init; }

    /// <summary>True when the file is read only when the batch reaches the item.</summary>
    public bool IsDeferred => Loader is not null;
}
