namespace Bit.BlazorUI;

/// <summary>
/// A service to show any content inside a centralized <see cref="BitProModal"/> using <see cref="BitProModalContainer"/>.
/// </summary>
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
