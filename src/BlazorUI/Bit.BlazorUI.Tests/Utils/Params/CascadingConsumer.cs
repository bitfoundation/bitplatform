using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Utils.Params;

public sealed class CascadingConsumer : ComponentBase
{
    [CascadingParameter] public int Number { get; set; }

    [CascadingParameter(Name = "Greeting")] public string? Greeting { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, $"{Number}-{Greeting}");
    }
}
