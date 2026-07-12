// Document outline (bookmarks) parsing. Resolves the /Outlines tree and maps
// each item's destination to a page index.

namespace Bit.BlazorUI;

/// <summary>
/// Builds the outline (bookmark) tree from a document catalog, resolving both
/// explicit and named destinations to page numbers.
/// </summary>
internal sealed class BitPdfOutlineBuilder
{
    private readonly IBitPdfXRef _xref;
    private readonly BitPdfDict _catalog;
    private readonly IReadOnlyDictionary<BitPdfDict, int> _pageIndex;

    public BitPdfOutlineBuilder(IBitPdfXRef xref, BitPdfDict catalog, IReadOnlyDictionary<BitPdfDict, int> pageIndex)
    {
        _xref = xref;
        _catalog = catalog;
        _pageIndex = pageIndex;
    }

    public IReadOnlyList<BitPdfOutlineItem> Build()
    {
        if (_xref.FetchIfRef(_catalog.Get("Outlines")) is not BitPdfDict outlines)
        {
            return Array.Empty<BitPdfOutlineItem>();
        }
        return BuildSiblings(outlines.GetRaw("First"), new HashSet<int>(), depth: 0);
    }

    private List<BitPdfOutlineItem> BuildSiblings(object? firstRaw, HashSet<int> visited, int depth)
    {
        var items = new List<BitPdfOutlineItem>();
        if (depth > 32)
        {
            // Very deep outline: stop descending but record why, rather than
            // silently dropping the deeper bookmarks.
            (_xref as BitPdfXRef)?.Warnings.Add("Outline nesting deeper than 32 levels; deeper bookmarks omitted.");
            return items;
        }

        // Walk the linked list of siblings. Entries are normally indirect
        // references (tracked for cycle safety); a direct dictionary is tolerated.
        object? current = firstRaw;
        int guard = 0;
        while (current is not null && guard++ < 8192)
        {
            BitPdfDict? node;
            if (current is BitPdfRef r)
            {
                if (!visited.Add(r.Num))
                {
                    break;
                }
                node = _xref.Fetch(r) as BitPdfDict;
            }
            else
            {
                node = current as BitPdfDict;
            }
            if (node is null)
            {
                break;
            }

            string title = (node.Get("Title") as BitPdfString)?.AsText() ?? "";
            BitPdfDestination? dest = ResolveDestinationInfo(node);
            var children = BuildSiblings(node.GetRaw("First"), visited, depth + 1);

            items.Add(new BitPdfOutlineItem
            {
                Title = title.Trim(),
                PageNumber = dest?.PageNumber,
                Destination = dest,
                Children = children,
            });

            current = node.GetRaw("Next");
        }
        return items;
    }

    private BitPdfDestination? ResolveDestinationInfo(BitPdfDict node)
    {
        // A bookmark may carry an explicit /Dest or a GoTo action /A with /D.
        object? dest = node.Get("Dest");
        if (dest is null && _xref.FetchIfRef(node.Get("A")) is BitPdfDict action
            && BitPdfPrimitives.IsName(action.Get("S"), "GoTo"))
        {
            dest = action.Get("D");
        }
        return ResolveDestination(dest);
    }

    internal BitPdfDestination? ResolveDestination(object? dest)
    {
        dest = _xref.FetchIfRef(dest);

        // Named destination: look it up in the name tree / dests dictionary.
        if (dest is BitPdfName name)
        {
            dest = LookupNamedDestination(name.Value);
        }
        else if (dest is BitPdfString s)
        {
            dest = LookupNamedDestination(s.AsLatin1());
        }
        dest = _xref.FetchIfRef(dest);

        // A destination dictionary may wrap the array under /D.
        if (dest is BitPdfDict destDict)
        {
            dest = _xref.FetchIfRef(destDict.Get("D"));
        }

        if (dest is List<object?> arr && arr.Count > 0)
        {
            int? page = ResolvePageOfTarget(arr[0]);
            return BitPdfDestination.FromArray(page, arr, _xref);
        }
        return null;
    }

    private int? ResolvePageOfTarget(object? target)
    {
        if (target is BitPdfRef pageRef && _xref.Fetch(pageRef) is BitPdfDict pageDict
            && _pageIndex.TryGetValue(pageDict, out int idx))
        {
            return idx + 1;
        }
        // Some destinations encode a 0-based page index directly. Clamp it to the
        // valid range so a corrupt/out-of-range index can't point past the document.
        if (target is double d)
        {
            int count = _pageIndex.Count;
            if (count <= 0)
            {
                return (int)d + 1;
            }
            return Math.Clamp((int)d, 0, count - 1) + 1;
        }
        return null;
    }

    private object? LookupNamedDestination(string name)
    {
        // PDF 1.1 style: catalog /Dests dictionary keyed by name.
        if (_xref.FetchIfRef(_catalog.Get("Dests")) is BitPdfDict dests && dests.Has(name))
        {
            return dests.Get(name);
        }
        // PDF 1.2+ style: /Names /Dests name tree (keyed by string).
        if (_xref.FetchIfRef(_catalog.Get("Names")) is BitPdfDict names
            && _xref.FetchIfRef(names.Get("Dests")) is BitPdfDict destsTree)
        {
            return SearchNameTree(destsTree, name, depth: 0);
        }
        return null;
    }

    private object? SearchNameTree(BitPdfDict node, string key, int depth)
    {
        if (depth > 32)
        {
            return null;
        }

        // Leaf: /Names is a flat [key1 value1 key2 value2 ...] array.
        if (_xref.FetchIfRef(node.Get("Names")) is List<object?> pairs)
        {
            for (int i = 0; i + 1 < pairs.Count; i += 2)
            {
                if (_xref.FetchIfRef(pairs[i]) is BitPdfString s && s.AsLatin1() == key)
                {
                    return pairs[i + 1];
                }
            }
        }

        // Interior: descend into /Kids whose /Limits bracket the key.
        if (_xref.FetchIfRef(node.Get("Kids")) is List<object?> kids)
        {
            foreach (var kidObj in kids)
            {
                if (_xref.FetchIfRef(kidObj) is not BitPdfDict kid)
                {
                    continue;
                }
                if (kid.Get("Limits") is List<object?> limits && limits.Count >= 2
                    && _xref.FetchIfRef(limits[0]) is BitPdfString lo && _xref.FetchIfRef(limits[1]) is BitPdfString hi)
                {
                    if (string.CompareOrdinal(key, lo.AsLatin1()) < 0
                        || string.CompareOrdinal(key, hi.AsLatin1()) > 0)
                    {
                        continue;
                    }
                }
                object? found = SearchNameTree(kid, key, depth + 1);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        return null;
    }
}
