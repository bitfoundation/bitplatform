// Tagged-PDF logical structure tree (/StructTreeRoot) exposure. Assistive
// technology and reflow tools use this to recover reading order, headings and
// alternate text. This parses the structure hierarchy into a simple tree; it
// does not resolve marked-content (MCID) leaves, which reference page content.

namespace Bit.BlazorUI;

internal static class BitPdfStructTree
{
    public static IReadOnlyList<BitPdfStructElement> Build(IBitPdfXRef xref, BitPdfDict catalog)
    {
        if (xref.FetchIfRef(catalog.Get("StructTreeRoot")) is not BitPdfDict root)
        {
            return Array.Empty<BitPdfStructElement>();
        }
        var visited = new HashSet<int>();
        return ReadKids(xref, root.Get("K"), visited, 0);
    }

    private static List<BitPdfStructElement> ReadKids(IBitPdfXRef xref, object? kids, HashSet<int> visited, int depth)
    {
        var result = new List<BitPdfStructElement>();
        if (depth > 50)
        {
            return result;
        }

        switch (kids)
        {
            case List<object?> arr:
                foreach (var item in arr)
                {
                    AddNode(xref, item, result, visited, depth);
                }
                break;
            case not null:
                AddNode(xref, kids, result, visited, depth);
                break;
        }
        return result;
    }

    private static void AddNode(IBitPdfXRef xref, object? item, List<BitPdfStructElement> result, HashSet<int> visited, int depth)
    {
        // Marked-content leaves are plain integers or MCR/OBJR dicts; skip them —
        // the structure tree API exposes the element hierarchy, not content refs.
        if (item is double)
        {
            return;
        }
        if (item is BitPdfRef r && !visited.Add(r.Num))
        {
            return; // cycle guard
        }
        if (xref.FetchIfRef(item) is not BitPdfDict elem)
        {
            return;
        }

        string? sType = (elem.Get("S") as BitPdfName)?.Value;
        if (sType is null)
        {
            // A grouping node without /S (e.g. an MCR/OBJR container): recurse.
            result.AddRange(ReadKids(xref, elem.Get("K"), visited, depth + 1));
            return;
        }

        result.Add(new BitPdfStructElement
        {
            Type = sType,
            Alt = (elem.Get("Alt") as BitPdfString)?.AsText(),
            ActualText = (elem.Get("ActualText") as BitPdfString)?.AsText(),
            Children = ReadKids(xref, elem.Get("K"), visited, depth + 1),
        });
    }
}
