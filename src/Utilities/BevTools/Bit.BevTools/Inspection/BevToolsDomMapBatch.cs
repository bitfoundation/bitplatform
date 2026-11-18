using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BevTools;

/// <summary>
/// What locating many components in one pass can share: each parent's child positions, the anchor root and the roots'
/// signatures. Valid only until a component renders, so it lives for one synchronous pass on the dispatcher (one tick
/// of render flashing), never longer.
/// </summary>
internal sealed class BevToolsDomMapBatch
{
    internal Dictionary<ComponentState, Dictionary<int, BevToolsDomStep[]>> Children { get; } = new(ReferenceEqualityComparer.Instance);

    internal Dictionary<ComponentState, string> Signatures { get; } = new(ReferenceEqualityComparer.Instance);

    internal bool AnchorKnown { get; set; }

    internal ComponentState? Anchor { get; set; }

    internal List<ComponentState>? Roots { get; set; }
}
