namespace Bit.BlazorUI;

/// <summary>
/// One modal of a container that puts markup of its own around each modal it renders: what the template handed to
/// <see cref="BitModalContainerBase{TReference, TParameters}.RenderModals(RenderFragment{BitModalContainerItem{TReference}})"/>
/// is rendered with, once per modal.
/// </summary>
/// <typeparam name="TReference">The modal reference type of the container.</typeparam>
public sealed class BitModalContainerItem<TReference> where TReference : class
{
    internal BitModalContainerItem(TReference reference, RenderFragment modal)
    {
        Reference = reference;
        Modal = modal;
    }



    /// <summary>
    /// The reference of the modal - closed while it plays its exit animation, which is the only time a container
    /// holds a closed one.
    /// </summary>
    public TReference Reference { get; }

    /// <summary>
    /// The modal itself, with everything it expects from its container: render it once, where it goes.
    /// </summary>
    public RenderFragment Modal { get; }
}
