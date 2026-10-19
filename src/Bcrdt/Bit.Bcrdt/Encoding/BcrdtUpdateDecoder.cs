namespace Bit.Bcrdt;

/// <summary>Integrates wire updates into a document.</summary>
public static class BcrdtUpdateDecoder
{
    private sealed record DeferredBlocks(BcrdtStateVector Missing, byte[] Update);

    /// <summary>
    /// Merges an update into a document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operation is idempotent, commutative and tolerant of partial information. Applying the
    /// same update twice changes nothing; applying updates out of order converges to the same state
    /// as applying them in order; and blocks whose causal dependencies have not arrived are held
    /// aside and integrated automatically once they do, rather than being dropped or rejected.
    /// </para>
    /// <para>
    /// Those three properties together are what let the transport layer be unreliable. It may
    /// duplicate, reorder, delay or lose messages, and the document still converges - the only
    /// thing it must eventually do is deliver each message at least once.
    /// </para>
    /// </remarks>
    /// <exception cref="BcrdtFormatException">
    /// The payload is not a well-formed update. The whole payload is decoded before anything is
    /// integrated, so a rejected update leaves the document exactly as it was.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A well-formed block could not be integrated. That block and the later blocks from the same
    /// author were skipped; everything else in the update, including its deletions, was applied
    /// before this was thrown, and the inner exception says what went wrong.
    /// </exception>
    public static void ApplyUpdate(BcrdtDocument doc, ReadOnlyMemory<byte> update, object? origin = null)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var reader = new BcrdtBufferReader(update);
        Dictionary<ulong, BcrdtClientBlocks> groups = BcrdtUpdateCodec.ReadClientBlocks(ref reader);
        BcrdtDeleteSet deleteSet = BcrdtDeleteSet.Read(ref reader);

        Exception? integrationFailure = null;

        doc.Transact(transaction =>
        {
            // Whatever the enclosing transaction was, the content arriving here was authored
            // elsewhere. The index caches rely on knowing that: a remote change can land anywhere, so
            // positions cached on the assumption of purely local edits are void. Local is left alone,
            // because an update applied inside a local transaction does not make that transaction's
            // own edits any less local.
            transaction.IntegratedRemoteContent = true;

            BcrdtStructStore store = doc.Store;
            BcrdtUpdateCodec.ResolveRoots(groups, doc);

            DeferredBlocks? deferred = IntegrateBlocks(transaction, store, groups, out integrationFailure);
            bool retryDeferred = MergeDeferred(store, deferred);

            BcrdtDeleteSet? unapplied = ApplyDeleteSet(transaction, deleteSet);

            if (store.PendingDeleteSet is { } pendingDeletes)
            {
                store.PendingDeleteSet = null;
                BcrdtDeleteSet? stillUnapplied = ApplyDeleteSet(transaction, pendingDeletes);
                store.PendingDeleteSet = CombineUnapplied(unapplied, stillUnapplied);
            }
            else
            {
                store.PendingDeleteSet = unapplied;
            }

            if (retryDeferred)
            {
                byte[] pending = store.PendingUpdate!;
                store.PendingUpdate = null;
                store.PendingMissing = null;
                ApplyUpdate(doc, pending, origin);
            }
        }, origin, local: false);

