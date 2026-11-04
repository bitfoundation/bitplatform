using System.Collections;

namespace Bit.BlazorUI;

public sealed partial class BitPdfDocument
{
    private PartialAttempt? _partialAttempt;
    private IReadOnlyList<BitPdfPage>? _partialPages;
    private int? _partialPageCount;
    private readonly Dictionary<int, BitPdfPage> _partialPageCache = [];
    private int PartialPageCount
    {
        get
        {
            if (_partialPageCount is int count)
            {
                return count;
            }

            if (Catalog.Get("Pages") is not BitPdfDict root
                || root.Get("Count") is not double declared
                || !double.IsFinite(declared)
                || declared < 0
                || declared != Math.Floor(declared)
                || declared > 1_000_000)
            {
                throw new BitPdfRangeFallbackException("The PDF page tree has no usable bounded root Count.");
            }

            _partialPageCount = (int)declared;
            return (int)declared;
        }
    }

    private sealed class PartialPageList(BitPdfDocument document) : IReadOnlyList<BitPdfPage>
    {
        public int Count => document.PartialPageCount;

        public BitPdfPage this[int index] => document.ResolvePartialPage(index);
        public IEnumerator<BitPdfPage> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private BitPdfPage ResolvePartialPage(int index)
    {
        var walk = BeginPartialPageWalk(index);
        if (walk is not null)
        {
            while (!walk.Advance())
            {
            }
        }

        return _partialPageCache[index];
    }

