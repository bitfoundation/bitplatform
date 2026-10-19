using System.Text;
using BenchmarkDotNet.Attributes;

namespace Bit.Bcrdt.Benchmarks;

/// <summary>
/// Measures the wire format: turning a document into bytes, turning bytes back into a document, and
/// summarizing what a replica knows.
/// </summary>
/// <remarks>
/// <para>
/// Encoding is on the critical path of every interaction that leaves the process. It runs on every
/// local commit to produce the broadcast delta, on every sync handshake to answer a peer's state
/// vector, and on every persistence append. Decoding runs whenever anything arrives. A state vector
/// is encoded and decoded on every reconnect and, on some transports, on every heartbeat, so it is
/// measured separately even though it is tiny.
/// </para>
/// <para>
/// Three budgets from the Bcrdt README's Performance section are defended here: <i>encode a
/// 100 000-character document</i> in under 25 ms and in under 1.2× its raw UTF-8 size, <i>apply a
/// 100 000-character update from a peer</i> in under 60 ms, and <i>state vector encode/decode with
/// 100 clients</i> in under 10 µs. The size half of the encoding budget is not a timing, so it is
/// reported separately by <see cref="GlobalCleanup"/> rather than pressed into the results table.
/// </para>
/// <para>
/// Note that the state vector benchmarks take their client count from an argument and ignore
/// <see cref="DocumentSize"/>, because a state vector's size depends on how many clients have ever
/// edited a document and not at all on what they wrote. BenchmarkDotNet applies a class's parameters
/// to every benchmark in it, so those four rows appear once per document size with identical
/// numbers; filter with <c>--filter "*BcrdtStateVector*"</c> to run them on their own.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[InvocationCount(1, unrollFactor: 1)]
public class SerializationBenchmarks
{
    private const string RootName = "body";

    /// <summary>How many times the sub-microsecond benchmarks repeat their operation per invocation.</summary>
    private const int Repeats = 1_000;

    /// <summary>
    /// How many times the whole-document encoding repeats per invocation. Smaller than
    /// <see cref="Repeats"/> because at the largest size a single encoding is far from
    /// sub-microsecond.
    /// </summary>
    private const int DocumentRepeats = 100;

    private BcrdtDocument _document = null!;
    private byte[] _encodedDocument = null!;
    private int _encodedSize;
    private int _rawUtf8Size;

    private readonly Dictionary<int, BcrdtStateVector> _stateVectors = [];
    private readonly Dictionary<int, byte[]> _encodedStateVectors = [];

    private BcrdtDocument _emptyTarget = null!;

    /// <summary>The document size in UTF-16 characters.</summary>
    [Params(1_000, 10_000, 100_000)]
    public int DocumentSize;

    /// <summary>Builds the document to encode, its encoded form, and the state vector fixtures.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        _document.GetText(RootName).Insert(0, new string('a', DocumentSize));

        _encodedDocument = _document.EncodeStateAsUpdate();
        _encodedSize = _encodedDocument.Length;
        _rawUtf8Size = Encoding.UTF8.GetByteCount(_document.GetText(RootName).ToString());

