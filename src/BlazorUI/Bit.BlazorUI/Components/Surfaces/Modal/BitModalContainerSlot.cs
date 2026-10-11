namespace Bit.BlazorUI;

/// <summary>
/// What a container that puts markup of its own around each modal renders that markup in: a component of no markup
/// of its own, there to carry the modal's key. A key is only told apart from the keys of its siblings, and the
/// markup of a template is not one of the container's own frames to key, so the slot is.
/// </summary>
/// <remarks>
/// It renders every time the container does, so the markup around a modal follows the state of the container; the
/// modal inside it is the <see cref="BitModalContainerEntry{TReference, TParameters}"/>, which renders only when its
/// own modal changed.
/// </remarks>
internal sealed class BitModalContainerSlot : IComponent
{
    internal const string ChildContentName = "ChildContent";

    private RenderHandle _renderHandle;



    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        var childContent = parameters.GetValueOrDefault<RenderFragment>(ChildContentName);

        _renderHandle.Render(childContent ?? (_ => { }));

        return Task.CompletedTask;
    }
}
