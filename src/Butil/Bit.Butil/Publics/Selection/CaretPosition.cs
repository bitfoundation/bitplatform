namespace Bit.Butil;

/// <summary>
/// A text position resolved from a point, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CaretPosition">CaretPosition</see>.
/// </summary>
/// <remarks>
/// The node itself can't cross the interop boundary, so what identifies it is here instead: its kind,
/// the tag it belongs to, and - for a text node - its text, which together with
/// <see cref="Offset"/> is enough to say which character the pointer was over.
/// </remarks>
public class CaretPosition
{
    /// <summary>The offset within the node, in characters for a text node.</summary>
    public int Offset { get; set; }

    /// <summary>The node's name, e.g. <c>"#text"</c> or <c>"DIV"</c>.</summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>The whole text of the node when it is a text node; empty otherwise.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The lower-cased tag of the element the position is in - the text node's parent, where it is one.</summary>
    public string ElementTag { get; set; } = string.Empty;
}
