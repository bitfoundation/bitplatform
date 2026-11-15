using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A run of collaborative rich text inside an XML tree.
/// </summary>
/// <remarks>
/// <para>
/// It is a <see cref="BcrdtText"/> in every respect - characters, embeds, <see cref="BcrdtText.Format"/>
/// marks, <see cref="BcrdtText.ToDelta"/> and <see cref="BcrdtText.ApplyDelta"/>, the same events and the
/// same merge - that can also be a child node of a <see cref="BcrdtXmlFragment"/> or
/// <see cref="BcrdtXmlElement"/>. That is the shape structured editors use: a paragraph element holding
/// a text whose marks are bold, italic and links.
/// </para>
/// <para>
/// Its own wire kind, <see cref="BcrdtTypeKind.XmlText"/>, is what lets every replica tell a tree's text
/// from a plain <see cref="BcrdtText"/> stored inside one - which a tree ignores - so the two never
/// convert into each other.
/// </para>
/// </remarks>
public sealed class BcrdtXmlText : BcrdtText, IBcrdtXmlNode
{
    /// <summary>Creates empty text, not yet attached to a document.</summary>
    public BcrdtXmlText() { }

    /// <summary>Creates text with initial content, not yet attached to a document.</summary>
    public BcrdtXmlText(string initialText) : base(initialText) { }

    private protected override BcrdtTypeKind TextKind => BcrdtTypeKind.XmlText;

    /// <inheritdoc/>
    public BcrdtXmlContainer? ParentNode => BcrdtXmlContainer.ParentOf(this);

    private protected override BcrdtText CreateEmpty() => new BcrdtXmlText();

    /// <inheritdoc/>
    /// <remarks>
    /// The characters, escaped. Marks are not rendered as markup and embeds are left out; read
    /// <see cref="BcrdtText.ToDelta"/> where they matter.
    /// </remarks>
    public string ToXmlString()
    {
        var builder = new StringBuilder();
        BcrdtXmlContainer.AppendNode(builder, this);
        return builder.ToString();
    }

    /// <inheritdoc/>
    public override BcrdtXmlText Clone() => (BcrdtXmlText)base.Clone();
}
