namespace Bit.Bcrdt;

/// <summary>
/// A named point in a document's history.
/// </summary>
/// <remarks>
/// <para>
/// A snapshot is not a copy of the document. It is a state vector plus a delete set - typically a
/// few hundred bytes regardless of document size - which together say exactly which content existed
/// and which of it was visible at that moment. Reconstructing the document from one is a matter of
/// replaying the blocks it names.
/// </para>
/// <para>
/// This is what version history costs: storing a hundred versions of a large document costs a
/// hundred small records, not a hundred documents. The trade is that the document must retain the
/// tombstones the snapshot refers to, so a document that snapshots must be created with
/// <see cref="BcrdtDocumentOptions.GarbageCollect"/> disabled. Trying to restore from a collected
/// document fails loudly rather than silently returning a wrong answer.
/// </para>
/// </remarks>
public sealed class BcrdtSnapshot
{
    /// <summary>Creates a snapshot from its two components.</summary>
    public BcrdtSnapshot(BcrdtStateVector stateVector, BcrdtDeleteSet deleteSet)
    {
        StateVector = stateVector;
        DeleteSet = deleteSet;
    }

    /// <summary>Which content existed at the captured moment.</summary>
    public BcrdtStateVector StateVector { get; }

    /// <summary>Which of that content had been deleted.</summary>
    public BcrdtDeleteSet DeleteSet { get; }

    /// <summary>Captures the document's current state.</summary>
    public static BcrdtSnapshot Capture(BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        return new BcrdtSnapshot(doc.Store.GetStateVector(), BcrdtDeleteSet.FromStore(doc.Store));
    }

    /// <summary>Whether a specific unit of content was visible at the captured moment.</summary>
    public bool IsVisible(BcrdtItemId id) => StateVector.Covers(id) && !DeleteSet.IsDeleted(id);

    /// <summary>
    /// Encodes the document as it was at the captured moment.
    /// </summary>
    /// <remarks>
    /// Produces an ordinary update, so restoring history and syncing with a peer share one code
    /// path. Blocks are truncated at the snapshot's clock boundaries, which means anything written
    /// after the snapshot is simply absent rather than deleted.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The source document reclaims deleted content, so the history the snapshot names no longer exists.
    /// </exception>
    public static byte[] EncodeStateAsUpdate(BcrdtDocument doc, BcrdtSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (doc.GarbageCollect)
        {
            throw new InvalidOperationException(
                "Snapshots require the source document to retain deleted content. Create it with " +
                "BcrdtDocumentOptions.GarbageCollect set to false.");
        }

        return doc.Transact(transaction =>
        {
            List<(ulong Client, long FromClock, List<(BcrdtAbstractStruct Block, int Offset)> Blocks)> groups = [];

            foreach ((ulong client, long clock) in snapshot.StateVector)
            {
                if (clock <= 0) continue;

                // Make the snapshot's boundary a real block boundary so the slice is exact.
                if (clock < doc.Store.GetState(client))
                {
                    BcrdtStructStore.GetStructCleanStart(transaction, new BcrdtItemId(client, clock));
                }

                List<BcrdtAbstractStruct>? structs = doc.Store.GetClientStructs(client);
                if (structs is null || structs.Count == 0) continue;

                int lastIndex = BcrdtStructStore.FindIndex(structs, clock - 1);
                List<(BcrdtAbstractStruct, int)> blocks = new(lastIndex + 1);
                for (int i = 0; i <= lastIndex; i++) blocks.Add((structs[i], 0));

                groups.Add((client, structs[0].Id.Clock, blocks));
            }

            groups.Sort(static (left, right) => right.Client.CompareTo(left.Client));

            var writer = new BcrdtBufferWriter(1024);
            try
            {
                BcrdtUpdateCodec.WriteBlockGroups(ref writer, groups);
                snapshot.DeleteSet.Write(ref writer);
                return writer.ToArray();
            }
            finally
            {
                writer.Dispose();
            }
        });
    }

    /// <summary>Builds a new document holding the state the snapshot names.</summary>
    public static BcrdtDocument RestoreDocument(BcrdtDocument doc, BcrdtSnapshot snapshot, BcrdtDocumentOptions? options = null)
    {
        byte[] update = EncodeStateAsUpdate(doc, snapshot);
        var restored = new BcrdtDocument(options ?? new BcrdtDocumentOptions { GarbageCollect = false });
        restored.ApplyUpdate(update, origin: "snapshot");
        return restored;
    }

    /// <summary>Encodes the snapshot to a compact byte array.</summary>
    public byte[] Encode()
    {
        var writer = new BcrdtBufferWriter(128);
        try
        {
            DeleteSet.Write(ref writer);
            StateVector.Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Decodes a snapshot produced by <see cref="Encode"/>.</summary>
    public static BcrdtSnapshot Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new BcrdtBufferReader(data);
        BcrdtDeleteSet deleteSet = BcrdtDeleteSet.Read(ref reader);
        BcrdtStateVector stateVector = BcrdtStateVector.Read(ref reader);
        return new BcrdtSnapshot(stateVector, deleteSet);
    }
}
