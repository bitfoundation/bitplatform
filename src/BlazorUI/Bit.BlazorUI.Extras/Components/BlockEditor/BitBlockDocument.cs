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
/// <para>
/// The blocks are copied as they are set - by the constructor or by a <c>with</c> - so a change to the list they came
/// from never reaches the document, and every block id is made unique there: a block whose id is empty, or is one an
/// earlier block of the document (in document order, children after their parent) already has, is given a new one, and
/// every other block stays the same instance.
/// </para>
/// </remarks>
public sealed record BitBlockDocument
{
    private readonly IReadOnlyList<BitBlock> _blocks = BitBlock.Freeze(null);

    /// <summary>Creates a document of the given blocks.</summary>
    public BitBlockDocument(IReadOnlyList<BitBlock> blocks)
    {
        Blocks = blocks;
    }

    /// <summary>A document with no blocks.</summary>
    public static BitBlockDocument Empty { get; } = new([]);

    /// <summary>The document's top-level blocks, in order.</summary>
    public IReadOnlyList<BitBlock> Blocks
    {
        get => _blocks;
        init => _blocks = WithUniqueIds(BitBlock.Freeze(value));
    }

    /// <summary>Writes the document as JSON, the lossless form to store it in.</summary>
    /// <remarks>
    /// The JSON is limited to 512 levels of depth, which a document reaches at 255 levels of nested blocks (254 when
    /// the deepest block has props).
    /// </remarks>
    /// <exception cref="System.Text.Json.JsonException">The document is nested deeper than that.</exception>
    public string ToJson() => BitBlockJson.Write(this);

    /// <summary>
    /// Reads a document from its JSON form. A null or empty string is an empty document.
    /// </summary>
    /// <param name="json">The JSON, as <see cref="ToJson"/> writes it.</param>
    /// <param name="types">
    /// The app's own block types, if any. The text of a block whose type is formatted text is put into the text
    /// vocabulary on the way in; that of a code block, or of a type nobody registered, is kept as it is.
    /// </param>
    /// <remarks>
    /// Every id the JSON gives is kept, but for one an earlier block already has; a block without one, or with a
    /// repeated one, is given an id no block of the document has.
    /// </remarks>
    /// <exception cref="System.Text.Json.JsonException">
    /// The string is not JSON of this form, or is deeper than 512 levels (more than 255 levels of nested blocks).
    /// </exception>
    public static BitBlockDocument FromJson(string? json, IEnumerable<BitBlockType>? types = null)
    {
        var registry = BitBlockTypes.Resolve(types);
        var blocks = BitBlockJson.Read(json);
        return new(Prepare(blocks, registry));
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
        => new(Prepare(BitBlockHtmlConverter.Read(html), BitBlockTypes.Resolve(null)));

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
        => new(Prepare(BitBlockMarkdownReader.Read(markdown), BitBlockTypes.Resolve(null)));

    /// <inheritdoc />
    public bool Equals(BitBlockDocument? other)
        => ReferenceEquals(this, other) || (other is not null && Blocks.SequenceEqual(other.Blocks));

    /// <inheritdoc />
    public override int GetHashCode() => Blocks.Count;

    // Puts the text of every formatted-text block into the vocabulary, all the way down. The ids are the constructor's.
    private static List<BitBlock> Prepare(IReadOnlyList<BitBlock> blocks, Dictionary<string, BitBlockType> registry)
    {
        var result = new List<BitBlock>(blocks.Count);
        foreach (var block in blocks)
        {
            var text = registry.TryGetValue(block.Type, out var type) && type.Kind == BitBlockKind.Text
                ? BitBlockInline.Normalize(block.Text)
                : block.Text;

            result.Add(block with
            {
                Text = text,
                Children = block.Children.Count == 0 ? block.Children : Prepare(block.Children, registry)
            });
        }

        return result;
    }

    // Gives a new id to every block whose id is empty or taken by an earlier block, one no block of the document has,
    // so a generated id never takes an id a later block brings. The rest stay the same instances, and a list with
    // nothing to change is returned as it is.
    private static IReadOnlyList<BitBlock> WithUniqueIds(IReadOnlyList<BitBlock> blocks)
    {
        var reserved = new HashSet<string>(StringComparer.Ordinal);
        if (CollectIds(blocks, reserved) is false) return blocks;

        return Renumber(blocks, reserved, new HashSet<string>(StringComparer.Ordinal));
    }

    // Collects every id, and says whether any of them is empty or repeated.
    private static bool CollectIds(IReadOnlyList<BitBlock> blocks, HashSet<string> ids)
    {
        var clash = false;
        foreach (var block in blocks)
        {
            if (block.Id.Length == 0 || ids.Add(block.Id) is false) clash = true;
            if (CollectIds(block.Children, ids)) clash = true;
        }

        return clash;
    }

    private static IReadOnlyList<BitBlock> Renumber(IReadOnlyList<BitBlock> blocks, HashSet<string> reserved, HashSet<string> seen)
    {
        List<BitBlock>? result = null;
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];

            var id = block.Id;
            if (id.Length == 0 || seen.Add(id) is false)
            {
                do id = BitBlock.NewId(); while (reserved.Add(id) is false);
                seen.Add(id);
            }

            var children = block.Children.Count == 0 ? block.Children : Renumber(block.Children, reserved, seen);

            if (ReferenceEquals(id, block.Id) is false || ReferenceEquals(children, block.Children) is false)
            {
                result ??= [.. blocks.Take(i)];
                result.Add(block with { Id = id, Children = children });
            }
            else
            {
                result?.Add(block);
            }
        }

        return result is null ? blocks : BitBlock.Freeze(result);
    }
}
