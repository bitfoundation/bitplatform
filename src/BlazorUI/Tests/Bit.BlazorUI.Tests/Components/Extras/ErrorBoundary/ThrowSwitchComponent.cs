using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

/// <summary>
/// Throws while <see cref="ShouldThrow"/> is set, so a test can make the same content throw again
/// after a recovery - which is what an error count reset has to be observed through.
/// </summary>
public class ThrowSwitchComponent : ComponentBase
{
    public static bool ShouldThrow;

    public static void Reset() => ShouldThrow = true;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("switched");
        }

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "throw-switch-safe");
        builder.AddContent(2, "Safe");
        builder.CloseElement();
    }
}
