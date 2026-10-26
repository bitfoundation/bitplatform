using System.Collections;

namespace Bit.BlazorUI;

public sealed partial class BitPdfDocument
{
    private IReadOnlyList<BitPdfPage>? _partialPages;
    private int? _partialPageCount;
    private readonly Dictionary<int, BitPdfPage> _partialPageCache = [];

    private int PartialPageCount
    {
        get
        {
            if (_partialPageCount is int count) return count;
            if (Catalog.Get("Pages") is not BitPdfDict root || root.Get("Count") is not double declared
                || !double.IsFinite(declared) || declared < 0 || declared != Math.Floor(declared) || declared > 1_000_000)
                throw new BitPdfRangeFallbackException("The PDF page tree has no usable bounded root Count.");
            _partialPageCount = (int)declared;
            return (int)declared;
        }
    }

    private sealed class PartialPageList(BitPdfDocument document) : IReadOnlyList<BitPdfPage>
    {
        public int Count => document.PartialPageCount;
        public BitPdfPage this[int index] => document.ResolvePartialPage(index);
        public IEnumerator<BitPdfPage> GetEnumerator()
        { for (int i = 0; i < Count; i++) yield return this[i]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private BitPdfPage ResolvePartialPage(int index)
    {
        var walk = BeginPartialPageWalk(index);
        if (walk is not null) while (!walk.Advance()) { }
        return _partialPageCache[index];
    }

    internal PartialPageWalk? BeginPartialPageWalk(int index)
    {
        _xref.ThrowIfSourceDisposed();
        if ((uint)index >= (uint)PartialPageCount) throw new ArgumentOutOfRangeException(nameof(index));
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

        internal bool Advance()
        {
            document._xref.ThrowIfSourceDisposed();
            if (_complete) return true;
            if (_kids is { } kids)
            {
                if (_nextChild >= kids.Count) throw InvalidTree();
                var child = document._xref.FetchIfRef(kids[_nextChild]) as BitPdfDict;
                int count = child is null ? 0 : PartialNodeCount(child);
                if (_remaining < count) { _node = child!; _kids = null; }
                else { _remaining -= count; _nextChild++; }
                return false;
            }
            if (_depth > 64 || _visited.Contains(_node)) throw InvalidTree();
            var inherited = _inherited.With(_node, document._xref);
            if (BitPdfPrimitives.IsName(_node.Get("Type"), "Page"))
            {
                if (_remaining != 0) throw InvalidTree();
                var page = new BitPdfPage(document._xref, _node, _index + 1,
                    inherited.MediaBox ?? [0, 0, 612, 792], inherited.Resources,
                    inherited.Rotate ?? 0, inherited.CropBox);
                document._partialPageCache[_index] = page;
                document._pageIndexByDict[_node] = _index;
                _complete = true;
                return true;
            }
            if (_node.Get("Kids") is not List<object?> children || children.Count == 0) throw InvalidTree();
            // All possibly missing reads completed; only now advance the walk.
            _visited.Add(_node); _depth++;
            _inherited = inherited; _kids = children; _nextChild = 0;
            return false;
        }

        private static BitPdfRangeFallbackException InvalidTree()
            => new("The PDF page tree cannot resolve the requested page safely.");
    }

    private static int PartialNodeCount(BitPdfDict node)
    {
        if (BitPdfPrimitives.IsName(node.Get("Type"), "Page")) return 1;
        if (node.Get("Count") is double count && double.IsFinite(count) && count >= 0
            && count <= 1_000_000 && count == Math.Floor(count)) return (int)count;
        throw new BitPdfRangeFallbackException("The PDF page tree has an invalid subtree Count.");
    }

    private int? ResolvePartialPageIndex(BitPdfDict target)
    {
        if (_pageIndexByDict.TryGetValue(target, out int known)) return known;
        if (Catalog.Get("Pages") is not BitPdfDict root) return null;
        var child = target;
        var visited = new HashSet<BitPdfDict>(ReferenceEqualityComparer.Instance);
        int index = 0;
        for (int depth = 0; depth <= 64 && visited.Add(child); depth++)
        {
            if (ReferenceEquals(child, root))
            {
                if ((uint)index >= (uint)PartialPageCount) return null;
                _pageIndexByDict[target] = index;
                return index;
            }
            if (child.Get("Parent") is not BitPdfDict parent || parent.Get("Kids") is not List<object?> kids) return null;
            var reference = _xref.FindCachedReference(child);
            int position = kids.FindIndex(k => ReferenceEquals(k, child) || (reference is not null && Equals(k, reference)));
            if (position < 0) return null;
            for (int i = 0; i < position; i++)
                if (_xref.FetchIfRef(kids[i]) is BitPdfDict sibling) index = checked(index + PartialNodeCount(sibling));
            child = parent;
        }
        return null;
    }

    internal string? ReadPartialTitle()
        => (_xref.Trailer?.Get("Info") as BitPdfDict)?.Get("Title") switch
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
        var todo = new Stack<object?>();
        todo.Push(page.Dict.GetRaw("Contents")); todo.Push(page.Resources);
        todo.Push(page.Dict.GetRaw("Annots")); todo.Push(page.Dict.GetRaw("Group"));
        var visitedRefs = new HashSet<BitPdfRef>();
        var visitedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int examined = 0;
        while (todo.TryPop(out var value))
        {
            if (++examined > 65536) throw new BitPdfByteSourceIOException("The PDF page dependency graph exceeds the safety limit.");
            try
            {
                if (value is BitPdfRef reference)
                {
                    if (!visitedRefs.Add(reference)) continue;
                    value = _xref.Fetch(reference);
                }
                if (value is null || !visitedObjects.Add(value)) continue;
                if (value is BitPdfBaseStream stream)
                {
                    if (!source.IsDataAvailable(stream.Start, stream.Length)) pending.Add((stream.Start, stream.Length));
                    value = stream.Dict;
                }
                if (value is BitPdfDict dictionary)
                {
                    // Destination links must not pull another page's complete resource graph.
                    if (BitPdfPrimitives.IsName(dictionary.GetRaw("Type"), "Page")) continue;
                    foreach (var pair in dictionary.RawEntries)
                        if (pair.Key is not ("Parent" or "P" or "Prev" or "Next" or "FS" or "EF" or "AF" or "Metadata")) todo.Push(pair.Value);
                }
                else if (value is List<object?> array)
                    foreach (var item in array) todo.Push(item);
            }
            catch (BitPdfMissingDataException missing) { pending.Add((missing.Offset, missing.Length)); }
        }
        return pending;
    }

