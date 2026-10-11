using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// Content that reads its modal's reference off the cascade and counts every time it is handed its parameters, so a
/// test can tell a cascade that hands the reference over once from one that hands it over on every render of the
/// container. It takes no parameters of its own, which would be handed over again whenever the modal re-renders.
/// </summary>
public class TestModalReferenceContent : ComponentBase
{
    [CascadingParameter] public BitModalReference? ModalReference { get; set; }

    public int ParametersSetCount { get; private set; }

    protected override void OnParametersSet()
    {
        ParametersSetCount++;

        base.OnParametersSet();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddAttribute(1, "class", "test-modal-reference");
        builder.AddContent(2, ModalReference?.Id);
        builder.CloseElement();
    }
}
