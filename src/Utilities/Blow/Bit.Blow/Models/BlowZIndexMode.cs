namespace Bit.Blow;

/// <summary>
/// How much of the stacking order the canvas manages for you. Mirrors React Flow's
/// <c>zIndexMode</c>.
/// </summary>
public enum BlowZIndexMode
{
    /// <summary>
    /// Selected and dragged elements are lifted, and a child node is kept above the group
    /// container it sits in. The right default for sub-flows, where a child that renders
    /// behind its own parent is never what was meant.
    /// </summary>
    Auto,

    /// <summary>Only selection and dragging lift an element; nesting is left alone.</summary>
    Basic,

    /// <summary>
    /// Nothing is lifted automatically - the only stacking is the explicit <c>ZIndex</c> you
    /// set on a node or edge. Choose this when the layering is yours to own.
    /// </summary>
    Manual,
}