    internal PartialAttempt BeginPartialAttempt() => new(this);

    internal sealed class PartialAttempt : IDisposable
    {
        private readonly BitPdfDocument _document;
        private readonly BitPdfXRef.PartialAttempt _xref;
        private readonly HashSet<int> _pages;
        private readonly HashSet<BitPdfDict> _indexes;
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
            _document = document; _xref = document._xref.BeginPartialAttempt();
            _pages = document._partialPageCache.Keys.ToHashSet();
            _indexes = document._pageIndexByDict.Keys.ToHashSet<BitPdfDict>(ReferenceEqualityComparer.Instance);
            _outline = document._outline; _metadata = document._metadata; _labels = document._pageLabels;
            _structure = document._structure; _layers = document._layers;
            _attachments = document._attachments; _forms = document._formFields;
        }
        internal void Commit() { _xref.Commit(); _committed = true; }
        public void Dispose()
        {
            _xref.Dispose();
            if (_committed) return;
            foreach (int page in _document._partialPageCache.Keys.Where(k => !_pages.Contains(k)).ToArray()) _document._partialPageCache.Remove(page);
            foreach (var dictionary in _document._pageIndexByDict.Keys.Where(k => !_indexes.Contains(k)).ToArray()) _document._pageIndexByDict.Remove(dictionary);
            _document._outline = _outline; _document._metadata = _metadata; _document._pageLabels = _labels;
            _document._structure = _structure; _document._layers = _layers;
            _document._attachments = _attachments; _document._formFields = _forms;
        }
    }
}
