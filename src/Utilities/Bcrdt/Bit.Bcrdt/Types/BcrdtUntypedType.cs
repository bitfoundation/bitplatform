namespace Bit.Bcrdt;

/// <summary>
/// A root type whose kind has not been declared yet.
/// </summary>
/// <remarks>
/// A document can receive content for a root type before the application ever asks for it - remote
/// updates arrive by name, and the name alone does not say whether it is a map or an array. Content
/// lands here until the application calls for the root by a specific kind, at which point the
/// accumulated blocks are re-parented onto a properly typed instance. Nested types never need this,
/// because their kind travels with them on the wire.
/// </remarks>
public sealed class BcrdtUntypedType : BcrdtAbstractType
{
    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Untyped;

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtUntypedType();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtGenericEvent(this, transaction);

    /// <summary>
    /// Projects the accumulated content, inferring its shape from what it actually contains.
    /// </summary>
    /// <remarks>
    /// The kind is unknown by definition here, so it is inferred rather than declared: keyed content
    /// is a map, character content is text, anything else is a sequence. This keeps a document
    /// inspectable and comparable even for roots the application has not asked for yet, which is
    /// exactly the situation a replica is in right after receiving its first update.
    /// </remarks>
    public override object ToJson()
    {
        if (KeyIndex.Count > 0)
        {
            var map = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach ((string key, object? value) in BcrdtTypeOperations.MapEntries(this))
            {
                map[key] = value is BcrdtAbstractType nested ? nested.ToJson() : value;
            }
            return map;
        }

        if (Start is null) return new SortedDictionary<string, object?>(StringComparer.Ordinal);

        bool looksLikeText = false;
        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (item.Deleted) continue;
            if (item.Content is BcrdtContentString or BcrdtContentFormat)
            {
                looksLikeText = true;
                break;
            }
        }

        if (looksLikeText)
        {
            var builder = new System.Text.StringBuilder();
            for (BcrdtItem? item = Start; item is not null; item = item.Right)
            {
                if (!item.Deleted && item.Content is BcrdtContentString text) builder.Append(text.Text);
            }
            return builder.ToString();
        }

        return BcrdtTypeOperations.ListToArray(this)
            .Select(value => value is BcrdtAbstractType nested ? nested.ToJson() : value)
            .ToList();
    }
}
