using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

public class ThrowingComponent : ComponentBase
{
    [Parameter] public string Message { get; set; } = string.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        throw new InvalidOperationException(Message);
    }
}
