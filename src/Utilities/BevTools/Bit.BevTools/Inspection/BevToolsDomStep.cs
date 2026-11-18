namespace Bit.BevTools;

/// <summary>
/// One step through Blazor's logical DOM: the position among the logical children of the node above, and what is there
/// - an element's tag name, <c>#text</c>, or <c>#c</c> for the comment Blazor inserts as the container of a component
/// or of a markup block. The kind is what the browser verifies each step against.
/// </summary>
public readonly record struct BevToolsDomStep(int Index, string Kind);
