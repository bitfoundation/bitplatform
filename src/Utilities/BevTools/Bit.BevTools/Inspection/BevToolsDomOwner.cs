namespace Bit.BevTools;

/// <summary>What owns a node of the page: the component that rendered it, the element it is, and how sure that is.</summary>
/// <param name="Record">The component reported: the one that rendered the node, or its nearest visible ancestor.</param>
/// <param name="InMarkup">The node was parsed from a markup block (a <c>MarkupString</c>, static HTML in a component) rather than rendered frame by frame.</param>
/// <param name="Ambiguous">Several roots render identical structure down to the node; the one in the same position in the page was chosen.</param>
/// <param name="Depth">How many steps of the requested path lead to the owner's container; -1 when the owner is the root component itself, whose container is the root's.</param>
/// <param name="RenderedBy">The framework or library component that actually rendered the node, when the owner reported is its nearest visible ancestor.</param>
/// <param name="Element">The element the path ends on ("&lt;button.btn&gt;"), when it ends on one.</param>
public sealed record BevToolsDomOwner(BevToolsComponentRecord Record, int Depth, string? RenderedBy, string? Element, bool InMarkup, bool Ambiguous);
