using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A collaborative XML element: a name, attributes, and an ordered list of child elements and texts.
/// </summary>
/// <remarks>
/// <para>
/// The name is fixed when the element is created and travels with it - an element that should change
/// its name, a paragraph becoming a heading, is replaced by a new one. Attributes live in the type's key
/// index, so they behave exactly like <see cref="BcrdtMap"/> keys: different attributes never conflict,
/// and concurrent writes to one attribute resolve last-writer-wins, the same way on every replica. The
/// children are the sequence <see cref="BcrdtXmlContainer"/> describes.
/// </para>
/// <para>
/// An attribute holds a value from the collaborative value model - a string, a number, a flag, a list or
/// map of those - never a collaborative type: content that should merge belongs in the children. A peer
/// can still write something else under an element's key through the generic map operations; every
/// replica ignores, identically, an entry whose name is not a valid XML name or whose value is null or a
/// collaborative type, so the attributes every replica reads are the same.
/// </para>
/// <para>
/// The wire kind is <see cref="BcrdtTypeKind.XmlElement"/>, followed by the name - the only kind that
/// carries anything beyond its number. A name decoded from a peer is validated exactly as a local one is,
/// and an update with an invalid one is rejected whole.
/// </para>
/// </remarks>
public sealed class BcrdtXmlElement : BcrdtXmlContainer, IBcrdtXmlNode
{
    /// <summary>The longest element or attribute name, in UTF-16 code units.</summary>
    /// <remarks>
    /// Real tag names are a few characters long; the bound keeps a hostile peer from making every replica
    /// hold - and every render repeat - a name of arbitrary size.
    /// </remarks>
    public const int MaxNameLength = 256;

    private Dictionary<string, object?>? _pendingAttributes;

