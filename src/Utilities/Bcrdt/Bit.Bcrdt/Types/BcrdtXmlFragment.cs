using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// The root of a collaborative XML tree: an ordered list of elements and texts with no tag of its own.
/// </summary>
/// <remarks>
/// <para>
/// This is the type a structured editor - ProseMirror, TipTap and the like - binds its document to: the
/// fragment is the document, each <see cref="BcrdtXmlElement"/> a node with a name, attributes and
/// children of its own, and each <see cref="BcrdtXmlText"/> a run of rich text. It follows the shape of
/// Yjs's <c>Y.XmlFragment</c>, so an editor binding written for that model maps onto this one, though the
/// wire format is bit Bcrdt's own.
/// </para>
/// <para>
/// Every part of the tree merges rather than overwrites: concurrent insertions of children all survive in
/// a stable order, concurrent edits to one text interleave like any <see cref="BcrdtText"/>, and two
/// clients setting different attributes of one element both win. Two clients setting the <i>same</i>
/// attribute resolve last-writer-wins, as a <see cref="BcrdtMap"/> key does.
/// </para>
/// <para>
/// A fragment is usually a root - <see cref="BcrdtDocument.GetXmlFragment"/> - but it can also be nested
/// in a map or an array, one tree per entry. It is not itself a child node: only elements and texts go
/// inside a fragment or an element.
/// </para>
/// <para>
/// The wire kind is <see cref="BcrdtTypeKind.XmlFragment"/>. A root's kind never travels - only its
/// name does - but its children are elements and texts, whose kinds do: a replica that predates the tree
/// types rejects an update carrying any of them as malformed rather than misreading it.
/// </para>
/// </remarks>
public sealed class BcrdtXmlFragment : BcrdtXmlContainer
{
    /// <summary>Creates an empty fragment, not yet attached to a document.</summary>
    /// <remarks>
    /// A detached fragment can be filled before it is inserted anywhere; the buffered children are
    /// replayed as real collaborative operations at the moment of insertion.
    /// </remarks>
    public BcrdtXmlFragment() { }

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.XmlFragment;

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtXmlFragment();

    /// <inheritdoc/>
    /// <remarks>The children's markup, one after another, with no enclosing tag.</remarks>
    public override string ToXmlString()
    {
        var builder = new StringBuilder();
        AppendChildren(builder);
        return builder.ToString();
    }

    /// <inheritdoc/>
    public override BcrdtXmlFragment Clone()
    {
        var clone = new BcrdtXmlFragment();
        clone.Insert(0, CloneChildren());
        return clone;
    }

    /// <summary>The children's projections, in order: see <see cref="BcrdtXmlElement.ToJson"/> and <see cref="BcrdtXmlText"/>.</summary>
    public override object ToJson() => ChildrenToJson();

    /// <inheritdoc/>
    public override string ToString() => ToXmlString();
}
