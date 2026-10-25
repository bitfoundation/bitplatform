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
            // A malformed (or hostile) /Kids graph can point back at a node it came
            // from; the depth guard alone would still walk it 2^32 ways.
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            CollectNameTree(xref, tree, result, seen, visited, pageNumber: null, depth: 0);
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

    // Range scans commit one node/page/file at a time. A failed decode has no
    // externally visible deduplication state for the next parser attempt to skip.
    internal sealed record NameNode(BitPdfDict Identity, (object? Spec, string? Name)[] Files, object?[] Children);
    internal sealed record FileResult(object Identity, BitPdfAttachment Attachment);

    internal static object? NameRoot(IBitPdfXRef xref, BitPdfDict catalog)
        => xref.FetchIfRef(catalog.GetRaw("Names")) is BitPdfDict names ? names.GetRaw("EmbeddedFiles") : null;

    internal static NameNode? ReadNameNode(IBitPdfXRef xref, object? value)
    {
        if (xref.FetchIfRef(value) is not BitPdfDict node) return null;
        var files = new List<(object?, string?)>();
        if (xref.FetchIfRef(node.GetRaw("Names")) is List<object?> pairs)
            for (int i = 0; i + 1 < pairs.Count; i += 2)
                files.Add((pairs[i + 1], (xref.FetchIfRef(pairs[i]) as BitPdfString)?.AsText()));
        return new(node, files.ToArray(), xref.FetchIfRef(node.GetRaw("Kids")) is List<object?> kids ? kids.ToArray() : []);
    }

    internal static object?[] PageFiles(IBitPdfXRef xref, BitPdfPage page)
    {
        var files = new List<object?>();
        if (page.Dict.Get("Annots") is List<object?> annots)
            foreach (var value in annots)
                if (xref.FetchIfRef(value) is BitPdfDict annotation
                    && BitPdfPrimitives.IsName(annotation.Get("Subtype"), "FileAttachment"))
                    files.Add(annotation.GetRaw("FS"));
        return files.ToArray();
    }

    internal static FileResult? ReadFile(IBitPdfXRef xref, object? spec, int? page, string? name)
    {
        var result = new List<BitPdfAttachment>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Add(xref, spec, result, seen, page, name);
        return result.Count == 0 ? null : new(seen.Single(), result[0]);
    }

    private static void CollectNameTree(IBitPdfXRef xref, BitPdfDict node, List<BitPdfAttachment> result,
        HashSet<object> seen, HashSet<object> visited, int? pageNumber, int depth)
    {
        if (depth > 32 || visited.Add(node) is false)
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
                    CollectNameTree(xref, child, result, seen, visited, pageNumber, depth + 1);
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

        BitPdfBaseStream? stream = null;
        foreach (string key in new[] { "UF", "F", "DOS", "Mac", "Unix" })
        {
            if (xref.FetchIfRef(ef.Get(key)) is BitPdfBaseStream s)
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
        catch (Exception sourceException) when (BitPdfByteSourceErrors.IsContentError(sourceException))
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
            MimeType = (stream.Dict?.Get("Subtype") as BitPdfName)?.Value?.Replace("#2F", "/", StringComparison.Ordinal),
            PageNumber = pageNumber,
            Content = content,
        });
    }
}