    /// <summary>Creates an empty element, not yet attached to a document.</summary>
    /// <param name="name">The tag name: an XML 1.0 name of at most <see cref="MaxNameLength"/> characters.</param>
    /// <remarks>
    /// A detached element can be given attributes and children before it is inserted anywhere; they are
    /// replayed as real collaborative operations at the moment of insertion, so a whole subtree can be
    /// built up in one expression and inserted in one transaction.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid XML name.</exception>
    public BcrdtXmlElement(string name)
    {
        BcrdtXmlSyntax.ValidateName(name, nameof(name));
        Name = name;
        _pendingAttributes = new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    /// <summary>The tag name.</summary>
    public string Name { get; }

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.XmlElement;

    /// <inheritdoc/>
    public BcrdtXmlContainer? ParentNode => ParentOf(this);

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtXmlElement(Name);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pendingAttributes is null) return;
        Dictionary<string, object?> buffered = _pendingAttributes;
        _pendingAttributes = null;
        foreach ((string name, object? value) in buffered) SetAttribute(name, value);
    }

    /// <summary>Sets an attribute.</summary>
    /// <param name="name">The attribute name: an XML 1.0 name of at most <see cref="MaxNameLength"/> characters.</param>
    /// <param name="value">
    /// A value from the collaborative value model. <see langword="null"/> removes the attribute, since an
    /// attribute that is present has a value.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid XML name.</exception>
    /// <exception cref="NotSupportedException">
    /// The value is outside the collaborative value model, or is a collaborative type.
    /// </exception>
    public void SetAttribute(string name, object? value)
    {
        BcrdtXmlSyntax.ValidateName(name, nameof(name));
        if (value is null)
        {
            RemoveAttribute(name);
            return;
        }
        if (value is BcrdtAbstractType)
        {
            throw new NotSupportedException(
                "An XML attribute holds a value, not a collaborative type. Put content that should merge in the element's children.");
        }

        // Validated and copied before anything changes, so a value that cannot be stored is refused
        // while that is still free.
        object? canonical = BcrdtAnyCodec.Canonicalize(value);

        if (_pendingAttributes is not null)
        {
            _pendingAttributes[name] = canonical;
            return;
        }

        Doc.Transact(transaction => BcrdtTypeOperations.MapSet(transaction, this, name, canonical));
    }

    /// <summary>Reads an attribute, or <see langword="null"/> if the element does not have it.</summary>
    public object? GetAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_pendingAttributes is not null) return _pendingAttributes.GetValueOrDefault(name);
        return MapGetRaw(name) is { } item && TryReadAttribute(name, item, out object? value) ? value : null;
    }

    /// <summary>Whether the element has an attribute.</summary>
    public bool HasAttribute(string name) => GetAttribute(name) is not null;

    /// <summary>Removes an attribute. Removing one the element does not have changes nothing.</summary>
    public void RemoveAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_pendingAttributes is not null)
        {
            _pendingAttributes.Remove(name);
            return;
        }

        if (MapGetRaw(name) is not { Deleted: false }) return;
        Doc.Transact(transaction => BcrdtTypeOperations.MapDelete(transaction, this, name));
    }

    /// <summary>A snapshot of the attributes, ordered by name.</summary>
    public IReadOnlyDictionary<string, object?> GetAttributes()
    {
        var attributes = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        if (_pendingAttributes is not null)
        {
            foreach ((string name, object? value) in _pendingAttributes) attributes[name] = value;
            return attributes;
        }

        foreach ((string name, BcrdtItem item) in KeyIndex)
        {
            if (TryReadAttribute(name, item, out object? value)) attributes[name] = value;
        }
        return attributes;
    }

    /// <summary>Reads what a key holds if every replica treats it as an attribute.</summary>
    private static bool TryReadAttribute(string name, BcrdtItem item, out object? value)
    {
        value = null;
        if (item.Deleted || !BcrdtXmlSyntax.IsValidName(name)) return false;
        object? stored = item.Content.GetValue(item.Length - 1);
        if (!IsAttributeValue(stored)) return false;
        value = stored;
        return true;
    }

    /// <summary>Whether a stored value counts as an attribute's value: present, and not a collaborative type.</summary>
    internal static bool IsAttributeValue(object? value) => value is not null and not BcrdtAbstractType;

    /// <inheritdoc/>
    /// <remarks>
    /// The element's start tag with its attributes in ordinal order, its children, and its end tag. See
    /// <see cref="BcrdtXmlContainer.ToXmlString"/> for how text and values are escaped.
    /// </remarks>
    public override string ToXmlString()
    {
        var builder = new StringBuilder();
        AppendXml(builder);
        return builder.ToString();
    }

    internal void AppendXml(StringBuilder builder)
    {
        builder.Append('<').Append(Name);
        foreach ((string name, object? value) in GetAttributes())
        {
            builder.Append(' ').Append(name).Append("=\"");
            BcrdtXmlSyntax.AppendEscaped(builder, BcrdtXmlSyntax.FormatValue(value!), attribute: true);
            builder.Append('"');
        }
        builder.Append('>');
        AppendChildren(builder);
        builder.Append("</").Append(Name).Append('>');
    }

    /// <inheritdoc/>
    /// <remarks>The copy has the same name, attributes and - copied in turn - children.</remarks>
    public override BcrdtXmlElement Clone()
    {
        var clone = new BcrdtXmlElement(Name);
        foreach ((string name, object? value) in GetAttributes()) clone.SetAttribute(name, value);
        clone.Insert(0, CloneChildren());
        return clone;
    }

    /// <summary>
    /// A plain-object projection: a map of <c>name</c>, <c>attributes</c> (by name) and <c>children</c>
    /// (each child's projection, in order).
    /// </summary>
    /// <remarks>Keys are sorted, so converged replicas project identically.</remarks>
    public override object ToJson() => new SortedDictionary<string, object?>(StringComparer.Ordinal)
    {
        ["attributes"] = GetAttributes(),
        ["children"] = ChildrenToJson(),
        ["name"] = Name,
    };

    /// <inheritdoc/>
    public override string ToString() => ToXmlString();
}
