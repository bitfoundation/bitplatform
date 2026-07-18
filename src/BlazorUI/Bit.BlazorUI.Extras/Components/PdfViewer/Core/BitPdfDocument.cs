// Document parsing: cross-reference resolution, the catalog and page-tree traversal.

namespace Bit.BlazorUI;

/// <summary>
/// A parsed PDF document: cross-reference resolution, the catalog, and the
/// flattened list of pages. This is the entry point of the C# engine.
/// </summary>
public sealed class BitPdfDocument
{
    private readonly BitPdfXRef _xref;
    private List<BitPdfPage>? _pages;
    private readonly Dictionary<BitPdfDict, int> _pageIndexByDict = new(ReferenceEqualityComparer.Instance);
    private IReadOnlyList<BitPdfOutlineItem>? _outline;
    private BitPdfMetadata? _metadata;
    private IReadOnlyList<string>? _pageLabels;

    private BitPdfDocument(BitPdfXRef xref) => _xref = xref;

    /// <summary>The cross-reference reader for this document.</summary>
    public BitPdfXRef XRef => _xref;

    /// <summary>The document catalog (<c>/Root</c>).</summary>
    public BitPdfDict Catalog { get; private set; } = BitPdfDict.Empty;

    /// <summary>The PDF version declared in the header (e.g. "1.7"), if present.</summary>
    public string? Version { get; private set; }

    /// <summary><c>true</c> when the trailer declares an <c>/Encrypt</c> dictionary.</summary>
    public bool IsEncrypted => _xref.Trailer?.Has("Encrypt") == true;

    /// <summary>
    /// The user access permissions (<c>/P</c>) of an encrypted document. Every
    /// permission is granted for an unencrypted document.
    /// </summary>
    public BitPdfPermissions Permissions => new(_xref.Permissions ?? -1, IsEncrypted && _xref.Permissions.HasValue);

    /// <summary>
    /// Non-fatal diagnostics from parsing (e.g. the file was damaged and its
    /// cross-reference table had to be rebuilt by scanning). Empty for clean files.
    /// </summary>
    public IReadOnlyList<string> Warnings => _xref.Warnings;

    /// <summary>The document's pages in order.</summary>
    public IReadOnlyList<BitPdfPage> Pages => _pages ??= BuildPages();

    /// <summary>Number of pages.</summary>
    public int PageCount => Pages.Count;

    /// <summary>The document outline (bookmarks), empty when none is present.</summary>
    public IReadOnlyList<BitPdfOutlineItem> Outline
    {
        get
        {
            if (_outline is null)
            {
                _ = Pages; // ensure the page-index map is populated
                _outline = new BitPdfOutlineBuilder(_xref, Catalog, _pageIndexByDict).Build();
            }
            return _outline;
        }
    }

    /// <summary>
    /// Document metadata: the <c>/Info</c> dictionary fields plus the raw XMP
    /// packet (<c>/Metadata</c>) when present.
    /// </summary>
    public BitPdfMetadata Metadata =>
        _metadata ??= BitPdfMetadata.Build(_xref, _xref.Trailer?.Get("Info") as BitPdfDict, Catalog);

    /// <summary>
    /// The per-page labels declared by the catalog's <c>/PageLabels</c> number
    /// tree (e.g. "i", "ii", "1", "A-1"), one per page in document order. When no
    /// <c>/PageLabels</c> entry exists, labels default to the 1-based page number.
    /// </summary>
    public IReadOnlyList<string> PageLabels =>
        _pageLabels ??= BitPdfPageLabelBuilder.Build(_xref, Catalog, PageCount);

    private IReadOnlyList<BitPdfStructElement>? _structure;

    /// <summary>
    /// The tagged-PDF logical structure tree (<c>/StructTreeRoot</c>) for
    /// assistive technology and reflow, or empty when the document is untagged.
    /// </summary>
    public IReadOnlyList<BitPdfStructElement> StructureTree =>
        _structure ??= BitPdfStructTree.Build(_xref, Catalog);

    private IReadOnlyList<BitPdfFormField>? _formFields;

    /// <summary>
    /// The interactive form fields (<c>/AcroForm</c>) as a flat list of
    /// name/type/value, or empty when the document has no form.
    /// </summary>
    public IReadOnlyList<BitPdfFormField> FormFields =>
        _formFields ??= BitPdfAcroForm.Build(_xref, Catalog);

    /// <summary>
    /// Resolves a destination (a named destination string/name, or an explicit
    /// <c>[page …]</c> array, or a GoTo action's <c>/D</c>) to a 1-based page
    /// number, or <c>null</c> when it cannot be resolved. Used for internal links.
    /// </summary>
    public int? ResolveDestinationPage(object? dest)
    {
        _ = Pages; // ensure the page-index map is populated
        return new BitPdfOutlineBuilder(_xref, Catalog, _pageIndexByDict).ResolveDestination(dest)?.PageNumber;
    }

    /// <summary>Parses <paramref name="bytes"/> into a document model.</summary>
    public static BitPdfDocument Load(byte[] bytes) => Load(bytes, null);

    /// <summary>
    /// Parses <paramref name="bytes"/> into a document model, using
    /// <paramref name="password"/> to decrypt an encrypted document (tried as
    /// both the user and owner password). Throws <see cref="BitPdfPasswordException"/>
    /// when a required password is missing or wrong.
    /// </summary>
    public static BitPdfDocument Load(byte[] bytes, string? password)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var xref = new BitPdfXRef(bytes) { Password = password };
        xref.Parse();

