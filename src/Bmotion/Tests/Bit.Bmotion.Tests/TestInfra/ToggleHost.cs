using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// Renders its <see cref="ChildContent"/> only while <see cref="Show"/> is true. Flipping it off
/// unmounts the content through an ordinary render diff - the renderer disposes the removed
/// components inline, exactly as an <c>@if</c> in an app would - so disposal is observable
/// synchronously after the re-render instead of racing a separate teardown path.
/// </summary>
internal sealed class ToggleHost : ComponentBase
{
    [Parameter] public bool Show { get; set; } = true;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Show) builder.AddContent(0, ChildContent);
    }
}
