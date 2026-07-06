namespace Bit.BlazorUI;

/// <summary>
/// A reference to the <see cref="BitModal"/> instance that is shown using the <see cref="BitModalService"/>.
/// </summary>
public class BitModalReference : BitModalReferenceBase<BitModalReference, BitModalParameters>
{
    public BitModalReference(BitModalService modalService, bool persistent) : base(modalService, persistent)
    {
    }
}