        var document = new BitPdfDocument(xref)
        {
            Version = ReadHeaderVersion(bytes),
        };

        document.Catalog = xref.Root
            ?? throw new BitPdfFormatException("Document catalog (/Root) not found.");

        // A catalog /Version overrides the header version when it is later
        // (PDF 32000-1 §7.5.2); an incremental update can raise the version here.
        if (document.Catalog.Get("Version") is BitPdfName catalogVersion
            && string.CompareOrdinal(catalogVersion.Value, document.Version) > 0)
        {
            document.Version = catalogVersion.Value;
        }

        return document;
    }

    private List<BitPdfPage> BuildPages()
    {
        var pages = new List<BitPdfPage>();
        if (Catalog.Get("Pages") is not BitPdfDict root)
        {
            return pages;
        }

        var inherited = new InheritedAttributes();
        var visited = new HashSet<int>();
        Traverse(root, inherited, pages, visited, depth: 0);
        return pages;
    }

    private readonly struct InheritedAttributes
    {
        public double[]? MediaBox { get; init; }
        public double[]? CropBox { get; init; }
        public BitPdfDict? Resources { get; init; }
        public int? Rotate { get; init; }

        public InheritedAttributes With(BitPdfDict node, IBitPdfXRef xref)
        {
            return new InheritedAttributes
            {
                MediaBox = ReadRectangle(node.Get("MediaBox"), xref) ?? MediaBox,
                CropBox = ReadRectangle(node.Get("CropBox"), xref) ?? CropBox,
                Resources = node.Get("Resources") as BitPdfDict ?? Resources,
                Rotate = node.Get("Rotate") is double r ? NormalizeRotation((int)r) : Rotate,
            };
        }
    }

    private void Traverse(BitPdfDict node, in InheritedAttributes inherited,
        List<BitPdfPage> pages, HashSet<int> visited, int depth)
    {
        if (depth > 64)
        {
            // Degrade rather than abort the whole document: skip this subtree and
            // warn, so the rest of the page tree still loads.
            _xref.Warnings.Add("Page tree nesting too deep; skipping a subtree.");
            return;
        }

        InheritedAttributes current = inherited.With(node, _xref);
        object? typeObj = node.Get("Type");

        // A node with /Kids is an interior /Pages node; otherwise it's a leaf page.
        if (node.Get("Kids") is List<object?> kids && !BitPdfPrimitives.IsName(typeObj, "Page"))
        {
            foreach (var kid in kids)
            {
                // Skip kids already seen: duplicate /Kids refs would otherwise
                // duplicate pages, and shared-subtree DAGs would traverse
                // exponentially. Cyclic kids are skipped, not thrown, so the
                // rest of the tree still loads.
                if (kid is BitPdfRef kr && !visited.Add(kr.Num))
                {
                    continue;
                }
                if (_xref.FetchIfRef(kid) is BitPdfDict child)
                {
                    Traverse(child, current, pages, visited, depth + 1);
                }
            }
            return;
        }

        // A node explicitly typed /Pages but with no usable /Kids is a damaged
        // interior node - skip it rather than materializing a phantom page.
        if (BitPdfPrimitives.IsName(typeObj, "Pages"))
        {
            _xref.Warnings.Add("Interior /Pages node has no valid /Kids; skipping.");
            return;
        }

        double[] mediaBox = current.MediaBox ?? [0, 0, 612, 792]; // US Letter default
        _pageIndexByDict[node] = pages.Count;
        pages.Add(new BitPdfPage(
            _xref,
            node,
            pages.Count + 1,
            mediaBox,
            current.Resources,
            current.Rotate ?? 0,
            current.CropBox));
    }

    private static double[]? ReadRectangle(object? value, IBitPdfXRef xref)
    {
        if (xref.FetchIfRef(value) is not List<object?> arr || arr.Count < 4)
        {
            return null;
        }
        var rect = new double[4];
        for (int i = 0; i < 4; i++)
        {
            if (xref.FetchIfRef(arr[i]) is not double d)
            {
                return null;
            }
            rect[i] = d;
        }
        return rect;
    }

    private static int NormalizeRotation(int rotate)
    {
        int r = rotate % 360;
        if (r < 0)
        {
            r += 360;
        }
        return r;
    }

    private static string? ReadHeaderVersion(byte[] bytes)
    {
        // Header looks like "%PDF-1.7" within the first bytes of the file.
        int limit = Math.Min(bytes.Length, 1024);
        ReadOnlySpan<byte> prefix = "%PDF-"u8;
        for (int i = 0; i + prefix.Length < limit; i++)
        {
            bool match = true;
            for (int k = 0; k < prefix.Length; k++)
            {
                if (bytes[i + k] != prefix[k])
                {
                    match = false;
                    break;
                }
            }
            if (!match)
            {
                continue;
            }
            int start = i + prefix.Length;
            int end = start;
            while (end < limit && bytes[end] is not (0x0D or 0x0A or 0x20))
            {
                end++;
            }
            return System.Text.Encoding.ASCII.GetString(bytes, start, end - start);
        }
        return null;
    }
}
