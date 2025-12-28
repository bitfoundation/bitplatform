using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

public class ThrowOnceComponent : ComponentBase
{
    private static int _counter;

    public static void Reset() => _counter = 0;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_counter == 0)
        {
            _counter++;
            throw new InvalidOperationException("once");
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "throw-once-safe");
        builder.AddContent(2, "Recovered");
        builder.CloseElement();
    }
}
