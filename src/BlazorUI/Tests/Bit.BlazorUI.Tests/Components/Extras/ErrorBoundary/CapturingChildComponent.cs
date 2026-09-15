using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

/// <summary>
/// Reaches the boundary it sits inside through the cascade rather than through a reference, which is
/// how anything inside a <see cref="BitErrorBoundary"/> hands it an exception the renderer never saw.
/// </summary>
public class CapturingChildComponent : ComponentBase
{
    [CascadingParameter] public BitErrorBoundary? Boundary { get; set; }

    [Parameter] public Exception? Exception { get; set; }

    public bool WasCascaded => Boundary is not null;

    public void CaptureNow()
    {
        Boundary?.Capture(Exception ?? new InvalidOperationException("captured"));
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "capturing-child");
        builder.AddContent(2, "child");
        builder.CloseElement();
    }
}