    internal PartialPageWalk? BeginPartialPageWalk(int index)
    {
        _xref.ThrowIfSourceDisposed();
        if ((uint)index >= (uint)PartialPageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _partialPageCache.ContainsKey(index) ? null : new(this, index, (BitPdfDict)Catalog.Get("Pages")!);
    }

    // Each advance finishes one node or sibling count before changing this state.
    // The async viewer can commit that step and release its raw-byte pins; a distant
    // lookup need not retain every preceding page dictionary's raw chunks at once.
    internal sealed class PartialPageWalk(BitPdfDocument document, int index, BitPdfDict root)
    {
        private BitPdfDict _node = root;
        private readonly int _index = index;
        private InheritedAttributes _inherited;
        private int _remaining = index, _depth, _nextChild;
        private List<object?>? _kids;
        private readonly HashSet<BitPdfDict> _visited = new(ReferenceEqualityComparer.Instance);
        private bool _complete;
        private int _prefetchedUntil;
        internal List<(int Offset, int Length)> TakeMetadataRanges(int chunkSize, int maxBytes)
        {
            var ranges = new List<(int Offset, int Length)>();
            if (_kids is null || _nextChild < _prefetchedUntil)
            {
                return ranges;
            }

            // Page one normally needs one child, not an entire lookahead batch.
            // Empty subtrees can require further batches; their Count is still read.
            int limit = Math.Min(16, _remaining + 1);
            int budget = Math.Max(chunkSize, maxBytes);
            int next = _nextChild;
            for (; next < _kids.Count && next - _nextChild < limit; next++)
            {
                var range = document._xref.MetadataRange(_kids[next], chunkSize);
                if (range is null)
                {
                    continue;
                }

                // Let the next batch combine a nearby trailing header with its successors,
                // rather than spending a separate request on one chunk at this batch's limit.
                if (ranges.Any(r => range.Value.Offset >= (long)r.Offset + r.Length
                    && range.Value.Offset <= (long)r.Offset + r.Length + chunkSize
                    && (long)range.Value.Offset + range.Value.Length - r.Offset > budget))
                {
                    break;
                }

                var candidate = Merge(ranges.Append(range.Value));
                if (candidate.Sum(r => r.Length) > budget)
                {
                    break;
                }

                ranges = candidate;
            }

            _prefetchedUntil = next;
            return ranges;
            List<(int Offset, int Length)> Merge(IEnumerable<(int Offset, int Length)> windows)
            {
                var merged = new List<(int Offset, int Length)>();
                foreach (var window in windows.OrderBy(w => w.Offset))
                {
                    if (merged.Count > 0)
                    {
                        var last = merged[^1];
                        long end = Math.Max((long)last.Offset + last.Length, (long)window.Offset + window.Length);
                        // One skipped chunk is cheaper than another round trip on typical HTTP links.
                        if (window.Offset <= (long)last.Offset + last.Length + chunkSize && end - last.Offset <= budget)
                        {
                            merged[^1] = (last.Offset, (int)(end - last.Offset));
                            continue;
                        }
                    }

                    merged.Add(window);
                }

                return merged;
            }
        }

        internal bool Advance()
        {
            document._xref.ThrowIfSourceDisposed();
            if (_complete)
            {
                return true;
            }

            if (_kids is { } kids)
            {
                if (_nextChild >= kids.Count)
                {
                    throw InvalidTree();
                }

                var child = document._xref.FetchIfRef(kids[_nextChild]) as BitPdfDict;
                int count = child is null ? 0 : PartialNodeCount(child);
                if (_remaining < count)
                {
                    _node = child!;
                    _kids = null;
                }
                else
                {
                    _remaining -= count;
                    _nextChild++;
                }

                return false;
            }

            if (_depth > 64 || _visited.Contains(_node))
            {
                throw InvalidTree();
            }

            var inherited = _inherited.With(_node, document._xref);
            if (BitPdfPrimitives.IsName(_node.Get("Type"), "Page"))
            {
                if (_remaining != 0)
                {
                    throw InvalidTree();
                }

                var page = new BitPdfPage(document._xref, _node, _index + 1, inherited.MediaBox ?? [0, 0, 612, 792], inherited.Resources, inherited.Rotate ?? 0, inherited.CropBox);
                document._partialAttempt?.TrackPage(_index);
                document._partialPageCache[_index] = page;
                document._partialAttempt?.TrackIndex(_node);
                document._pageIndexByDict[_node] = _index;
                _complete = true;
                return true;
            }

            if (_node.Get("Kids") is not List<object?> children || children.Count == 0)
            {
                throw InvalidTree();
            }

            // All possibly missing reads completed; only now advance the walk.
            _visited.Add(_node);
            _depth++;
            _inherited = inherited;
            _kids = children;
            _nextChild = 0;
            _prefetchedUntil = 0;
            return false;
        }

        private static BitPdfRangeFallbackException InvalidTree() => new("The PDF page tree cannot resolve the requested page safely.");
    }

    private static int PartialNodeCount(BitPdfDict node)
    {
        // The PDF /Count contract permits skipping subtrees during random access.
        // Recounting every leaf would load the entire skipped page tree. Buffered
        // loading remains available for recovering files with incorrect counts.
        if (BitPdfPrimitives.IsName(node.Get("Type"), "Page"))
        {
            return 1;
        }

        if (node.Get("Count") is double count && double.IsFinite(count) && count >= 0 && count <= 1_000_000 && count == Math.Floor(count))
        {
            return (int)count;
        }

        throw new BitPdfRangeFallbackException("The PDF page tree has an invalid subtree Count.");
    }

    private int? ResolvePartialPageIndex(BitPdfDict target)
    {
        if (_pageIndexByDict.TryGetValue(target, out int known))
        {
            return known;
        }

        if (Catalog.Get("Pages") is not BitPdfDict root)
        {
            return null;
        }

        var child = target;
        var visited = new HashSet<BitPdfDict>(ReferenceEqualityComparer.Instance);
        int index = 0;
        for (int depth = 0; depth <= 64 && visited.Add(child); depth++)
        {
            if (ReferenceEquals(child, root))
            {
                if ((uint)index >= (uint)PartialPageCount)
                {
                    return null;
                }

                _partialAttempt?.TrackIndex(target);
                _pageIndexByDict[target] = index;
                return index;
            }

            if (child.Get("Parent") is not BitPdfDict parent || parent.Get("Kids") is not List<object?> kids)
            {
                return null;
            }

            var reference = _xref.FindCachedReference(child);
            int position = kids.FindIndex(k => ReferenceEquals(k, child) || (reference is not null && Equals(k, reference)));
            if (position < 0)
            {
                return null;
            }

            for (int i = 0; i < position; i++)
            {
                if (_xref.FetchIfRef(kids[i]) is BitPdfDict sibling)
                {
                    index = checked(index + PartialNodeCount(sibling));
                }
            }

            child = parent;
        }

        return null;
    }

    internal string? ReadPartialTitle() => (_xref.Trailer?.Get("Info") as BitPdfDict)?.Get("Title") switch
    {
        BitPdfString title => title.AsText(),
        BitPdfName title => title.Value,
        string title => title,
        _ => null,
    };
    internal List<(int Offset, int Length)> CollectPageDependencies(int index, IBitPdfPartialByteSource source)
    {
        var page = Pages[index];
        var pending = new List<(int Offset, int Length)>();
        var todo = new Stack<(object? Value, BitPdfDict? Resources)>();
        Queue(page.Dict.GetRaw("Contents"), page.Resources);
        Queue(page.Resources, page.Resources);
        Queue(page.Dict.GetRaw("Annots"), page.Resources);
        Queue(page.Dict.GetRaw("Group"), page.Resources);
        var visitedRefs = new HashSet<BitPdfRef>();
        var visitedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int examined = 0;
        try
        {
            QueueUsedXObjects(page.GetContentBytes(), page.Resources);
        }
        catch (BitPdfMissingDataException missing)
        {
            pending.Add((missing.Offset, missing.Length));
        }
        catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex))
        {
        }

        // Discovery is only a prefetch optimization. Stop at its work bound and
        // let normal missing-data restarts acquire anything not discovered here.
        while (examined < 65536 && todo.TryPop(out var next))
        {
            try
            {
                var value = next.Value;
                var resources = next.Resources;
                if (value is BitPdfRef reference)
                {
                    if (!visitedRefs.Add(reference))
                    {
                        continue;
                    }

                    value = _xref.Fetch(reference);
                }

                if (value is null || !visitedObjects.Add(value))
                {
                    continue;
                }

                examined++;
                if (value is BitPdfBaseStream stream)
                {
                    bool available = source.IsDataAvailable(stream.Start, stream.Length);
                    if (!available)
                    {
                        pending.Add((stream.Start, stream.Length));
                    }

                    if (available && BitPdfPrimitives.IsName(stream.Dict?.GetRaw("Subtype"), "Form"))
                    {
                        resources = stream.Dict!.Get("Resources") as BitPdfDict ?? resources;
                        try
                        {
                            QueueUsedXObjects(BitPdfStreamDecoder.Decode(stream), resources);
                        }
                        catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex))
                        {
                        }
                    }

                    value = stream.Dict;
                }

