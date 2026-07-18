namespace Bit.BlazorUI;

/// <summary>
/// A service to show any content inside a centralized <see cref="BitProModal"/> using <see cref="BitProModalContainer"/>.
/// </summary>
/// <remarks>
/// A <see cref="BitProModalContainer"/> must be mounted in the layout for shown modals to render: a non-persistent modal
/// shown while no container is mounted is silently not rendered (see the base type remarks). Use
/// <see cref="BitModalServiceBase{TReference, TParameters}.IsContainerAvailable"/> to check whether a container is
/// currently mounted before showing a modal.
/// </remarks>
public class BitProModalService : BitModalServiceBase<BitProModalReference, BitProModalParameters>
{
    protected override BitProModalReference CreateReference(bool persistent)
    {
        return new BitProModalReference(this, persistent);
    }

    protected override RenderFragment BuildModalFragment(BitProModalReference modalReference, RenderFragment content)
    {
        return new RenderFragment(builder =>
        {
            var seq = 0;
            builder.OpenComponent<BitProModal>(seq++);
            builder.SetKey(modalReference.Id);
            builder.AddComponentParameter(seq++, nameof(BitProModal.IsOpen), true);
            builder.AddComponentParameter(seq++, nameof(BitProModal.IsOpenChanged), EventCallback.Factory.Create<bool>(modalReference, () => modalReference.Close()));
            builder.AddComponentParameter(seq++, nameof(BitProModal.ChildContent), content);
            builder.CloseComponent();
        });
    }
}
