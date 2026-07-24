namespace Bit.BlazorUI.Legacy;

// This is used by BitDataGridLegacy to move its body rendering to the end of the render queue so we can collect
// the list of child columns first. It has to be public only because it's used from .razor logic.

/// <summary>
/// For internal use only. Do not use.
/// </summary>
public class BitDataGridLegacyDefer : ComponentBase
{
    /// <summary>
    /// For internal use only. Do not use.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// For internal use only. Do not use.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}
