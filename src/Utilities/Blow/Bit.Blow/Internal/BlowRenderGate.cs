using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Blow;

/// <summary>
/// Renders its content only when <see cref="Version"/> changes. The canvas puts its node and edge layers behind one,
/// so a frame that moves nothing but the viewport does not even walk the thousands of elements to find that none of
/// them changed - on the WebAssembly interpreter that walk alone is most of a frame. Infrastructure: not meant to be
/// used directly.
/// </summary>
public sealed class BlowRenderGate : ComponentBase
{
    /// <summary>The content is rendered again whenever this changes, and only then.</summary>
    [Parameter] public int Version { get; set; }

    /// <summary>The content behind the gate.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private int _rendered;

    /// <inheritdoc />
    protected override bool ShouldRender() => Version != _rendered;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _rendered = Version;
        builder.AddContent(0, ChildContent);
    }
}
