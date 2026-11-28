namespace Bit.BlazorUI;

/// <summary>
/// A document of typed blocks: what a block editor edits, and what an app reads, builds and stores. Immutable, so a
/// snapshot is a cheap reference and an edit shares every block it did not touch.
/// </summary>
/// <remarks>
/// <para>
/// It reads from and writes to three forms, with no component involved, so a server can render or convert stored
/// documents on its own:
/// <list type="bullet">
/// <item><b>JSON</b> (<see cref="ToJson"/>, <see cref="FromJson"/>) is the lossless form, the one to store.</item>
/// <item><b>HTML</b> (<see cref="ToHtml"/>, <see cref="FromHtml"/>) is the markup a <see cref="BitRichTextEditor"/>
/// stores, so a document can move into one and back. Reading takes any HTML: what has no block becomes a paragraph of
/// its text.</item>
/// <item><b>Markdown</b> (<see cref="ToMarkdown"/>, <see cref="FromMarkdown"/>). The text vocabulary is Markdown's own,
/// so writing a document and reading it back gives the same blocks (with new ids), except for what Markdown has no way
/// to say: an image's width, an empty paragraph, a line break in a heading.</item>
/// </list>
/// </para>
/// <para>
/// Two documents are equal when their blocks are, in order (see <see cref="BitBlock"/>).
/// </para>
/// </remarks>
public sealed record BitBlockDocument
{
    /// <summary>Creates a document of the given blocks.</summary>
    public BitBlockDocument(IReadOnlyList<BitBlock> blocks)
    {
        Blocks = blocks ?? [];
    }

    /// <summary>A document with no blocks.</summary>
    public static BitBlockDocument Empty { get; } = new([]);

    /// <summary>The document's top-level blocks, in order.</summary>
    public IReadOnlyList<BitBlock> Blocks { get; init; }

    /// <summary>Writes the document as JSON, the lossless form to store it in.</summary>
    public string ToJson() => BitBlockJson.Write(this);

    /// <summary>
    /// Reads a document from its JSON form. A null or empty string is an empty document.
    /// </summary>
    /// <param name="json">The JSON, as <see cref="ToJson"/> writes it.</param>
    /// <param name="types">
    /// The app's own block types, if any. The text of a block whose type is formatted text is put into the text
    /// vocabulary on the way in; that of a code block, or of a type nobody registered, is kept as it is.
    /// </param>
    /// <exception cref="System.Text.Json.JsonException">The string is not JSON of this form.</exception>
    public static BitBlockDocument FromJson(string? json, IEnumerable<BitBlockType>? types = null)
    {
        var registry = BitBlockTypes.Resolve(types);
        var blocks = BitBlockJson.Read(json);
        return new(Prepare(blocks, registry, new HashSet<string>(StringComparer.Ordinal)));
    }

    /// <summary>
    /// Writes the document as HTML: the markup a <see cref="BitRichTextEditor"/> stores (<c>&lt;h2&gt;</c>,
    /// <c>&lt;ul&gt;&lt;li&gt;</c>, <c>&lt;blockquote&gt;&lt;p&gt;</c>, <c>&lt;pre&gt;&lt;code class="language-x"&gt;</c>,
    /// <c>&lt;img&gt;</c>, <c>&lt;hr&gt;</c>), sanitized with its default policy.
    /// </summary>
    /// <param name="types">
    /// The app's own block types, if any; one with a <see cref="BitBlockType.ToHtml"/> writes its blocks itself.
    /// </param>
    public string ToHtml(IEnumerable<BitBlockType>? types = null)
        => BitBlockHtmlConverter.Write(Blocks, BitBlockTypes.Resolve(types));

    /// <summary>
    /// Reads a document from HTML, sanitized first with the <see cref="BitRichTextEditor"/>'s default policy. Each
    /// element with a block becomes one; anything else becomes a paragraph of its text (a table, a row per paragraph),
    /// and media becomes a link to its source. A null or empty string is an empty document.
    /// </summary>
    public static BitBlockDocument FromHtml(string? html)
        => new(Prepare(BitBlockHtmlConverter.Read(html), BitBlockTypes.Resolve(null), new HashSet<string>(StringComparer.Ordinal)));

    /// <summary>
    /// Writes the document as Markdown, with the emphasis extensions for what CommonMark lacks
    /// (<c>~~struck~~</c>, <c>~sub~</c>, <c>^sup^</c>, <c>==marked==</c>, <c>++inserted++</c>) and GitHub's task lists.
    /// </summary>
    /// <param name="types">
    /// The app's own block types, if any; one with a <see cref="BitBlockType.ToMarkdown"/> writes its blocks itself.
    /// </param>
    public string ToMarkdown(IEnumerable<BitBlockType>? types = null)
        => BitBlockMarkdownWriter.Write(Blocks, BitBlockTypes.Resolve(types));

    /// <summary>
    /// Reads a document from Markdown: CommonMark with GitHub's strikethrough and task lists and the emphasis
    /// extensions <see cref="ToMarkdown"/> writes. What has no block (a table) becomes a paragraph of its text. A null
    /// or empty string is an empty document.
    /// </summary>
    public static BitBlockDocument FromMarkdown(string? markdown)
        => new(Prepare(BitBlockMarkdownReader.Read(markdown), BitBlockTypes.Resolve(null), new HashSet<string>(StringComparer.Ordinal)));

    /// <inheritdoc />
    public bool Equals(BitBlockDocument? other)
        => ReferenceEquals(this, other) || (other is not null && Blocks.SequenceEqual(other.Blocks));

    /// <inheritdoc />
    public override int GetHashCode() => Blocks.Count;

    // Gives every block an id no other block of the document has, and puts the text of every formatted-text block into
    // the vocabulary, all the way down.
    private static List<BitBlock> Prepare(IReadOnlyList<BitBlock> blocks, Dictionary<string, BitBlockType> registry, HashSet<string> ids)
    {
        var result = new List<BitBlock>(blocks.Count);
        foreach (var block in blocks)
        {
            var id = block.Id;
            while (ids.Add(id) is false) id = BitBlock.NewId();

            var text = registry.TryGetValue(block.Type, out var type) && type.Kind == BitBlockKind.Text
                ? BitBlockInline.Normalize(block.Text)
                : block.Text;

            result.Add(block with
            {
                Id = id,
                Text = text,
                Children = block.Children.Count == 0 ? block.Children : Prepare(block.Children, registry, ids)
            });
        }

        return result;
    }
}
