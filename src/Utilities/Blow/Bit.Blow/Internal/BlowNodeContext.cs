namespace Bit.Blow;

/// <summary>
/// Cascaded down to the content of a single node so nested handles know which
/// node they belong to.
/// </summary>
public sealed class BlowNodeContext
{
    /// <summary>
    /// The node this content belongs to. Settable because the context is cascaded as a fixed
    /// value: when the node instance behind an id is replaced (a restore, an undo), the renderer
    /// points the same context at the new instance rather than handing out a new context the
    /// content would never be told about.
    /// </summary>
    public required BlowNode Node { get; set; }
}