        foreach (int clientCount in (int[])[10, 100])
        {
            var vector = new BcrdtStateVector(clientCount);
            for (int i = 0; i < clientCount; i++)
            {
                // Realistically wide client identifiers and realistically long histories, so the
                // varint lengths are what a live document would actually produce rather than the
                // single bytes that small test values encode to.
                vector.Set(0xC0FFEE_0000UL + (ulong)i * 7919UL, 1_000_000L + i * 37L);
            }
            _stateVectors[clientCount] = vector;
            _encodedStateVectors[clientCount] = vector.Encode();
        }
    }

    /// <summary>Provides a fresh, empty document for the decoding benchmark.</summary>
    /// <remarks>
    /// <see cref="ApplyUpdateIntoEmptyDocument"/> is destructive: once the update has been applied,
    /// applying it again takes the idempotent fast path and measures duplicate detection instead of
    /// decoding. A new document per iteration is the only honest fixture, and the class runs one
    /// invocation per iteration so that it lasts exactly as long as it needs to.
    /// </remarks>
    [IterationSetup]
    public void IterationSetup()
    {
        _emptyTarget?.Dispose();

        // A peer receiving its first sync has a store and a sync provider subscribed, so it
        // re-encodes what it integrates. See UpdateSink.
        _emptyTarget = UpdateSink.Attach(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 }));
    }

    /// <summary>Releases the fixtures and reports the encoded payload sizes.</summary>
    /// <remarks>
    /// The encoding budget has a size half as well as a time half - under 1.2× the raw UTF-8 bytes -
    /// and a ratio is not a measurement BenchmarkDotNet has a column for. Writing it to the log from
    /// here keeps it alongside the timings without inventing a fake benchmark to carry it.
    /// BenchmarkDotNet runs each benchmark case in its own process, so this line appears once per
    /// case rather than once per document size; that is the right place for it, immediately after
    /// the output of the case it describes.
    /// </remarks>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        Console.WriteLine(
            $"// payload size @ {DocumentSize:N0} chars: encoded {_encodedSize:N0} B, " +
            $"raw UTF-8 {_rawUtf8Size:N0} B, ratio {(double)_encodedSize / _rawUtf8Size:F3}x " +
            $"(budget < 1.200x); state vector 10 clients {_encodedStateVectors[10].Length:N0} B, " +
            $"100 clients {_encodedStateVectors[100].Length:N0} B");

        _document?.Dispose();
        _emptyTarget?.Dispose();
    }

    /// <summary>Encodes the whole document as an update for a peer that has nothing.</summary>
    /// <remarks>
    /// The sync handshake's <c>SyncStep2</c> for a brand-new participant, and the shape persistence
    /// compaction writes. Read-only, so no reset is needed - and at the smaller sizes a single
    /// encoding is a few microseconds, too short to time once per iteration, so it is repeated and
    /// reported per encoding.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = DocumentRepeats, Description = "EncodeStateAsUpdate for the whole document")]
    public int EncodeWholeDocument()
    {
        BcrdtDocument document = _document;
        int total = 0;
        for (int i = 0; i < DocumentRepeats; i++) total += document.EncodeStateAsUpdate().Length;
        return total;
    }

    /// <summary>Decodes and integrates that same payload into an empty document.</summary>
    /// <remarks>
    /// The receiving half of a first sync, and the same code path that loading a document from
    /// storage takes - the wire format section of the Bcrdt README makes them deliberately identical.
    /// </remarks>
    [Benchmark(Description = "ApplyUpdate of that payload into an empty document")]
    public int ApplyUpdateIntoEmptyDocument()
    {
        _emptyTarget.ApplyUpdate(_encodedDocument);
        return _emptyTarget.GetText(RootName).Count;
    }

    /// <summary>Summarizes what the document holds.</summary>
    /// <remarks>
    /// Called on every reconnect and on every heartbeat that carries a <c>SyncStep1</c>, so it needs
    /// to be cheap enough to ignore. It walks one entry per client, not per block.
    /// </remarks>
    [Benchmark(OperationsPerInvoke = Repeats, Description = "GetStateVector")]
    public int GetStateVector()
    {
        int total = 0;
        for (int i = 0; i < Repeats; i++) total += _document.GetStateVector().Count;
        return total;
    }

    /// <summary>Serializes a state vector for the given number of clients.</summary>
    [Benchmark(OperationsPerInvoke = Repeats, Description = "BcrdtStateVector.Encode")]
    [Arguments(10)]
    [Arguments(100)]
    public int EncodeStateVector(int clientCount)
    {
        BcrdtStateVector vector = _stateVectors[clientCount];
        int total = 0;
        for (int i = 0; i < Repeats; i++) total += vector.Encode().Length;
        return total;
    }

    /// <summary>Deserializes a state vector for the given number of clients.</summary>
    [Benchmark(OperationsPerInvoke = Repeats, Description = "BcrdtStateVector.Decode")]
    [Arguments(10)]
    [Arguments(100)]
    public int DecodeStateVector(int clientCount)
    {
        byte[] encoded = _encodedStateVectors[clientCount];
        int total = 0;
        for (int i = 0; i < Repeats; i++) total += BcrdtStateVector.Decode(encoded).Count;
        return total;
    }
}
