using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// Content that records every time it is built, so a test can tell a Modal that keeps its content between
/// openings from one that takes it away and builds it again.
/// </summary>
public class TestModalStateContent : ComponentBase
{
    [Parameter] public List<string> Log { get; set; } = [];

    protected override void OnInitialized()
    {
        Log.Add("built");

        base.OnInitialized();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddAttribute(1, "class", "test-modal-state");
        builder.AddContent(2, Log.Count);
        builder.CloseElement();
    }
}