        if (integrationFailure is not null)
        {
            throw new InvalidOperationException(
                "A block in the update could not be integrated. It was skipped, together with the later " +
                "blocks from the same author, and the rest of the update was applied.",
                integrationFailure);
        }
    }

    /// <summary>
    /// Folds newly deferred blocks into whatever was already waiting.
    /// </summary>
    /// <returns>Whether the wait is over and the deferred blocks should be retried now.</returns>
    private static bool MergeDeferred(BcrdtStructStore store, DeferredBlocks? deferred)
    {
        if (store.PendingMissing is not { } pendingMissing)
        {
            if (deferred is not null)
            {
                store.PendingUpdate = deferred.Update;
                store.PendingMissing = deferred.Missing;
            }
            return false;
        }

        bool retry = false;
        foreach ((ulong client, long clock) in pendingMissing)
        {
            if (clock < store.GetState(client))
            {
                retry = true;
                break;
            }
        }

        if (deferred is not null)
        {
            foreach ((ulong client, long clock) in deferred.Missing)
            {
                if (!pendingMissing.ContainsClient(client) || pendingMissing.Get(client) > clock)
                {
                    pendingMissing.Set(client, clock);
                }
            }
            store.PendingUpdate = BcrdtUpdateOperations.Merge(store.PendingUpdate!, deferred.Update);
        }

        return retry;
    }

    private static BcrdtDeleteSet? CombineUnapplied(BcrdtDeleteSet? left, BcrdtDeleteSet? right)
    {
        if (left is null) return right;
        if (right is null) return left;
        return BcrdtDeleteSet.Merge([left, right]);
    }

    /// <summary>
    /// Places every block from an update, deferring the ones whose dependencies are still missing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Blocks arrive grouped by author but depend on each other across groups, so a straight pass
    /// through them would stall constantly. Instead, when a block turns out to need content from
    /// another author, the walk jumps to that author's blocks in the same update and works its way
    /// back - a depth-first traversal of the dependency graph using an explicit stack.
    /// </para>
    /// <para>
    /// That is what keeps applying a large multi-client update linear. A retry-until-stable loop
    /// would be quadratic in the number of authors, which is exactly the situation a busy document
    /// produces.
    /// </para>
    /// <para>
    /// A block that throws while being placed costs its author's remaining blocks in this update -
    /// they continue it, so none of them can be placed either - but not the rest of the update.
    /// Abandoning the walk there would also drop the deletions and the deferred blocks that follow,
    /// leaving the replica wrong until some later full resync. The first failure is returned through
    /// <paramref name="failure"/> for the caller to report once everything else has been applied.
    /// </para>
    /// </remarks>
    private static DeferredBlocks? IntegrateBlocks(
        BcrdtTransaction transaction,
        BcrdtStructStore store,
        Dictionary<ulong, BcrdtClientBlocks> groups,
        out Exception? failure)
    {
        failure = null;
        if (groups.Count == 0) return null;

        List<ulong> remainingClients = [.. groups.Keys];
        remainingClients.Sort();

        BcrdtClientBlocks? NextGroup()
        {
            while (remainingClients.Count > 0)
            {
                if (groups.TryGetValue(remainingClients[^1], out BcrdtClientBlocks? candidate) && !candidate.Exhausted)
                {
                    return candidate;
                }
                remainingClients.RemoveAt(remainingClients.Count - 1);
            }
            return null;
        }

        BcrdtClientBlocks? currentGroup = NextGroup();
        if (currentGroup is null) return null;

        List<BcrdtAbstractStruct> stack = [];
        Dictionary<ulong, List<BcrdtAbstractStruct>> deferred = [];
        var missing = new BcrdtStateVector();
        Dictionary<ulong, long> clockCache = [];

        void NoteMissing(ulong client, long clock)
        {
            if (!missing.ContainsClient(client) || missing.Get(client) > clock) missing.Set(client, clock);
        }

        void DeferStack()
        {
            foreach (BcrdtAbstractStruct blocked in stack)
            {
                ulong client = blocked.Id.Client;
                if (groups.TryGetValue(client, out BcrdtClientBlocks? group))
                {
                    // Step back over the block we took but could not place, then defer it and
                    // everything behind it - those depend on it, so they cannot be placed either.
                    group.Position--;
                    deferred[client] = group.Blocks.GetRange(group.Position, group.Blocks.Count - group.Position);
                    groups.Remove(client);
                    group.Blocks.Clear();
                    group.Position = 0;
                }
                else
                {
                    deferred[client] = [blocked];
                }
                remainingClients.Remove(client);
            }
            stack.Clear();
        }

        void Discard(ulong client)
        {
            if (groups.Remove(client, out BcrdtClientBlocks? group))
            {
                group.Blocks.Clear();
                group.Position = 0;
            }
            remainingClients.Remove(client);
            clockCache.Remove(client);
        }

        BcrdtAbstractStruct head = currentGroup.Blocks[currentGroup.Position++];

        while (true)
        {
            try
            {
                if (head is not BcrdtSkip)
                {
                    if (!clockCache.TryGetValue(head.Id.Client, out long localClock))
                    {
                        localClock = store.GetState(head.Id.Client);
                        clockCache[head.Id.Client] = localClock;
                    }

                    long offset = localClock - head.Id.Clock;

                    if (offset < 0)
                    {
                        // Earlier content from this same author is missing, which no amount of
                        // rearranging within this update can fix.
                        stack.Add(head);
                        NoteMissing(head.Id.Client, head.Id.Clock - 1);
                        DeferStack();
                    }
                    else
                    {
                        ulong? dependency = head is BcrdtItem item ? item.GetMissing(transaction, store) : null;

                        if (dependency is { } blockedOn)
                        {
                            stack.Add(head);
                            if (groups.TryGetValue(blockedOn, out BcrdtClientBlocks? provider) && !provider.Exhausted)
                            {
                                head = provider.Blocks[provider.Position++];
                                continue;
                            }

                            NoteMissing(blockedOn, store.GetState(blockedOn));
                            DeferStack();
                        }
                        else if (offset == 0 || offset < head.Length)
                        {
                            head.Integrate(transaction, (int)offset);
                            clockCache[head.Id.Client] = head.Id.Clock + head.Length;

                            // Content stamped with our own identity that we did not write ourselves.
                            if (head.Id.Client == transaction.Doc.ClientId) transaction.ReceivedOwnClientContent = true;
                        }

                        // The remaining case - offset >= length - means we already have every unit
                        // this block carries. Skipping it silently is the duplicate detector.
                    }
                }
            }
            catch (Exception ex)
            {
                failure ??= ex;
                Discard(head.Id.Client);
            }

            if (stack.Count > 0)
            {
                head = stack[^1];
                stack.RemoveAt(stack.Count - 1);
            }
            else if (currentGroup is not null && !currentGroup.Exhausted)
            {
                head = currentGroup.Blocks[currentGroup.Position++];
            }
            else
            {
                currentGroup = NextGroup();
                if (currentGroup is null) break;
                head = currentGroup.Blocks[currentGroup.Position++];
            }
        }

        return deferred.Count > 0 ? new DeferredBlocks(missing, EncodeDeferred(deferred)) : null;
    }

    private static byte[] EncodeDeferred(Dictionary<ulong, List<BcrdtAbstractStruct>> deferred)
    {
        List<(ulong Client, long FromClock, List<(BcrdtAbstractStruct Block, int Offset)> Blocks)> groups = [];
        foreach ((ulong client, List<BcrdtAbstractStruct> blocks) in deferred)
        {
            if (blocks.Count == 0) continue;
            groups.Add((client, blocks[0].Id.Clock, [.. blocks.Select(block => (block, 0))]));
        }

        var writer = new BcrdtBufferWriter(256);
        try
        {
            BcrdtUpdateCodec.WriteBlockGroups(ref writer, groups);
            new BcrdtDeleteSet().Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Applies the deletions in an update, returning any that referenced content we do not have.
    /// </summary>
    private static BcrdtDeleteSet? ApplyDeleteSet(BcrdtTransaction transaction, BcrdtDeleteSet deleteSet)
    {
        var unapplied = new BcrdtDeleteSet();
        BcrdtStructStore store = transaction.Doc.Store;

        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in deleteSet.Clients)
        {
            long state = store.GetState(client);
            List<BcrdtAbstractStruct>? structs = store.GetClientStructs(client);

            foreach (BcrdtDeleteRange range in ranges)
            {
                if (structs is null || range.Clock >= state)
                {
                    unapplied.Add(client, range.Clock, range.Length);
                    continue;
                }

                // The tail of the range covers content we have not received yet.
                if (state < range.End) unapplied.Add(client, state, range.End - state);

                int index = BcrdtStructStore.FindIndex(structs, range.Clock);
                BcrdtAbstractStruct block = structs[index];

                if (!block.Deleted && block.Id.Clock < range.Clock && block is BcrdtItem head)
                {
                    structs.Insert(index + 1, BcrdtStructStore.Split(transaction, head, (int)(range.Clock - head.Id.Clock)));
                    index++;
                }

                while (index < structs.Count)
                {
                    block = structs[index++];
                    if (block.Id.Clock >= range.End) break;
                    if (block.Deleted || block is not BcrdtItem target) continue;

                    if (range.End < target.Id.Clock + target.Length)
                    {
                        structs.Insert(index, BcrdtStructStore.Split(transaction, target, (int)(range.End - target.Id.Clock)));
                    }
                    target.Delete(transaction);
                }
            }
        }

        unapplied.SortAndMerge();
        return unapplied.IsEmpty ? null : unapplied;
    }
}
