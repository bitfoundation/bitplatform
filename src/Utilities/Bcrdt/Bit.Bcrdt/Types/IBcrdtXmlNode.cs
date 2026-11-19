namespace Bit.Bcrdt;

/// <summary>
/// A node of a collaborative XML tree: a <see cref="BcrdtXmlElement"/> or a <see cref="BcrdtXmlText"/>.
/// </summary>
/// <remarks>
/// Only those two types are nodes. A <see cref="BcrdtXmlContainer"/> refuses anything else as a child, so
/// a type implementing this interface outside the library cannot be inserted into a tree.
/// </remarks>
public interface IBcrdtXmlNode
{
    /// <summary>
    /// The fragment or element this node is a child of, or <see langword="null"/> when it is not in a tree.
    /// </summary>
    /// <remarks>
    /// A detached node waiting inside a detached container reports that container, so a tree built up
    /// before insertion can be walked upwards as well as downwards.
    /// </remarks>
    BcrdtXmlContainer? ParentNode { get; }

    /// <summary>The node as XML markup.</summary>
    string ToXmlString();
}
