using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Extras.ProModal;

public class TestProModalContent : ComponentBase
{
    [Parameter] public string Message { get; set; } = string.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddAttribute(1, "class", "test-promodal-content");
        builder.AddContent(2, Message);
        builder.CloseElement();
    }
}
