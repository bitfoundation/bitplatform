namespace Bit.BlazorUI;

// This is used by BitQuickGrid to move its body rendering to the end of the render queue so we can collect
// the list of child columns first. It has to be public only because it's used from .razor logic.

/// <summary>
/// For internal use only. Do not use.
/// </summary>
public class BitQuickGridDefer : ComponentBase
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

/// <summary>
/// Backward-compatible alias for <see cref="BitQuickGridDefer"/>, kept so existing code that referenced
/// the old <c>BitDataGridDefer</c> type keeps compiling. It inherits the renamed type, so it behaves
/// identically during the migration window.
/// </summary>
[Obsolete("BitDataGridDefer has been renamed to BitQuickGridDefer. Use BitQuickGridDefer instead.")]
public class BitDataGridDefer : BitQuickGridDefer
{
}
