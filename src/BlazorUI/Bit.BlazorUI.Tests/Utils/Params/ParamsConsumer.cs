using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Utils.Params;

public sealed class ParamsConsumer : ComponentBase
{
    [CascadingParameter(Name = "A")] public FakeParamsA? A { get; set; }

    [CascadingParameter(Name = "B")] public FakeParamsB? B { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, $"{A?.Value}-{B?.Text}");
    }
}
