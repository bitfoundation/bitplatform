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
            // Dismissal is wired through IsOpenChanged rather than OnOverlayClick so that the
            // Blocking parameter is honored. IsOpenChanged only fires after BitModal's own
            // AssignIsOpen succeeds, and BitModal short-circuits (without calling AssignIsOpen)
            // when Blocking is set, so a Blocking modal won't light-dismiss on overlay click.
            // Wiring through OnOverlayClick instead would call Close() before BitModal's Blocking
            // guard runs, bypassing Blocking. This mirrors how BitProModalService wires dismissal.
            builder.AddComponentParameter(seq++, nameof(BitModal.IsOpenChanged), EventCallback.Factory.Create<bool>(modalReference, () => modalReference.Close()));
            builder.AddComponentParameter(seq++, nameof(BitModal.ChildContent), content);
            builder.CloseComponent();
        });
    }
}
