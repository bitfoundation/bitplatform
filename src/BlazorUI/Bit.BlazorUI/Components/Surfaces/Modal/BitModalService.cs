namespace Bit.BlazorUI;

/// <summary>
/// A core service to show any content inside a centralized <see cref="BitModal"/> using <see cref="BitModalContainer"/>.
/// </summary>
public class BitModalService : BitModalServiceBase<BitModalReference, BitModalParameters>
{
    protected override BitModalReference CreateReference(bool persistent)
    {
        return new BitModalReference(this, persistent);
    }

    protected override RenderFragment BuildModalFragment(BitModalReference modalReference, RenderFragment content)
    {
        return new RenderFragment(builder =>
        {
            var seq = 0;
            builder.OpenComponent<BitModal>(seq++);
            builder.SetKey(modalReference.Id);
            builder.AddComponentParameter(seq++, nameof(BitModal.IsOpen), true);
            builder.AddComponentParameter(seq++, nameof(BitModal.OnOverlayClick), EventCallback.Factory.Create<MouseEventArgs>(modalReference, () => modalReference.Close()));
            builder.AddComponentParameter(seq++, nameof(BitModal.ChildContent), content);
            builder.CloseComponent();
        });
    }
}
