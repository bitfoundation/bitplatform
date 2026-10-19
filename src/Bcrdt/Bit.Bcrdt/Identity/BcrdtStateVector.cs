using System.Collections;

namespace Bit.Bcrdt;

/// <summary>
/// A complete, compact summary of everything a replica has seen.
/// </summary>
/// <remarks>
/// <para>
/// A state vector maps each known client to the clock the replica expects that client to produce
/// next. It is the only thing a peer needs in order to compute the exact delta another peer is
/// missing, which is what makes synchronization incremental rather than whole-document - a client
/// returning from a week offline sends a few dozen bytes and receives precisely the blocks it lacks.
/// </para>
/// <para>
/// Size is proportional to the number of clients that have ever edited the document, not to its
/// content, so it stays small even for documents with millions of operations.
/// </para>
/// </remarks>
public sealed class BcrdtStateVector : IEnumerable<KeyValuePair<ulong, long>>
{
    private readonly Dictionary<ulong, long> _clocks;

    /// <summary>Creates an empty state vector, meaning "I have seen nothing".</summary>
    public BcrdtStateVector() => _clocks = [];

    /// <summary>Creates a state vector with a capacity hint.</summary>
    public BcrdtStateVector(int capacity) => _clocks = new Dictionary<ulong, long>(capacity);

    private BcrdtStateVector(Dictionary<ulong, long> clocks) => _clocks = clocks;

    /// <summary>Number of distinct clients described.</summary>
    public int Count => _clocks.Count;

    /// <summary>Whether the replica has seen nothing at all.</summary>
    public bool IsEmpty => _clocks.Count == 0;

    /// <summary>The clients described by this vector.</summary>
    public IEnumerable<ulong> Clients => _clocks.Keys;

    /// <summary>
    /// The next clock expected from <paramref name="client"/>, or zero if that client is unknown.
    /// </summary>
    public long Get(ulong client) => _clocks.TryGetValue(client, out long clock) ? clock : 0;

    /// <summary>Sets the next expected clock for a client.</summary>
    public void Set(ulong client, long clock) => _clocks[client] = clock;

    /// <summary>
    /// Whether the client appears at all, as distinct from appearing with a clock of zero.
    /// </summary>
    /// <remarks>
    /// The distinction matters when a vector records what is <i>missing</i> rather than what is
    /// held: there, a stored zero means "I need everything from this client", which is very
    /// different from "I have never heard of this client".
    /// </remarks>
    public bool ContainsClient(ulong client) => _clocks.ContainsKey(client);

    /// <summary>Advances a client's clock, never moving it backwards.</summary>
    public void Max(ulong client, long clock)
    {
        if (!_clocks.TryGetValue(client, out long existing) || clock > existing)
        {
            _clocks[client] = clock;
        }
    }

    /// <summary>Whether this replica already has the content addressed by <paramref name="id"/>.</summary>
    /// <remarks>
    /// This single comparison is the engine's duplicate detector. Re-delivering an update the
    /// replica already applied costs one dictionary lookup per block and mutates nothing, which is
    /// why the protocol can be careless about at-least-once delivery.
    /// </remarks>
    public bool Covers(BcrdtItemId id) => Get(id.Client) > id.Clock;

    /// <summary>Merges another vector in, keeping the higher clock for each client.</summary>
    public void MergeMax(BcrdtStateVector other)
    {
        foreach ((ulong client, long clock) in other._clocks)
        {
            Max(client, clock);
        }
    }

    /// <summary>
    /// Whether this vector describes at least as much as <paramref name="other"/> for every client.
    /// </summary>
    /// <remarks>
    /// If <c>a.IsSupersetOf(b)</c> then a peer holding <c>b</c> needs a delta from the holder of
    /// <c>a</c>, and not the other way round. If neither is a superset of the other, the replicas
    /// have diverged concurrently and both directions are required.
    /// </remarks>
    public bool IsSupersetOf(BcrdtStateVector other)
    {
        foreach ((ulong client, long clock) in other._clocks)
        {
            if (Get(client) < clock) return false;
        }
        return true;
    }

    /// <summary>Whether the two vectors describe exactly the same knowledge.</summary>
    public bool StructurallyEquals(BcrdtStateVector other)
    {
        if (this == other) return true;
        foreach ((ulong client, long clock) in _clocks)
        {
            if (clock != 0 && other.Get(client) != clock) return false;
        }
        foreach ((ulong client, long clock) in other._clocks)
        {
            if (clock != 0 && Get(client) != clock) return false;
        }
        return true;
    }

    /// <summary>Creates an independent copy.</summary>
    public BcrdtStateVector Clone() => new(new Dictionary<ulong, long>(_clocks));

    /// <summary>Writes the vector as a varint-encoded client/clock table.</summary>
    public void Write(ref BcrdtBufferWriter writer)
    {
        writer.WriteVarUInt((ulong)_clocks.Count);
        foreach ((ulong client, long clock) in _clocks)
        {
            writer.WriteVarUInt(client);
            writer.WriteVarUInt((ulong)clock);
        }
    }

    /// <summary>Reads a vector written by <see cref="Write"/>.</summary>
    public static BcrdtStateVector Read(ref BcrdtBufferReader reader)
    {
        int count = reader.ValidateCount(reader.ReadVarUInt32(), "state vector entry");
        var vector = new BcrdtStateVector(count);
        for (int i = 0; i < count; i++)
        {
            ulong client = reader.ReadVarUInt();
            ulong clock = reader.ReadVarUInt();
            if (clock > long.MaxValue) throw new BcrdtFormatException("Clock exceeds Int64 range.");
            vector.Set(client, (long)clock);
        }
        return vector;
    }

    /// <summary>Encodes the vector to a standalone byte array.</summary>
    public byte[] Encode()
    {
        var writer = new BcrdtBufferWriter(_clocks.Count * 6 + 4);
        try
        {
            Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Decodes a vector produced by <see cref="Encode"/>.</summary>
    public static BcrdtStateVector Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new BcrdtBufferReader(data);
        return Read(ref reader);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<ulong, long>> GetEnumerator() => _clocks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public override string ToString() =>
        _clocks.Count == 0 ? "<empty>" : string.Join(", ", _clocks.Select(p => $"{p.Key}@{p.Value}"));
}
