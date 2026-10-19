namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Stands in for the subscribers every real document has.
/// </summary>
/// <remarks>
/// <para>
/// A document only encodes the update a transaction produced when something is listening to
/// <see cref="BcrdtDocument.Updated"/>. In an application something always is - the sync provider
/// that broadcasts it and the persistence binding that stores it both subscribe - so every local
/// keystroke and every applied remote update pays for an encoding. A benchmark fixture with no
/// subscriber skips that work entirely and reports a number no user ever experiences.
/// </para>
/// <para>
/// Attaching this sink to every document a benchmark mutates puts the encoding back on the measured
/// path, and keeps the update allocation in the <c>[MemoryDiagnoser]</c> figures.
/// </para>
/// </remarks>
internal static class UpdateSink
{
    private static long _bytesSeen;

    /// <summary>The total size of the updates the sink has consumed, so the work cannot be elided.</summary>
    public static long BytesSeen => Interlocked.Read(ref _bytesSeen);

    /// <summary>Subscribes the sink to a document's updates.</summary>
    public static BcrdtDocument Attach(BcrdtDocument document)
    {
        document.Updated += Consume;
        return document;
    }

    private static void Consume(BcrdtUpdateEventArgs args) => Interlocked.Add(ref _bytesSeen, args.Update.Length);
}