                if (value is BitPdfDict dictionary)
                {
                    // Destination links must not pull another page's complete resource graph.
                    if (BitPdfPrimitives.IsName(dictionary.GetRaw("Type"), "Page"))
                    {
                        continue;
                    }

                    // XObject dictionaries may include large, unused images/forms.
                    // Queue only Do operands from the page and referenced forms;
                    // other resources remain available to the renderer on demand.
                    foreach (var pair in dictionary.RawEntries)
                    {
                        if (pair.Key is not ("XObject" or "Parent" or "P" or "Prev" or "Next" or "FS" or "EF" or "AF" or "Metadata"))
                        {
                            Queue(pair.Value, resources);
                        }
                    }
                }
                else if (value is List<object?> array)
                {
                    foreach (var item in array)
                    {
                        Queue(item, resources);
                    }
                }
            }
            catch (BitPdfMissingDataException missing)
            {
                pending.Add((missing.Offset, missing.Length));
            }
        }

        return pending;
        void Queue(object? value, BitPdfDict? resources)
        {
            if (value is BitPdfRef or BitPdfDict or List<object?> or BitPdfBaseStream)
            {
                todo.Push((value, resources));
            }
        }

        void QueueUsedXObjects(byte[] content, BitPdfDict? resources)
        {
            BitPdfDict? objects = null;
            foreach (var op in new BitPdfContentParser(content).Parse())
            {
                if (op.Code != BitPdfOpCode.XObject || op.Operands.Count == 0 || op.Operands[0] is not BitPdfName name)
                {
                    continue;
                }

                objects ??= resources?.Get("XObject") as BitPdfDict;
                Queue(objects?.GetRaw(name.Value), resources);
            }
        }
    }

    internal PartialAttempt BeginPartialAttempt() => new(this);
    internal sealed class PartialAttempt : IDisposable
    {
        private readonly BitPdfDocument _document;
        private readonly BitPdfXRef.PartialAttempt _xref;
        private readonly Dictionary<int, BitPdfPage?> _pages = [];
        private readonly Dictionary<BitPdfDict, int?> _indexes = new(ReferenceEqualityComparer.Instance);
        private readonly IReadOnlyList<BitPdfOutlineItem>? _outline;
        private readonly BitPdfMetadata? _metadata;
        private readonly IReadOnlyList<string>? _labels;
        private readonly IReadOnlyList<BitPdfStructElement>? _structure;
        private readonly IReadOnlyList<BitPdfLayer>? _layers;
        private readonly IReadOnlyList<BitPdfAttachment>? _attachments;
        private readonly IReadOnlyList<BitPdfFormField>? _forms;
        private bool _committed;
        internal PartialAttempt(BitPdfDocument document)
        {
            _document = document;
            _xref = document._xref.BeginPartialAttempt();
            document._partialAttempt = this;
            _outline = document._outline;
            _metadata = document._metadata;
            _labels = document._pageLabels;
            _structure = document._structure;
            _layers = document._layers;
            _attachments = document._attachments;
            _forms = document._formFields;
        }

        internal void TrackPage(int index) => _pages.TryAdd(index, _document._partialPageCache.GetValueOrDefault(index));
        internal void TrackIndex(BitPdfDict dictionary) => _indexes.TryAdd(dictionary, _document._pageIndexByDict.TryGetValue(dictionary, out int index) ? index : null);
        internal void CommitObjects()
        {
            _xref.CommitObjects();
            // These entries are recorded only after a page or its destination
            // index is fully resolved. Keep them too; a later outline miss must
            // not repeat every preceding bookmark's parent/sibling walk.
            _pages.Clear();
            _indexes.Clear();
        }

        internal void Commit()
        {
            _xref.Commit();
            _committed = true;
        }

        public void Dispose()
        {
            _document._partialAttempt = null;
            _xref.Dispose();
            if (_committed)
            {
                return;
            }

            foreach (var pair in _pages)
            {
                if (pair.Value is null)
                {
                    _document._partialPageCache.Remove(pair.Key);
                }
                else
                {
                    _document._partialPageCache[pair.Key] = pair.Value;
                }
            }

            foreach (var pair in _indexes)
            {
                if (pair.Value is not int index)
                {
                    _document._pageIndexByDict.Remove(pair.Key);
                }
                else
                {
                    _document._pageIndexByDict[pair.Key] = index;
                }
            }

            _document._outline = _outline;
            _document._metadata = _metadata;
            _document._pageLabels = _labels;
            _document._structure = _structure;
            _document._layers = _layers;
            _document._attachments = _attachments;
            _document._formFields = _forms;
        }
    }
}
