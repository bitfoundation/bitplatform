// Embedded-file (attachment) extraction: the catalog's /Names /EmbeddedFiles name
// tree plus the /FileAttachment annotations pinned to individual pages.

namespace Bit.BlazorUI;

internal static class BitPdfEmbeddedFiles
{
    public static IReadOnlyList<BitPdfAttachment> Build(IBitPdfXRef xref, BitPdfDict catalog, IReadOnlyList<BitPdfPage> pages)
    {
        var result = new List<BitPdfAttachment>();
        // The same file specification can be reachable both from the name tree and
        // from an annotation; the stream reference is what identifies it.
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        if (xref.FetchIfRef(catalog.Get("Names")) is BitPdfDict names
            && xref.FetchIfRef(names.Get("EmbeddedFiles")) is BitPdfDict tree)
        {
            CollectNameTree(xref, tree, result, seen, pageNumber: null, depth: 0);
        }

        foreach (var page in pages)
        {
            if (page.Dict.Get("Annots") is not List<object?> annots)
            {
                continue;
            }
            foreach (var item in annots)
            {
                if (xref.FetchIfRef(item) is BitPdfDict annot
                    && BitPdfPrimitives.IsName(annot.Get("Subtype"), "FileAttachment"))
                {
                    Add(xref, annot.Get("FS"), result, seen, page.Number, fallbackName: null);
                }
            }
        }

        return result;
    }

    private static void CollectNameTree(IBitPdfXRef xref, BitPdfDict node, List<BitPdfAttachment> result,
        HashSet<object> seen, int? pageNumber, int depth)
    {
        if (depth > 32)
        {
            return;
        }

        // Leaf: /Names is a flat [key1 value1 key2 value2 ...] array, where the key
        // is the tree's own name for the file and the value its file specification.
        if (xref.FetchIfRef(node.Get("Names")) is List<object?> pairs)
        {
            for (int i = 0; i + 1 < pairs.Count; i += 2)
            {
                string? key = (xref.FetchIfRef(pairs[i]) as BitPdfString)?.AsText();
                Add(xref, pairs[i + 1], result, seen, pageNumber, key);
            }
        }

        if (xref.FetchIfRef(node.Get("Kids")) is List<object?> kids)
        {
            foreach (var kid in kids)
            {
                if (xref.FetchIfRef(kid) is BitPdfDict child)
                {
                    CollectNameTree(xref, child, result, seen, pageNumber, depth + 1);
                }
            }
        }
    }

    private static void Add(IBitPdfXRef xref, object? fileSpecObj, List<BitPdfAttachment> result,
        HashSet<object> seen, int? pageNumber, string? fallbackName)
    {
        if (xref.FetchIfRef(fileSpecObj) is not BitPdfDict spec)
        {
            return;
        }
        // /EF holds the embedded streams, keyed by the flavour of the path that
        // produced them; /F is the portable one, /UF its Unicode counterpart.
        if (xref.FetchIfRef(spec.Get("EF")) is not BitPdfDict ef)
        {
            return;
        }

        BitPdfStream? stream = null;
        foreach (string key in new[] { "UF", "F", "DOS", "Mac", "Unix" })
        {
            if (xref.FetchIfRef(ef.Get(key)) is BitPdfStream s)
            {
                stream = s;
                break;
            }
        }
        if (stream is null || seen.Add(stream) is false)
        {
            return;
        }

        byte[] content;
        try
        {
            content = BitPdfStreamDecoder.Decode(stream);
        }
        catch
        {
            // A broken attachment must not cost the document its other ones.
            content = [];
        }

        string? name = (xref.FetchIfRef(spec.Get("UF")) as BitPdfString)?.AsText()
            ?? (xref.FetchIfRef(spec.Get("F")) as BitPdfString)?.AsText()
            ?? fallbackName;

        result.Add(new BitPdfAttachment
        {
            Name = string.IsNullOrWhiteSpace(name) ? "attachment" : name,
            Description = (xref.FetchIfRef(spec.Get("Desc")) as BitPdfString)?.AsText(),
            MimeType = (stream.Dict.Get("Subtype") as BitPdfName)?.Value?.Replace("#2F", "/", StringComparison.Ordinal),
            PageNumber = pageNumber,
            Content = content,
        });
    }
}
